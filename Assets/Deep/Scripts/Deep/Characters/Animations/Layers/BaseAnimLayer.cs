using System.Runtime.CompilerServices;
using AurigaGames.Deep.Characters.Animations.States;
using AurigaGames.Deep.Core;
using AurigaGames.Deep.Core.Animations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Characters.Animations.Layers
{
    [CreateAssetMenu(menuName = "Deep/Animation/Base Layer", order = 1)]
    public class BaseAnimLayer : AnimationLayer
    {
        [Title("States")]
        public GroundedDef Grounded;

        public InAirDef InAir;

        [Title("Layering")]
        public OverlayDef Overlay;

        [LabelText("Breathe")] public AnimationClip BreatheClip;
        [Range(0f, 1f), LabelText("Breathe Weight")] public float BreatheWeight = 1f;

        [Range(0f, -1f)] public float LegsMask = -0.5f;
        [Range(0f, 50f)] public float LegsMaskSmoothSpeed = 25f;

        [Title("FootIK")]
        [LabelText("Settings")] public FootIKSettings FootIKSettings = new()
        {
            EmitRayOffset = 0.5f,
            MaxRayDistance = 0.75f,

            MaxStepSize = 0.55f,

            WeightShiftAngle = 17.5f,
            WeightShiftHorizontal = -0.5f,
            WeightShiftVertical = -0.35f,
            MaxRotationOffset = 30f,

            WeightLerpSpeed = 35f,
            OffsetLerpSpeed = 10f,
        };
        [LabelText("Collide Against")] public LayerMask FootIKCollideAgainst;

        public override IAnimationLayerInstance Create(Animator animator, PlayableGraph graph)
        {
            return new Instance(animator, graph, this);
        }

        public class Instance : IAnimationLayerInstance, IAnimationLayerLogic, IAnimationLayerLateLogic
        {
            private const int MaxFootRaycasts = 4; // TODO: move MaxFootRaycasts to settings?

            private readonly BaseStateMachine _baseStates = new();
            private readonly OverlayStateMachine _overlayStates = new();

            private readonly LayeringJob _layeringJob;

            private readonly float _turnLimit;

            private readonly FootIKSettings _footIKSettings;
            private readonly RaycastHit[] _footRaycasts = new RaycastHit[MaxFootRaycasts];
            private readonly int _footIKLayers;
            private readonly CapsuleCollider _characterCollider;

            private AnimationScriptPlayable _playable;

            private float _legsMask;

            private float _aimYaw;
            private float _lastAimYaw;

            private readonly Transform _leftFootTM;
            private readonly Transform _rightFootTM;

            private Vector2 _footIKOffset;
            private float _footIKWeight;

            public readonly PlayableGraph Graph;
            public readonly BaseAnimLayer Layer;

            public readonly Transform Transform;
            public readonly Transform ParentTransform;

            public readonly CharacterMovement Movement;
            public readonly CharacterBody Body;

            public readonly AnimationStatesPlayable BaseMixer;
            public readonly AnimationStatesPlayable OverlayMixer;

            public Vector3 PlanarVelocity;
            public float PlanarSpeed;

            public float YawRate;
            public float YawReference;
            public float Yaw;
            public float YawSpeed;
            public float Pitch;
            public bool InPlace = true;

            public float TargetIKWeight;

            public Instance(Animator animator, PlayableGraph graph, BaseAnimLayer layer)
            {
                Graph = graph;
                Layer = layer;

                var baseStatesPlayable = ScriptPlayable<AnimationStatesPlayable>.Create(graph);
                var overlayStatesPlayable = ScriptPlayable<AnimationStatesPlayable>.Create(graph);

                Transform = animator.transform;
                ParentTransform = Transform.parent;

                Movement = animator.GetComponentInParent<CharacterMovement>();
                Body = animator.GetComponentInParent<CharacterBody>();
                BaseMixer = baseStatesPlayable.GetBehaviour();
                OverlayMixer = overlayStatesPlayable.GetBehaviour();

                _baseStates.Initialize(this);
                _overlayStates.Initialize(this);

                _layeringJob = LayeringJobBinder.Create(animator);

                var standPlayable = AnimationClipPlayable.Create(graph, Layer.Grounded.Stand.PoseClip);
                standPlayable.SetApplyFootIK(false);
                var breatheClip = AnimationClipPlayable.Create(graph, Layer.BreatheClip);
                breatheClip.SetApplyFootIK(false);
                var breatheMixer = AnimationLayerMixerPlayable.Create(graph);
                breatheMixer.AddInput(baseStatesPlayable, 0, 1f);
                breatheMixer.AddInput(breatheClip, 0, Layer.BreatheWeight);
                breatheMixer.SetLayerAdditive(1, true);

                var layeringPlayable = AnimationScriptPlayable.Create(graph, _layeringJob);
                layeringPlayable.AddInput(standPlayable, 0, 1f);
                layeringPlayable.AddInput(breatheMixer, 0, 1f);
                layeringPlayable.AddInput(overlayStatesPlayable, 0, 1f);

                var footIKJob = new FootIKJob {Settings = Layer.FootIKSettings};
                _playable = AnimationScriptPlayable.Create(graph, footIKJob);
                _playable.AddInput(layeringPlayable, 0, 1f);

                _footIKSettings = Layer.FootIKSettings;
                _leftFootTM = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                _rightFootTM = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                _footIKLayers = Layer.FootIKCollideAgainst.value;
                _characterCollider = Movement.GetComponent<CapsuleCollider>();

                _baseStates.Initialize(this);
                _overlayStates.Initialize(this);

                _turnLimit = layer.Grounded.Stand.TurnLimit;
            }

            public void SetPlayableInput(Playable playable, int port) { }

            public void GetPlayableOutput(out Playable playable, out int port)
            {
                playable = _playable;
                port = 0;
            }

            public void Destroy()
            {
                _overlayStates.OnExit();
                _baseStates.OnExit();

                _layeringJob.Destroy();
            }

            public void Update()
            {
                // update aiming
                _aimYaw = ParentTransform.rotation.eulerAngles.y;
                Pitch = Body.AimPitch;

                YawRate = Mathf.Abs(_aimYaw - _lastAimYaw) / Time.deltaTime;
                _lastAimYaw = _aimYaw;

                var characterUp = Transform.up;

                PlanarVelocity = Vector3.ProjectOnPlane(Movement.Velocity, characterUp);
                PlanarSpeed = PlanarVelocity.magnitude;

                // update state machines
                _baseStates.OnUpdate();
                _overlayStates.OnUpdate();

                // update layering
                var targetLegsMask = 0f;
                if (!InPlace || Mathf.Abs(YawSpeed) > 0f)
                {
                    targetLegsMask = Layer.LegsMask;
                }

                _legsMask = Mathf.Lerp(_legsMask, targetLegsMask, Layer.LegsMaskSmoothSpeed * Time.deltaTime);
                OverlayMixer.OverlayWeights.Hips += _legsMask;
                OverlayMixer.OverlayWeights.Legs += _legsMask;
                _layeringJob.Update(OverlayMixer.OverlayWeights);

                // update foot IK
                UpdateFootIK();
            }

            public void LateUpdate()
            {
                if (InPlace)
                {
                    var targetYaw = ParentTransform.rotation.eulerAngles.y;
                    if (Mathf.Abs(YawSpeed) > 0f)
                    {
                        YawReference = MathHelper.NormalizeAngle(YawReference + YawSpeed);
                        var yawDelta = MathHelper.NormalizeAngle(targetYaw - YawReference);
                        if (Mathf.Abs(yawDelta) > _turnLimit)
                        {
                            var yawDiff = yawDelta > 0f ? _turnLimit : -_turnLimit;
                            YawReference = MathHelper.NormalizeAngle(YawReference + (yawDelta - yawDiff));
                        }
                    }

                    Yaw = MathHelper.NormalizeAngle(targetYaw - YawReference);
                }
                else
                    Yaw = Mathf.SmoothStep(Yaw, 0.0f, Layer.Grounded.Stand.SmoothSpeed * Time.deltaTime);

                var angles = Transform.localEulerAngles;
                Transform.localEulerAngles = new Vector3(angles.x, -Yaw, angles.z);

                Body.YawOffset = Yaw;
            }

            private void UpdateFootIK()
            {
                var job = _playable.GetJobData<FootIKJob>();

                // lerp IK weight
                _footIKWeight = Mathf.Lerp(_footIKWeight, TargetIKWeight,
                    _footIKSettings.WeightLerpSpeed * Time.deltaTime);
                if (_footIKWeight <= 0f)
                {
                    job.Weight = _footIKWeight;
                    _playable.SetJobData(job);
                    return;
                }

                // setup raycast origins
                var rootPos = Transform.position;
                var leftFootPos = _leftFootTM.position;
                var rightFootPos = _rightFootTM.position;
                leftFootPos.y = rootPos.y;
                rightFootPos.y = rootPos.y;

                // raycast
                var rayEmitOffset = Vector3.up * _footIKSettings.EmitRayOffset; // TODO: support custom gravity?
                var maxRayDistance = _footIKSettings.EmitRayOffset + _footIKSettings.MaxRayDistance;
                var leftSuccess = FootRaycast(leftFootPos, rayEmitOffset, maxRayDistance, out var leftHit);
                var rightSuccess = FootRaycast(rightFootPos, rayEmitOffset, maxRayDistance, out var rightHit);

                var leftOffset = 0f;
                var rightOffset = 0f;
                if (leftSuccess)
                {
                    leftOffset = Mathf.Clamp(leftHit.point.y - leftFootPos.y, -_footIKSettings.MaxStepSize,
                        _footIKSettings.MaxStepSize);
                }

                if (rightSuccess)
                {
                    rightOffset = Mathf.Clamp(rightHit.point.y - rightFootPos.y, -_footIKSettings.MaxStepSize,
                        _footIKSettings.MaxStepSize);
                }

                var stepMag = Mathf.Abs(leftOffset - rightOffset);

                if (stepMag > _footIKSettings.MaxStepSize)
                {
                    leftOffset = (leftOffset / stepMag) * _footIKSettings.MaxStepSize;
                    rightOffset = (rightOffset / stepMag) * _footIKSettings.MaxStepSize;
                }

                var targetIKOffset = new Vector2(leftOffset, rightOffset);
                _footIKOffset = Vector2.Lerp(_footIKOffset, targetIKOffset,
                    _footIKSettings.OffsetLerpSpeed * Time.deltaTime);

                job.Weight = _footIKWeight;
                job.IKOffset = _footIKOffset;
                job.LeftNormal = leftHit.normal;
                job.RightNormal = rightHit.normal;
                _playable.SetJobData(job);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool FootRaycast(Vector3 footPos, Vector3 rayOffset, float maxDistance,
                out RaycastHit closestHit)
            {
                closestHit = new RaycastHit();

                var numHits = Physics.RaycastNonAlloc
                (
                    footPos + rayOffset, Vector3.down, _footRaycasts, maxDistance,
                    _footIKLayers, QueryTriggerInteraction.Ignore
                );

                var foundValidHit = false;
                var closestDistance = Mathf.Infinity;
                for (var i = 0; i < numHits; i++)
                {
                    var hit = _footRaycasts[i];
                    var hitDistance = hit.distance;

                    // Find the closest valid hit
                    if (hitDistance > 0f && hit.collider != _characterCollider)
                    {
                        if (hitDistance < closestDistance)
                        {
                            closestHit = hit;
                            closestDistance = hitDistance;

                            foundValidHit = true;
                        }
                    }
                }

                return foundValidHit;
            }
        }
    }
}
