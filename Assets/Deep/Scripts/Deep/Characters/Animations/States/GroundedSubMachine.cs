using AurigaGames.Deep.Characters.Animations.Layers;
using AurigaGames.Deep.Core;
using AurigaGames.Deep.Core.Animations;
using AurigaGames.Deep.Core.StateSystem;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Characters.Animations.States
{
    public class GroundedSubMachine : SubStateMachine<BaseAnimLayer.Instance, BaseStateMachine, GroundedSubMachine>
    {
        public readonly StandState Stand = new();
        public readonly MoveState Move = new();
        public readonly SprintState Sprint = new();

        public override void Initialize(BaseAnimLayer.Instance outer)
        {
            base.Initialize(outer);

            Stand.Initialize(outer);
            Move.Initialize(outer);
            Sprint.Initialize(outer);

            OnEnter();
        }

        public override void OnEnter()
        {
            Outer.TargetIKWeight = 1f;

            SetState(Outer.Body.IsSprinting ? Sprint : Outer.PlanarSpeed > 0.0f ? Move : Stand);
        }

        public override void OnExit()
        {
            base.OnExit();

            Outer.TargetIKWeight = 0f;
        }

        public override void OnUpdate()
        {
            // TODO: Swimming/Climbing
            if (!Outer.Movement.IsGrounded)
            {
                Owner.SetState(Owner.InAir);
                return;
            }

            base.OnUpdate();
        }

        public class StandState : State<BaseAnimLayer.Instance, GroundedSubMachine>
        {
            private GroundedDef.StandDef _def;

            private AnimationMixerPlayable _mixer;

            private AnimationClipPlayable _animStandPose;
            private AnimationClipPlayable _animTransitionL;
            private AnimationClipPlayable _animTransitionR;
            private AnimationClipPlayable _animRotateL;
            private AnimationClipPlayable _animRotateR;

            private AnimationClipPlayable _animRotate;
            private float _animRotateDuration;
            
            private MixerTransition<AnimationMixerPlayable, MixerPort> _transition;

            private short _rotationDirection;
            private short _lastRotationDirection;
            private MixerPort _standPort;

            private FloatRange _turnYawSpeed;
            private FloatRange _turnPlaySpeed;
            private AnimationCurve _curveL;
            private AnimationCurve _curveR;

            public float FadeDuration = 0.2f;

            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                base.Initialize(outer);

                _def = Outer.Layer.Grounded.Stand;

                _animStandPose = AnimationClipPlayable.Create(Outer.Graph, _def.PoseClip);
                _animStandPose.SetApplyFootIK(true);
                _animStandPose.Pause();

                _animTransitionL = AnimationClipPlayable.Create(Outer.Graph, _def.TransitionLClip);
                _animTransitionL.SetApplyFootIK(true);
                _animTransitionL.SetSpeed(1.5f);
                _animTransitionR = AnimationClipPlayable.Create(Outer.Graph, _def.TransitionRClip);
                _animTransitionR.SetApplyFootIK(true);
                _animTransitionR.SetSpeed(1.5f);

                _animRotateL = AnimationClipPlayable.Create(Outer.Graph, _def.TurnLClip);
                _animRotateL.SetApplyFootIK(true);
                _animRotateR = AnimationClipPlayable.Create(Outer.Graph, _def.TurnRClip);
                _animRotateR.SetApplyFootIK(true);

                _mixer = AnimationMixerPlayable.Create(Outer.Graph);
                _mixer.AddInput(_animStandPose, 0, 1.0f);
                _mixer.AddInput(_animTransitionL, 0);
                _mixer.AddInput(_animTransitionR, 0);
                _mixer.AddInput(_animRotateL, 0);
                _mixer.AddInput(_animRotateR, 0);

                _transition = new MixerTransition<AnimationMixerPlayable, MixerPort>(_mixer);

                _turnYawSpeed = new FloatRange(_def.MinYawSpeed, _def.MaxYawSpeed);
                _turnPlaySpeed = new FloatRange(_def.MinPlaySpeed, _def.MaxPlaySpeed);

                FadeDuration = _def.FadeInDuration;
            }

            public override void OnEnter()
            {
                Outer.YawReference = Outer.ParentTransform.eulerAngles.y;
                Outer.InPlace = true;
                Outer.YawSpeed = 0f;

                Outer.BaseMixer.Play(_mixer, FadeDuration);

                _standPort = Random.Range(0f, 1f) > 0.5f ? MixerPort.TransitionL : MixerPort.TransitionR;

                _animTransitionL.SetTime(0.8f);
                _animTransitionR.SetTime(0.8f);

                _rotationDirection = 0;
            }

            public override void OnExit()
            {
                Outer.InPlace = false;
                Outer.YawSpeed = 0f;

                FadeDuration = _def.FadeInDuration;
            }

            public override void OnUpdate()
            {
                if (Outer.PlanarSpeed > Mathf.Epsilon)
                {
                    Owner.Move.FadeDuration = _def.FadeOutDuration;
                    Owner.SetState(Owner.Move);
                    return;
                }

                var yaw = Outer.Yaw;
                var absYaw = Mathf.Abs(yaw);
                if (_rotationDirection == 0)
                {
                    if (absYaw > _def.Threshold)
                    {
                        _rotationDirection = (short) Mathf.Sign(yaw);

                        var anim = _rotationDirection == -1 ? _animRotateL : _animRotateR;

                        anim.SetTime(0.0f);
                        anim.SetDuration(Mathf.Infinity);
                    }
                }

                if (_rotationDirection != 0)
                {
                    if (yaw * _rotationDirection <= 0.0f)
                    {
                        var anim = _rotationDirection == -1 ? _animRotateL : _animRotateR;
                        var repeatCount = Mathf.Ceil((float) (anim.GetTime() / anim.GetAnimationClip().length));
                        _animRotateDuration = anim.GetAnimationClip().length * repeatCount;
                        anim.SetDuration(_animRotateDuration);
                        _animRotate = anim;

                        _standPort = MixerPort.StandPose;

                        _lastRotationDirection = _rotationDirection;
                        _rotationDirection = 0;
                    }
                }

                if (_rotationDirection == 0)
                {
                    if (_lastRotationDirection != 0 && _animRotate.IsValid())
                    {
                        if (_animRotate.GetTime() >= _animRotate.GetDuration())
                        {
                            _lastRotationDirection = 0;
                            Outer.YawSpeed = 0f;
                        }
                        else
                        {
                            var curve = _lastRotationDirection == -1 ? _curveL : _curveR;
                            Outer.YawSpeed = curve != null
                                ? curve.Evaluate((float) (_animRotate.GetTime() * _animRotate.GetSpeed())) * Time.deltaTime
                                : _turnYawSpeed.Max * _rotationDirection * Time.deltaTime;
                        }
                    }
                    if (_lastRotationDirection == 0)
                    {
                        _transition.Update(_standPort, _def.TurnToStandDuration, Time.deltaTime);
                    }
                }
                else
                {
                    var speed = MathHelper.GetMappedRangeValueClamped(
                        _turnYawSpeed.Min, _turnYawSpeed.Max,
                        _turnPlaySpeed.Min, _turnPlaySpeed.Max,
                        Outer.YawRate);
                    var port = _rotationDirection == -1 ? MixerPort.RotationL : MixerPort.RotationR;
                    var anim = _rotationDirection == -1 ? _animRotateL : _animRotateR;
                    anim.SetSpeed(speed);

                    _transition.Update(port, _def.StandToTurnDuration, Time.deltaTime);

                    var curve = _rotationDirection == -1 ? _curveL : _curveR;
                    Outer.YawSpeed = curve != null
                        ? curve.Evaluate((float) (anim.GetTime() * anim.GetSpeed())) * Time.deltaTime
                        : _turnYawSpeed.Max * _rotationDirection * Time.deltaTime;
                }
            }

            private enum MixerPort
            {
                StandPose,
                TransitionL,
                TransitionR,
                RotationL,
                RotationR,
            }
        }

        public class MoveState : State<BaseAnimLayer.Instance, GroundedSubMachine>
        {
            private GroundedDef.MoveDef _def;

            private CardinalDirection _cardinalDirection;

            private float _stride;
            private float _forwardWeight = 1.0f;
            private float _backwardWeight;
            private float _leftWeight;
            private float _rightWeight;

            private AnimationMixerPlayable _forwardMixer;
            private AnimationMixerPlayable _backwardMixer;
            private AnimationMixerPlayable _leftMixer;
            private AnimationMixerPlayable _rightMixer;

            public float FadeDuration;

            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                base.Initialize(outer);

                _def = Outer.Layer.Grounded.Move;

                _cardinalDirection = CardinalDirection.Forward;

                _forwardMixer = CreateDirectionMixer(
                    CreateForwardMixer(), CreateBackwardMixer(),
                    CreateLeftForwardMixer(), CreateRightForwardMixer());
                _backwardMixer = CreateDirectionMixer(
                    CreateForwardMixer(), CreateBackwardMixer(),
                    CreateLeftBackwardMixer(), CreateRightBackwardMixer());
                _leftMixer = CreateDirectionMixer(
                    CreateForwardMixer(), CreateBackwardMixer(),
                    CreateLeftForwardMixer(), CreateRightBackwardMixer());
                _rightMixer = CreateDirectionMixer(
                    CreateForwardMixer(), CreateBackwardMixer(),
                    CreateLeftBackwardMixer(), CreateRightForwardMixer());

                FadeDuration = _def.FadeInDuration;
            }

            public override void OnEnter()
            {
                switch (_cardinalDirection)
                {
                    case CardinalDirection.Forward:
                    {
                        _forwardMixer.SetTime(0f);
                        Outer.BaseMixer.Play(_forwardMixer, FadeDuration);
                        break;
                    }
                    case CardinalDirection.Backward:
                    {
                        _backwardMixer.SetTime(0f);
                        Outer.BaseMixer.Play(_backwardMixer, FadeDuration);
                        break;
                    }
                    case CardinalDirection.Left:
                    {
                        _leftMixer.SetTime(0f);
                        Outer.BaseMixer.Play(_leftMixer, FadeDuration);
                        break;
                    }
                    case CardinalDirection.Right:
                    {
                        _rightMixer.SetTime(0f);
                        Outer.BaseMixer.Play(_rightMixer, FadeDuration);
                        break;
                    }
                }
            }

            public override void OnExit()
            {
                FadeDuration = _def.FadeInDuration;
            }

            public override void OnUpdate()
            {
                var relativeVelocityDir = Outer.Transform.InverseTransformDirection(Outer.PlanarVelocity);
                relativeVelocityDir.Normalize();

                if (Mathf.Approximately(relativeVelocityDir.magnitude, 0f))
                {
                    Owner.Stand.FadeDuration = _def.FadeOutDuration;
                    Owner.SetState(Owner.Stand);
                    return;
                }

                if (Outer.Body.IsSprinting && Outer.PlanarSpeed >= _def.SprintThreshold)
                {
                    Owner.Sprint.FadeDuration = _def.FadeOutDuration;
                    Owner.SetState(Owner.Sprint);
                    return;
                }

                var relativeDir = relativeVelocityDir /
                                  (Mathf.Abs(relativeVelocityDir.x) + Mathf.Abs(relativeVelocityDir.y) +
                                   Mathf.Abs(relativeVelocityDir.z));

                var forwardWeight = Mathf.Clamp01(relativeDir.z);
                var backwardWeight = Mathf.Abs(Mathf.Clamp(relativeDir.z, -1f, 0f));
                var leftWeight = Mathf.Abs(Mathf.Clamp(relativeDir.x, -1f, 0f));
                var rightWeight = Mathf.Clamp01(relativeDir.x);

                // TODO: Time.deltaTime - use time manager or OnUpdate(deltaTime) 
                _forwardWeight = Mathf.Lerp(_forwardWeight, forwardWeight, _def.VelocityLerpSpeed * Time.deltaTime);
                _backwardWeight = Mathf.Lerp(_backwardWeight, backwardWeight, _def.VelocityLerpSpeed * Time.deltaTime);
                _leftWeight = Mathf.Lerp(_leftWeight, leftWeight, _def.VelocityLerpSpeed * Time.deltaTime);
                _rightWeight = Mathf.Lerp(_rightWeight, rightWeight, _def.VelocityLerpSpeed * Time.deltaTime);

                var targetStride = MathHelper.GetMappedRangeValueUnclamped(
                    _def.MinMoveSpeed, _def.MaxMoveSpeed,
                    0f, 1f, Outer.PlanarSpeed);
                _stride = Mathf.Lerp(_stride, targetStride, _def.StrideLerpSpeed * Time.deltaTime);

                UpdateWeights(_forwardMixer);
                UpdateWeights(_backwardMixer);
                UpdateWeights(_leftMixer);
                UpdateWeights(_rightMixer);

                var angle = Mathf.Atan2(relativeVelocityDir.x, relativeVelocityDir.z) * Mathf.Rad2Deg;
                var nextCardinalDirection = CalculateCardinalDirection(angle, 70.0f, 5.0f);

                if (nextCardinalDirection == _cardinalDirection)
                {
                    return;
                }

                switch (_cardinalDirection)
                {
                    case CardinalDirection.Forward:
                        switch (nextCardinalDirection)
                        {
                            case CardinalDirection.Backward:
                                PlayNext(_forwardMixer, _backwardMixer);
                                break;
                            case CardinalDirection.Left:
                                PlayNext(_forwardMixer, _leftMixer);
                                break;
                            case CardinalDirection.Right:
                                PlayNext(_forwardMixer, _rightMixer);
                                break;
                        }

                        break;
                    case CardinalDirection.Backward:
                        switch (nextCardinalDirection)
                        {
                            case CardinalDirection.Forward:
                                PlayNext(_backwardMixer, _forwardMixer);
                                break;
                            case CardinalDirection.Left:
                                PlayNext(_backwardMixer, _leftMixer);
                                break;
                            case CardinalDirection.Right:
                                PlayNext(_backwardMixer, _rightMixer);
                                break;
                        }

                        break;
                    case CardinalDirection.Left:
                        switch (nextCardinalDirection)
                        {
                            case CardinalDirection.Forward:
                                PlayNext(_leftMixer, _forwardMixer);
                                break;
                            case CardinalDirection.Backward:
                                PlayNext(_leftMixer, _backwardMixer);
                                break;
                            case CardinalDirection.Right:
                                PlayNext(_leftMixer, _rightMixer);
                                break;
                        }

                        break;
                    case CardinalDirection.Right:
                        switch (nextCardinalDirection)
                        {
                            case CardinalDirection.Forward:
                                PlayNext(_rightMixer, _forwardMixer);
                                break;
                            case CardinalDirection.Backward:
                                PlayNext(_rightMixer, _backwardMixer);
                                break;
                            case CardinalDirection.Left:
                                PlayNext(_rightMixer, _leftMixer);
                                break;
                        }

                        break;
                }

                _cardinalDirection = nextCardinalDirection;
            }

            private AnimationMixerPlayable CreateDirectionMixer(
                Playable forward, Playable backward,
                Playable left, Playable right)
            {
                var mixer = AnimationMixerPlayable.Create(Outer.Graph);
                mixer.SetPropagateSetTime(true);
                mixer.AddInput(forward, 0);
                mixer.AddInput(backward, 0);
                mixer.AddInput(left, 0);
                mixer.AddInput(right, 0);
                mixer.SetSpeed(_def.PlaySpeed);
                return mixer;
            }

            private Playable CreateForwardMixer() => CreateStrideMixer(_def.ForwardPoseClip, _def.ForwardAnimClip);
            private Playable CreateBackwardMixer() => CreateStrideMixer(_def.BackwardPoseClip, _def.BackwardAnimClip);

            private Playable CreateLeftForwardMixer() =>
                CreateStrideMixer(_def.LeftForwardPoseClip, _def.LeftForwardAnimClip);

            private Playable CreateLeftBackwardMixer() =>
                CreateStrideMixer(_def.LeftBackwardPoseClip, _def.LeftBackwardAnimClip);

            private Playable CreateRightForwardMixer() =>
                CreateStrideMixer(_def.RightForwardPoseClip, _def.RightForwardAnimClip);

            private Playable CreateRightBackwardMixer() =>
                CreateStrideMixer(_def.RightBackwardPoseClip, _def.RightBackwardAnimClip);


            private AnimationMixerPlayable CreateStrideMixer(AnimationClip pose, AnimationClip anim)
            {
                var posePlayable = AnimationClipPlayable.Create(Outer.Graph, pose);
                posePlayable.SetApplyFootIK(true);
                posePlayable.Pause();
                var animPlayable = AnimationClipPlayable.Create(Outer.Graph, anim);
                animPlayable.SetApplyFootIK(true);

                var mixer = AnimationMixerPlayable.Create(Outer.Graph);
                mixer.SetPropagateSetTime(true);
                mixer.AddInput(posePlayable, 0, 1f);
                mixer.AddInput(animPlayable, 0);

                return mixer;
            }

            private void UpdateWeights(AnimationMixerPlayable mixer)
            {
                mixer.SetInputWeight(0, _forwardWeight);
                mixer.SetInputWeight(1, _backwardWeight);
                mixer.SetInputWeight(2, _leftWeight);
                mixer.SetInputWeight(3, _rightWeight);

                var weight1 = Mathf.Clamp01(_stride);
                var weight0 = 1f - weight1;

                for (var i = 0; i < 4; ++i)
                {
                    var dirMixer = mixer.GetInput(i);
                    dirMixer.SetInputWeight(0, weight0);
                    dirMixer.SetInputWeight(1, weight1);
                }
            }

            private void PlayNext(Playable current, Playable next)
            {
                next.SetTime(current.GetTime());
                Outer.BaseMixer.Play(next, _def.DirectionFadeDuration);
            }

            private static CardinalDirection CalculateCardinalDirection(float angle, float forwardHalfAngle,
                float angleOffset)
            {
                if (angle >= -forwardHalfAngle - angleOffset && angle <= forwardHalfAngle + angleOffset)
                {
                    return CardinalDirection.Forward;
                }

                if (angle >= forwardHalfAngle - angleOffset && angle <= 180.0f - forwardHalfAngle + angleOffset)
                {
                    return CardinalDirection.Right;
                }

                if (angle <= -(forwardHalfAngle - angleOffset) && angle >= -(180.0f - forwardHalfAngle + angleOffset))
                {
                    return CardinalDirection.Left;
                }

                return CardinalDirection.Backward;
            }

            private enum CardinalDirection
            {
                Forward,
                Backward,
                Left,
                Right
            }
        }

        public class SprintState : State<BaseAnimLayer.Instance, GroundedSubMachine>
        {
            private GroundedDef.SprintDef _def;

            private AnimationClipPlayable _animForward;
            private AnimationClipPlayable _animLeft;
            private AnimationClipPlayable _animRight;

            private AnimationMixerPlayable _mixer;

            private float _forwardWeight = 1f;
            private float _leftWeight;
            private float _rightWeight;

            public float FadeDuration = 0.2f;

            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                base.Initialize(outer);

                _def = Outer.Layer.Grounded.Sprint;

                // TODO: adjust sprint play speed to attributes?
                var movePlaySpeed = Outer.Layer.Grounded.Move.PlaySpeed;
                var playSpeed = movePlaySpeed * _def.PlaySpeed;

                _animForward = AnimationClipPlayable.Create(Outer.Graph, _def.ForwardClip);
                _animForward.SetApplyFootIK(true);
                _animForward.SetSpeed(movePlaySpeed);
                _animLeft = AnimationClipPlayable.Create(Outer.Graph, _def.LeftClip);
                _animLeft.SetApplyFootIK(true);
                _animLeft.SetSpeed(playSpeed);
                _animRight = AnimationClipPlayable.Create(Outer.Graph, _def.RightClip);
                _animRight.SetApplyFootIK(true);
                _animRight.SetSpeed(playSpeed);

                _mixer = AnimationMixerPlayable.Create(Outer.Graph);
                _mixer.SetPropagateSetTime(true);
                _mixer.AddInput(_animForward, 0, 1f);
                _mixer.AddInput(_animLeft, 0);
                _mixer.AddInput(_animRight, 0);
            }

            public override void OnEnter()
            {
                Outer.BaseMixer.Play(_mixer, FadeDuration);
            }

            public override void OnExit()
            {
                FadeDuration = _def.FadeInDuration;
            }

            public override void OnUpdate()
            {
                var relativeVelocityDir = Outer.Transform.InverseTransformDirection(Outer.PlanarVelocity);
                relativeVelocityDir.Normalize();

                if (Mathf.Approximately(relativeVelocityDir.magnitude, 0f))
                {
                    Owner.Stand.FadeDuration = _def.FadeOutDuration;
                    Owner.SetState(Owner.Stand);
                    return;
                }

                if (!Outer.Body.IsSprinting)
                {
                    Owner.Move.FadeDuration = _def.FadeOutDuration;
                    Owner.SetState(Owner.Move);
                    return;
                }

                var relativeDir = relativeVelocityDir /
                                  (Mathf.Abs(relativeVelocityDir.x) + Mathf.Abs(relativeVelocityDir.y) +
                                   Mathf.Abs(relativeVelocityDir.z));

                var forwardWeight = Mathf.Clamp01(relativeDir.z);
                var leftWeight = Mathf.Abs(Mathf.Clamp(relativeDir.x, -1f, 0f));
                var rightWeight = Mathf.Clamp01(relativeDir.x);

                // TODO: Time.deltaTime - use time manager or OnUpdate(deltaTime) 
                _forwardWeight = Mathf.Lerp(_forwardWeight, forwardWeight, _def.VelocityLerpSpeed * Time.deltaTime);
                _leftWeight = Mathf.Lerp(_leftWeight, leftWeight, _def.VelocityLerpSpeed * Time.deltaTime);
                _rightWeight = Mathf.Lerp(_rightWeight, rightWeight, _def.VelocityLerpSpeed * Time.deltaTime);

                _mixer.SetInputWeight(0, _forwardWeight);
                _mixer.SetInputWeight(1, _leftWeight);
                _mixer.SetInputWeight(2, _rightWeight);
            }
        }
    }
}
