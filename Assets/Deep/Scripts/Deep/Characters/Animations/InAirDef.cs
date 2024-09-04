using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AurigaGames.Deep.Characters.Animations
{
    [Serializable]
    public class InAirDef
    {
        [Title("States")] public JumpDef Jump;
        public JumpLoopDef JumpLoop;
        public FallDef Fall;
        public LandDef Land;

        [Serializable]
        public class JumpDef
        {
            [Title("Animations")]
            [LabelText("Left")]
            public AnimationClip ClipL;

            [LabelText("Right")] public AnimationClip ClipR;

            public float PlaySpeed = 1.2f;

            [Title("Transition")]
            [LabelText("Fade In")]
            public float FadeInDuration = 0.2f;

            [LabelText("Fade Out")] public float FadeOutDuration = 0.2f;
        }

        [Serializable]
        public class JumpLoopDef
        {
            [Title("Animations")]
            [LabelText("Loop")]
            public AnimationClip Clip;

            public float PlaySpeed = 1.2f;

            [Title("Transition")]
            [LabelText("Fade In")]
            public float FadeInDuration = 0.2f;

            [LabelText("Fade Out")] public float FadeOutDuration = 0.2f;
        }

        [Serializable]
        public class FallDef
        {
            [Title("Animations")]
            [LabelText("Fall")]
            public AnimationClip Clip;

            public float PlaySpeed = 1.2f;

            [Title("Transition")]
            [LabelText("Fade In")]
            public float FadeInDuration = 0.2f;

            [LabelText("Fade Out")] public float FadeOutDuration = 0.2f;
        }

        [Serializable]
        public class LandDef
        {
            [Title("Animations")]
            [LabelText("Land")]
            public AnimationClip Clip;

            public float PlaySpeed = 1.2f;

            [Title("Transition")]
            [LabelText("Fade In")]
            public float FadeInDuration = 0.2f;

            [LabelText("Fade Out")] public float FadeOutDuration = 0.2f;
        }
    }
}