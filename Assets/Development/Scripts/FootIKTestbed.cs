using AurigaGames.Deep.Core.Animations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Development
{
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class FootIKTestbed : MonoBehaviour
    {
        private Animator _animator;

        private PlayableGraph _graph;

        private LayeringJob _layeringJob;
        private AnimationScriptPlayable _layeringPlayable;

        private FootIKJob _footIKJob;
        private AnimationScriptPlayable _footIKPlayable;
        private int _footIKLayerMask;
        private RaycastHit _leftHit;
        private RaycastHit _rightHit;

        [Title("Animations")]
        [LabelText("Pose")] public AnimationClip BasePoseClip;
        [LabelText("Base")] public AnimationClip BaseClip;
        [LabelText("Overlay")] public AnimationClip OverlayClip;
        [LabelText("Breathe")] public AnimationClip BreatheClip;

        [Title("Layering")]
        [LabelText("Weights")] public OverlayWeights OverlayWeights = new()
        {
            Hips = 1f,
            Legs = 1f,
            Spine = 0.5f,
            Head = 0.5f,
            ArmL = 0.5f,
            ArmLOverride = 0f,
            ArmR = 1f,
            ArmROverride = 1f,
            HandL = 1f,
            HandR = 1f,
        };
        [LabelText("On")] public bool UseLayering = true;

        [Title("Foot IK")]
        public FootIKSettings FootIKSettings = new()
        {
            EmitRayOffset = 0.5f,
            MaxRayDistance = 0.75f,

            MaxStepSize = 0.55f,

            WeightShiftAngle = 17.5f,
            WeightShiftHorizontal = -0.5f,
            WeightShiftVertical = -0.35f,
            MaxRotationOffset = 30f,
        };
        [Range(0f, 1f)] public float IKWeight = 1f;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _graph = PlayableGraph.Create(gameObject.name);

            // create clip playables
            var basePosePlayable = AnimationClipPlayable.Create(_graph, BasePoseClip);
            basePosePlayable.SetApplyFootIK(false);
            var basePlayable = AnimationClipPlayable.Create(_graph, BaseClip);
            basePlayable.SetApplyFootIK(true);
            var overlayPlayable = AnimationClipPlayable.Create(_graph, OverlayClip);
            overlayPlayable.SetApplyFootIK(true);
            var breathePlayable = AnimationClipPlayable.Create(_graph, BreatheClip);
            breathePlayable.SetApplyFootIK(false);

            // breathe
            var breatheMixer = AnimationLayerMixerPlayable.Create(_graph);
            breatheMixer.AddInput(basePlayable, 0, 1f);
            breatheMixer.AddInput(breathePlayable, 0, 1f);
            breatheMixer.SetLayerAdditive(1, true);

            // layering
            _layeringJob = LayeringJobBinder.Create(_animator);
            if (UseLayering)
            {
                _layeringPlayable = AnimationScriptPlayable.Create(_graph, _layeringJob);
                _layeringPlayable.AddInput(basePosePlayable, 0, 1f);
                _layeringPlayable.AddInput(breatheMixer, 0, 1f);
                _layeringPlayable.AddInput(overlayPlayable, 0, 1f);
            }

            // foot ik
            _footIKPlayable = AnimationScriptPlayable.Create(_graph, new FootIKJob {Settings = FootIKSettings});
            if (UseLayering)
                _footIKPlayable.AddInput(_layeringPlayable, 0, 1f);
            else
                _footIKPlayable.AddInput(breatheMixer, 0, 1f);

            _footIKLayerMask = 1 << LayerMask.NameToLayer("Default");

            // graph output
            var output = AnimationPlayableOutput.Create(_graph, "FootIKTest", _animator);
            output.SetSourcePlayable(_footIKPlayable);

            // start graph
            _graph.Play();
        }

        private void Update()
        {
            // layering
            _layeringJob.Update(OverlayWeights);

            // foot IK
            UpdateFootIK();
        }

        private void OnDestroy()
        {
            _layeringJob.Destroy();

            _graph.Destroy();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(_leftHit.point, _leftHit.normal * 0.2f);
            Gizmos.DrawRay(_rightHit.point, _rightHit.normal * 0.2f);
        }

        private void UpdateFootIK()
        {
            // setup raycast origins
            var rootPos = transform.position;
            var leftFootTM = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFootTM = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var leftFootPos = leftFootTM.position;
            var rightFootPos = rightFootTM.position;
            leftFootPos.y = rootPos.y;
            rightFootPos.y = rootPos.y;

            // raycast
            var rayEmitOffset = Vector3.up * FootIKSettings.EmitRayOffset;
            var maxRayDistance = FootIKSettings.EmitRayOffset + FootIKSettings.MaxRayDistance;
            var leftSuccess = Physics.Raycast(leftFootPos + rayEmitOffset, Vector3.down, out _leftHit, maxRayDistance,
                _footIKLayerMask);
            var rightSuccess = Physics.Raycast(rightFootPos + rayEmitOffset, Vector3.down, out _rightHit,
                maxRayDistance,
                _footIKLayerMask);

            var leftOffset = 0f;
            var rightOffset = 0f;
            if (leftSuccess)
            {
                leftOffset = Mathf.Clamp(_leftHit.point.y - leftFootPos.y, -FootIKSettings.MaxStepSize,
                    FootIKSettings.MaxStepSize);
            }

            if (rightSuccess)
            {
                rightOffset = Mathf.Clamp(_rightHit.point.y - rightFootPos.y, -FootIKSettings.MaxStepSize,
                    FootIKSettings.MaxStepSize);
            }

            var stepMag = Mathf.Abs(leftOffset - rightOffset);

            if (stepMag > FootIKSettings.MaxStepSize)
            {
                leftOffset = (leftOffset / stepMag) * FootIKSettings.MaxStepSize;
                rightOffset = (rightOffset / stepMag) * FootIKSettings.MaxStepSize;
            }

            var job = _footIKPlayable.GetJobData<FootIKJob>();
            job.Weight = IKWeight;
            job.Settings = FootIKSettings;
            job.IKOffset = new Vector2(leftOffset, rightOffset);
            job.LeftNormal = _leftHit.normal;
            job.RightNormal = _rightHit.normal;
            _footIKPlayable.SetJobData(job);
        }
    }
}
