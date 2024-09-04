using AurigaGames.Deep.Characters.Animations.Layers;
using AurigaGames.Deep.Core;
using AurigaGames.Deep.Core.Animations;
using AurigaGames.Deep.Core.StateSystem;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Characters.Animations.States
{
    public class OverlayStateMachine : StateMachine<BaseAnimLayer.Instance, OverlayStateMachine>
    {
        public readonly DefaultState Default = new();
        public readonly OverlayPistolSubMachine Pistol = new();
        public readonly OverlayRifleSubMachine Rifle = new();

        public override void Initialize(BaseAnimLayer.Instance outer)
        {
            base.Initialize(outer);

            Default.Initialize(outer);
            Pistol.Initialize(outer);
            Rifle.Initialize(outer);

            OnEnter();
        }

        public override void OnEnter()
        {
            SetState(Default);
        }

        public class DefaultState : OverlayState<OverlayDef.DefaultDef, OverlayStateMachine>
        {
            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                Def = outer.Layer.Overlay.Default;
                AimSweep = AnimationClipPlayable.Create(outer.Graph, Def.AimSweepClip);

                base.Initialize(outer);
            }

            public override void OnUpdate()
            {
                switch (Outer.Body.Overlay)
                {
                    case OverlayKind.Pistol:
                        Owner.SetState(Owner.Pistol);
                        return;
                    case OverlayKind.Rifle:
                        Owner.SetState(Owner.Rifle);
                        return;
                    default:
                        base.OnUpdate();
                        break;
                }
            }
        }

        public abstract class OverlayState<TDef, TOwner> : State<BaseAnimLayer.Instance, TOwner>
            where TDef : OverlayDef.OverlayBaseDef
            where TOwner : StateMachine<BaseAnimLayer.Instance, TOwner>
        {
            protected TDef Def;

            protected AnimationClipPlayable AnimStand;
            protected AnimationClipPlayable AnimMove;

            protected AnimationClipPlayable AimSweep;
            protected AnimationMixerPlayable Mixer;
            protected AnimationLayerMixerPlayable LayerMixer;
            protected MixerTransition<AnimationMixerPlayable, MixerPort> Transition;

            public float FadeDuration = 0.2f;

            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                base.Initialize(outer);

                AnimStand = AnimationClipPlayable.Create(Outer.Graph, Def.StandClip);
                AnimStand.SetApplyFootIK(true);
                if (Def.StandPose)
                {
                    AnimStand.Pause();
                }

                AnimMove = AnimationClipPlayable.Create(Outer.Graph, Def.MoveClip);
                AnimMove.SetApplyFootIK(true);
                if (Def.MovePose)
                {
                    AnimMove.Pause();
                }

                Mixer = AnimationMixerPlayable.Create(Outer.Graph);
                Mixer.AddInput(AnimStand, 0, 1f);
                Mixer.AddInput(AnimMove, 0);

                AimSweep.SetApplyFootIK(false);
                AimSweep.Pause();
                AimSweep.SetTime(MathHelper.GetMappedRangeValueClamped(-90f, 90f, 0f, 1f, Outer.Pitch));

                LayerMixer = AnimationLayerMixerPlayable.Create(Outer.Graph);
                LayerMixer.AddInput(Mixer, 0, 1f);
                LayerMixer.AddInput(AimSweep, 0, 1f);
                LayerMixer.SetLayerAdditive(1, true);
                LayerMixer.SetLayerMaskFromAvatarMask(1, Outer.Layer.Overlay.AimMask);

                Transition = new MixerTransition<AnimationMixerPlayable, MixerPort>(Mixer);
            }

            public override void OnEnter()
            {
                Outer.OverlayMixer.Play(LayerMixer, FadeDuration, Def.OverlayWeights);
            }

            public override void OnExit()
            {
                FadeDuration = 0.2f;
            }

            public override void OnUpdate()
            {
                var sweepTime = MathHelper.GetMappedRangeValueClamped(-90f, 90f, 0f, 1f, Outer.Pitch);
                AimSweep.SetTime(sweepTime);

                var port = Outer.Movement.Velocity.magnitude > Mathf.Epsilon ? MixerPort.Move : MixerPort.Stand;
                Transition.Update(port, 0.2f, Time.deltaTime);
            }

            protected enum MixerPort
            {
                Stand,
                Move
            }
        }
    }
}
