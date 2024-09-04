using System;
using AurigaGames.Deep.Core.Animations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AurigaGames.Deep.Characters.Animations
{
    [Serializable]
    public class OverlayDef
    {
        [Title("States")]
        public DefaultDef Default;
        public WeaponDef Pistol;
        public WeaponDef Rifle;

        [Title("Aiming")]
        [LabelText("Mask")] public AvatarMask AimMask;

        [Serializable]
        public abstract class OverlayBaseDef
        {
            [HorizontalGroup("Stand"), LabelText("Stand")]
            public AnimationClip StandClip;
            [HorizontalGroup("Stand", 0.15f), LabelText("Pose"), LabelWidth(30)]
            public bool StandPose = true;

            [HorizontalGroup("Move"), LabelText("Move")]
            public AnimationClip MoveClip;
            [HorizontalGroup("Move", 0.15f), LabelText("Pose"), LabelWidth(30)]
            public bool MovePose = true;

            [LabelText("Aim Sweep")] public AnimationClip AimSweepClip;

            [LabelText("Layering")] public OverlayWeights OverlayWeights;
        }

        [Serializable]
        public class DefaultDef : OverlayBaseDef { }

        [Serializable]
        public class WeaponDef
        {
            [Title("States")]
            public RelaxedDef Relaxed;
            public ReadyDef Ready;
            public AimDef Aim;

            [Serializable]
            public class RelaxedDef : OverlayBaseDef { }

            [Serializable]
            public class ReadyDef : OverlayBaseDef { }

            [Serializable]
            public class AimDef : OverlayBaseDef { }
        }
    }
}
