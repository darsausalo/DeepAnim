using AurigaGames.Deep.Characters.Animations.Layers;
using AurigaGames.Deep.Core.StateSystem;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Characters.Animations.States
{
    public class InAirSubMachine : SubStateMachine<BaseAnimLayer.Instance, BaseStateMachine, InAirSubMachine>
    {
        public readonly JumpState Jump = new();
        public readonly JumpLoopState JumpLoop = new();
        public readonly FallState Fall = new();
        public readonly LandState Land = new();

        public override void Initialize(BaseAnimLayer.Instance outer)
        {
            base.Initialize(outer);

            Jump.Initialize(outer);
            JumpLoop.Initialize(outer);
            Fall.Initialize(outer);
            Land.Initialize(outer);
        }

        public override void OnEnter()
        {
            Start();
        }

        private void Start()
        {
            SetState(Outer.Movement.JumpConsumed ? Jump : Fall);
        }

        private void Stop(float fadeDuration)
        {
            // TODO: InAir->Swimming/Climbing
            Owner.Grounded.Stand.FadeDuration = fadeDuration;
            Owner.SetState(Owner.Grounded);
        }

        public class JumpState : State<BaseAnimLayer.Instance, InAirSubMachine>
        {
            private InAirDef.JumpDef _def;

            private AnimationClipPlayable _animL;
            private AnimationClipPlayable _animR;
            private AnimationClipPlayable _anim;

            public float FadeDuration;

            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                base.Initialize(outer);

                _def = Outer.Layer.InAir.Jump;

                _animL = AnimationClipPlayable.Create(Outer.Graph, _def.ClipL);
                _animL.SetApplyFootIK(false);
                _animL.SetDuration(_def.ClipL.length);
                _animL.SetSpeed(_def.PlaySpeed);

                _animR = AnimationClipPlayable.Create(Outer.Graph, _def.ClipR);
                _animR.SetApplyFootIK(false);
                _animR.SetDuration(_def.ClipR.length);
                _animR.SetSpeed(_def.PlaySpeed);

                FadeDuration = _def.FadeInDuration;
            }

            public override void OnEnter()
            {
                // TODO: determine foot
                _anim = _animL;
                _anim.SetTime(0.0f);
                Outer.BaseMixer.Play(_animL, FadeDuration);
            }

            public override void OnExit()
            {
                FadeDuration = _def.FadeInDuration;
            }

            public override void OnUpdate()
            {
                if (_anim.GetTime() >= _anim.GetDuration() || Outer.Movement.IsGrounded)
                {
                    Owner.SetState(Outer.Movement.IsGrounded ? Owner.Land : Owner.JumpLoop);
                }
            }
        }

        public class JumpLoopState : State<BaseAnimLayer.Instance, InAirSubMachine>
        {
            private InAirDef.JumpLoopDef _def;

            private AnimationClipPlayable _playable;

            public float FadeDuration;

            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                base.Initialize(outer);

                _def = Outer.Layer.InAir.JumpLoop;

                _playable = AnimationClipPlayable.Create(Outer.Graph, _def.Clip);
                _playable.SetApplyFootIK(false);
                _playable.SetSpeed(_def.PlaySpeed);

                FadeDuration = _def.FadeInDuration;
            }

            public override void OnEnter()
            {
                Outer.BaseMixer.Play(_playable, FadeDuration);
            }

            public override void OnExit()
            {
                FadeDuration = _def.FadeInDuration;
            }

            public override void OnUpdate()
            {
                // TODO: swimming (climbing?) ?
                if (Outer.Movement.IsGrounded)
                {
                    Owner.SetState(Owner.Land);
                }
            }
        }

        public class FallState : State<BaseAnimLayer.Instance, InAirSubMachine>
        {
            private InAirDef.FallDef _def;

            private AnimationClipPlayable _playable;

            public float FadeDuration;

            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                base.Initialize(outer);

                _def = Outer.Layer.InAir.Fall;

                _playable = AnimationClipPlayable.Create(Outer.Graph, _def.Clip);
                _playable.SetApplyFootIK(false);
                _playable.SetSpeed(_def.PlaySpeed);

                FadeDuration = _def.FadeInDuration;
            }

            public override void OnEnter()
            {
                Outer.BaseMixer.Play(_playable, FadeDuration);
            }

            public override void OnExit()
            {
                FadeDuration = _def.FadeInDuration;
            }

            public override void OnUpdate()
            {
                // TODO: swimming (climbing?) ?
                if (Outer.Movement.IsGrounded)
                {
                    Owner.SetState(Owner.Land);
                }
            }
        }

        public class LandState : State<BaseAnimLayer.Instance, InAirSubMachine>
        {
            private InAirDef.LandDef _def;

            private AnimationClipPlayable _anim;

            public float FadeDuration;

            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                base.Initialize(outer);

                _def = Outer.Layer.InAir.Land;

                _anim = AnimationClipPlayable.Create(Outer.Graph, _def.Clip);
                _anim.SetApplyFootIK(false);
                _anim.SetDuration(_def.Clip.length);
                _anim.SetSpeed(_def.PlaySpeed);
                _anim.SetSpeed(_def.PlaySpeed);

                FadeDuration = _def.FadeInDuration;
            }

            public override void OnEnter()
            {
                _anim.SetTime(0.0f);
                Outer.BaseMixer.Play(_anim, FadeDuration);
            }

            public override void OnExit()
            {
                FadeDuration = _def.FadeInDuration;
            }

            public override void OnUpdate()
            {
                if (_anim.GetTime() >= _anim.GetDuration() - 0.2f ||
                    _anim.GetTime() >= Outer.Movement.JumpPostGroundingGraceTime &&
                    (!Outer.Movement.IsGrounded || Outer.Movement.HasMoveInput))
                {
                    if (!Outer.Movement.IsGrounded)
                    {
                        Owner.Start();
                    }
                    else
                    {
                        Owner.Stop(_def.FadeOutDuration);
                    }
                }
            }
        }
    }
}