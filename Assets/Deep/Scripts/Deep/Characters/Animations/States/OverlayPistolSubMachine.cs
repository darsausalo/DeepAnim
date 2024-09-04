using AurigaGames.Deep.Characters.Animations.Layers;
using AurigaGames.Deep.Core.StateSystem;
using UnityEngine;
using UnityEngine.Animations;

namespace AurigaGames.Deep.Characters.Animations.States
{
    public class OverlayPistolSubMachine :
        SubStateMachine<BaseAnimLayer.Instance, OverlayStateMachine, OverlayPistolSubMachine>
    {
        public readonly RelaxedState Relaxed = new();
        public readonly ReadyState Ready = new();
        public readonly AimState Aim = new();
        
        public override void Initialize(BaseAnimLayer.Instance outer)
        {
            base.Initialize(outer);
            
            Relaxed.Initialize(outer);
            Ready.Initialize(outer);
            Aim.Initialize(outer);
            
            OnEnter();
        }

        public override void OnEnter()
        {
            SetState(Relaxed);
        }

        public class RelaxedState :
            OverlayStateMachine.OverlayState<OverlayDef.WeaponDef.RelaxedDef, OverlayPistolSubMachine>
        {
            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                Def = outer.Layer.Overlay.Pistol.Relaxed;
                AimSweep = AnimationClipPlayable.Create(outer.Graph, outer.Layer.Overlay.Default.AimSweepClip);
                AimSweep.SetApplyFootIK(false);

                base.Initialize(outer);
            }

            public override void OnUpdate()
            {
                switch (Outer.Body.Overlay)
                {
                    case OverlayKind.Default:
                        Owner.Owner.SetState(Owner.Owner.Default);
                        return;
                    case OverlayKind.Rifle:
                        Owner.Owner.SetState(Owner.Owner.Rifle);
                        return;
                }
                if (Outer.Body.Aim)
                {
                    Owner.Aim.FadeDuration = 0.1f;
                    Owner.SetState(Owner.Aim);
                    return;
                }
                base.OnUpdate();
            }
        }

        public class ReadyState :
            OverlayStateMachine.OverlayState<OverlayDef.WeaponDef.ReadyDef, OverlayPistolSubMachine>
        {
            private float _enterTime;
            
            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                Def = outer.Layer.Overlay.Pistol.Ready;
                AimSweep = AnimationClipPlayable.Create(outer.Graph, outer.Layer.Overlay.Default.AimSweepClip);
                AimSweep.SetApplyFootIK(false);

                base.Initialize(outer);
            }

            public override void OnEnter()
            {
                base.OnEnter();

                _enterTime = Time.time;
            }

            public override void OnUpdate()
            {
                switch (Outer.Body.Overlay)
                {
                    case OverlayKind.Default:
                        Owner.Owner.SetState(Owner.Owner.Default);
                        return;
                    case OverlayKind.Rifle:
                        Owner.Owner.SetState(Owner.Owner.Rifle);
                        return;
                }
                if (Outer.Body.Aim)
                {
                    Owner.Aim.FadeDuration = 0.1f;
                    Owner.SetState(Owner.Aim);
                    return;
                }

                if (Time.time - _enterTime > 3f) // TODO: sprint/air
                {
                    Owner.Relaxed.FadeDuration = 0.75f;
                    Owner.SetState(Owner.Relaxed);
                    return;
                }

                base.OnUpdate();
            }
        }

        public class AimState :
            OverlayStateMachine.OverlayState<OverlayDef.WeaponDef.AimDef, OverlayPistolSubMachine>
        {
            public override void Initialize(BaseAnimLayer.Instance outer)
            {
                Def = outer.Layer.Overlay.Pistol.Aim;
                AimSweep = AnimationClipPlayable.Create(outer.Graph, Def.AimSweepClip);
                AimSweep.SetApplyFootIK(false);

                base.Initialize(outer);
            }

            public override void OnUpdate()
            {
                switch (Outer.Body.Overlay)
                {
                    case OverlayKind.Default:
                        Owner.Owner.SetState(Owner.Owner.Default);
                        return;
                    case OverlayKind.Rifle:
                        Owner.Owner.SetState(Owner.Owner.Rifle);
                        return;
                }
                if (!Outer.Body.Aim)
                {
                    Owner.Ready.FadeDuration = 1f; // TODO: sprint
                    Owner.SetState(Owner.Ready);
                    return;
                }
                base.OnUpdate();
            }
        }
    }
}
