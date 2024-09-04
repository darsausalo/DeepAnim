using System;
using AurigaGames.Deep.Core;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AurigaGames.Deep.Characters.Animations
{
    [Serializable]
    public class GroundedDef
    {
        [Title("States")]
        public StandDef Stand;
        public MoveDef Move;
        public SprintDef Sprint;

        [Serializable]
        public class StandDef
        {
            [Title("Animations")]
            [Required, LabelText("Pose")]
            public AnimationClip PoseClip;

            [Required, LabelText("Transition L->R")]
            public AnimationClip TransitionLClip;

            [Required, LabelText("Transition R->L")]
            public AnimationClip TransitionRClip;

            [Required, LabelText("Turn Left")] public AnimationClip TurnLClip;
            [InlineButton("ExtractCurveL", "E")] public AnimationCurve TurnLCurve;

            [Required, LabelText("Turn Right")] public AnimationClip TurnRClip;
            [InlineButton("ExtractCurveR", "E")] public AnimationCurve TurnRCurve;

            [Title("Rotation")]

            [Range(45f, 90f)]
            public float Threshold = 50.0f;
            [Range(50f, 120f)]
            [LabelText("Limit")] public float TurnLimit = 80f;

            [Range(90f, 270f)] public float MinYawSpeed = 90f;
            [Range(90f, 270f)] public float MaxYawSpeed = 270f;

            [Range(1f, 5f)] public float MinPlaySpeed = 1.4f;
            [Range(1f, 5f)] public float MaxPlaySpeed = 4f;

            [Range(1f, 50f)] public float SmoothSpeed = 25f;

            [Title("Transition")]
            [Range(0f, 1f), LabelText("Fade In"), SuffixLabel("s", true)]
            public float FadeInDuration = 0.2f;

            [Range(0f, 1f), LabelText("Fade Out"), SuffixLabel("s", true)]
            public float FadeOutDuration = 0.2f;

            [Range(0f, 1f), LabelText("Turn -> Stand"), SuffixLabel("s", true)]
            public float TurnToStandDuration = 0.1f;

            [Range(0f, 1f), LabelText("Stand -> Turn"), SuffixLabel("s", true)]
            public float StandToTurnDuration = 0.1f;

#if UNITY_EDITOR

            [UsedImplicitly]
            public void ExtractCurveL()
            {
                if (TurnLClip)
                {
                    TurnLCurve = CreateYawSpeedCurve(TurnLClip);
                }
            }

            [UsedImplicitly]
            public void ExtractCurveR()
            {
                if (TurnRClip)
                {
                    TurnRCurve = CreateYawSpeedCurve(TurnRClip);
                }
            }

            private static AnimationCurve CreateYawSpeedCurve(AnimationClip clip)
            {
                AnimationCurve curveYawSpeed = null;

                AnimationCurve curveX = null;
                AnimationCurve curveY = null;
                AnimationCurve curveZ = null;
                AnimationCurve curveW = null;

                var bindings = AnimationUtility.GetCurveBindings(clip);
                foreach (var binding in bindings)
                {
                    if (binding.propertyName.StartsWith("RootQ"))
                    {
                        if (binding.propertyName.EndsWith(".x"))
                        {
                            curveX = AnimationUtility.GetEditorCurve(clip, binding);
                        }
                        else if (binding.propertyName.EndsWith(".y"))
                        {
                            curveY = AnimationUtility.GetEditorCurve(clip, binding);
                        }
                        else if (binding.propertyName.EndsWith(".z"))
                        {
                            curveZ = AnimationUtility.GetEditorCurve(clip, binding);
                        }
                        else if (binding.propertyName.EndsWith(".w"))
                        {
                            curveW = AnimationUtility.GetEditorCurve(clip, binding);
                        }
                    }
                }

                if (curveX != null && curveY != null && curveZ != null && curveW != null)
                {
                    curveYawSpeed = new AnimationCurve();

                    var numFrames = Mathf.CeilToInt(clip.frameRate * clip.length);
                    for (var frame = 0; frame < numFrames; ++frame)
                    {
                        var nextFrame = frame + 1;

                        var frameTime = GetTimeAtFrame(clip, numFrames, frame);
                        var nextFrameTime = GetTimeAtFrame(clip, numFrames, nextFrame);

                        var frameQ = new Quaternion(curveX.Evaluate(frameTime), curveY.Evaluate(frameTime),
                            curveZ.Evaluate(frameTime), curveW.Evaluate(frameTime));
                        var nextFrameQ = new Quaternion(curveX.Evaluate(nextFrameTime), curveY.Evaluate(nextFrameTime),
                            curveZ.Evaluate(nextFrameTime), curveW.Evaluate(nextFrameTime));

                        var yawSpeed = MathHelper.NormalizeAngle(nextFrameQ.eulerAngles.y - frameQ.eulerAngles.y) *
                                       clip.frameRate;

                        curveYawSpeed.AddKey(frameTime, yawSpeed);
                    }

                    curveYawSpeed.preWrapMode = WrapMode.Loop;
                    curveYawSpeed.postWrapMode = WrapMode.Loop;
                }

                return curveYawSpeed;
            }

            private static float GetTimeAtFrame(AnimationClip clip, int numFrames, int frame)
            {
                var frameTime = numFrames > 1 ? clip.length / (numFrames - 1) : 0.0f;
                return Mathf.Clamp(frameTime * frame, 0.0f, clip.length);
            }
#endif
        }

        [Serializable]
        public class MoveDef
        {
            [BoxGroup("Forward")]
            [Required, LabelText("Pose")]
            public AnimationClip ForwardPoseClip;

            [BoxGroup("Forward")]
            [Required, LabelText("Clip")]
            public AnimationClip ForwardAnimClip;

            [BoxGroup("Backward")]
            [Required, LabelText("Pose")]
            public AnimationClip BackwardPoseClip;

            [BoxGroup("Backward")]
            [Required, LabelText("Clip")]
            public AnimationClip BackwardAnimClip;

            [BoxGroup("Left Forward")]
            [Required, LabelText("Pose")]
            public AnimationClip LeftForwardPoseClip;

            [BoxGroup("Left Forward")]
            [Required, LabelText("Clip")]
            public AnimationClip LeftForwardAnimClip;

            [BoxGroup("Left Backward")]
            [Required, LabelText("Pose")]
            public AnimationClip LeftBackwardPoseClip;

            [BoxGroup("Left Backward")]
            [Required, LabelText("Clip")]
            public AnimationClip LeftBackwardAnimClip;

            [BoxGroup("Right Forward")]
            [Required, LabelText("Pose")]
            public AnimationClip RightForwardPoseClip;

            [BoxGroup("Right Forward")]
            [Required, LabelText("Clip")]
            public AnimationClip RightForwardAnimClip;

            [BoxGroup("Right Backward")]
            [Required, LabelText("Pose")]
            public AnimationClip RightBackwardPoseClip;

            [BoxGroup("Right Backward")]
            [Required, LabelText("Clip")]
            public AnimationClip RightBackwardAnimClip;

            [Title("Direction Blending")] public float PlaySpeed = 1.25f;
            [MinValue(0f)] public float MinMoveSpeed;
            [MinValue(0f)] public float MaxMoveSpeed = 3.5f;
            [MinValue(0f)] public float StrideLerpSpeed = 12.0f;
            [MinValue(0f)] public float VelocityLerpSpeed = 12.0f;
            [MinValue(0f)] public float SprintThreshold = 4.5f; // TODO: use attributes to determine sprint threshold?

            [MinValue(0f), LabelText("Fade Duration")]
            public float DirectionFadeDuration = 0.3f;

            [Title("Transition")]
            [LabelText("Fade In"), SuffixLabel("s", true)]
            public float FadeInDuration = 0.2f;

            [LabelText("Fade Out"), SuffixLabel("s", true)]
            public float FadeOutDuration = 0.2f;
        }
        
        [Serializable]
        public class SprintDef
        {
            [Title("Animations")]
            [Required, LabelText("Forward")]
            public AnimationClip ForwardClip;

            [Required, LabelText("Left")]
            public AnimationClip LeftClip;

            [Required, LabelText("Right")]
            public AnimationClip RightClip;

            [Title("Direction blending")]
            public float PlaySpeed = 1.2f;

            [MinValue(0f)] public float VelocityLerpSpeed = 20f;
            
            [Title("Transition")]
            [LabelText("Fade In"), SuffixLabel("s", true)]
            public float FadeInDuration = 0.2f;

            [LabelText("Fade Out"), SuffixLabel("s", true)]
            public float FadeOutDuration = 0.2f;
        }
    }
}
