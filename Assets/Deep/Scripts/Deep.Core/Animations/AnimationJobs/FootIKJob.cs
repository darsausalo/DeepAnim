using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;

namespace AurigaGames.Deep.Core.Animations
{
    public struct FootIKJob : IAnimationJob
    {
        public FootIKSettings Settings;

        public Vector2 IKOffset;
        public Vector3 LeftNormal;
        public Vector3 RightNormal;

        public float Weight;

        public void ProcessAnimation(AnimationStream stream)
        {
            if (Weight <= 0f) return;
            
            if (stream.isHumanStream)
            {
                var human = stream.AsHuman();

                var leftPos = human.GetGoalPosition(AvatarIKGoal.LeftFoot);
                var rightPos = human.GetGoalPosition(AvatarIKGoal.RightFoot);
                var leftRot = human.GetGoalRotation(AvatarIKGoal.LeftFoot);
                var rightRot = human.GetGoalRotation(AvatarIKGoal.RightFoot);

                // setup IK pos 
                var leftIKOffset = IKOffset.x * Weight;
                var rightIKOffset = IKOffset.y * Weight;

                var leftIKPos = new Vector3(leftPos.x, leftPos.y + leftIKOffset, leftPos.z);
                var rightIKPos = new Vector3(rightPos.x, rightPos.y + rightIKOffset, rightPos.z);

                var leftFinalPos = Vector3.Lerp(leftPos, leftIKPos, Weight);
                var rightFinalPos = Vector3.Lerp(rightPos, rightIKPos, Weight);

                // setup IK rot
                var leftUp = leftRot * Vector3.up;
                var leftRotAxis = Vector3.Cross(leftUp, LeftNormal);
                var leftAngle = Mathf.Clamp(Vector3.Angle(leftUp, LeftNormal), -Settings.MaxRotationOffset, Settings.MaxRotationOffset);
                var leftIKRot = Quaternion.AngleAxis(leftAngle, leftRotAxis) * leftRot;

                var rightUp = rightRot * Vector3.up;
                var rightRotAxis = Vector3.Cross(rightUp, RightNormal);
                var rightAngle = Mathf.Clamp(Vector3.Angle(rightUp, RightNormal), -Settings.MaxRotationOffset, Settings.MaxRotationOffset);
                var rightIKRot = Quaternion.AngleAxis(rightAngle, rightRotAxis) * rightRot;

                var leftFinalRot = Quaternion.Lerp(leftRot, leftIKRot, Weight);
                var rightFinalRot = Quaternion.Lerp(rightRot, rightIKRot, Weight);

                // setup body offset
                var hipHeightOffset = (leftIKOffset + rightIKOffset) * 0.5f;
                var forwardBackBias = (leftIKOffset - rightIKOffset) * Settings.WeightShiftHorizontal;

                hipHeightOffset += Mathf.Abs(leftIKOffset - rightIKOffset) * Settings.WeightShiftVertical;
                var standAngle = Quaternion.AngleAxis(Settings.WeightShiftAngle, Vector3.up) * Vector3.forward;
                // human.bodyLocalPosition += new Vector3(standAngle.x * forwardBackBias, hipHeightOffset,
                //     standAngle.z * forwardBackBias);
                human.bodyLocalPosition += new Vector3(0f, hipHeightOffset, 0f);

                // update goals
                human.SetGoalPosition(AvatarIKGoal.LeftFoot, leftFinalPos);
                human.SetGoalPosition(AvatarIKGoal.RightFoot, rightFinalPos);
                human.SetGoalRotation(AvatarIKGoal.LeftFoot, leftFinalRot);
                human.SetGoalRotation(AvatarIKGoal.RightFoot, rightFinalRot);

                human.SetGoalWeightPosition(AvatarIKGoal.LeftFoot, 1f);
                human.SetGoalWeightPosition(AvatarIKGoal.RightFoot, 1f);
                human.SetGoalWeightRotation(AvatarIKGoal.LeftFoot, 1f);
                human.SetGoalWeightRotation(AvatarIKGoal.RightFoot, 1f);
            }
        }

        public void ProcessRootMotion(AnimationStream stream) { }
    }

    [Serializable]
    public struct FootIKSettings
    {
        [Title("Raycast")]
        [Range(0f, 1f)] public float EmitRayOffset; // 0.5
        [Range(0f, 20f)] public float MaxRayDistance; // 0.75

        [Title("Footstep")]
        [Range(0f, 1f)] public float MaxStepSize;
        [Range(5f, 50f)] public float MaxRotationOffset;

        [Title("")]
        [Range(-90f, 90f)] public float WeightShiftAngle;
        [Range(-1f, 1f)] public float WeightShiftHorizontal;
        [Range(-1f, 1f)] public float WeightShiftVertical;

        [Title("Blending")]
        [Range(0f, 50f), LabelText("Weight Lerp Speed")] public float WeightLerpSpeed;
        [Range(0f, 50f), LabelText("Offset Lerp Speed")] public float OffsetLerpSpeed;
    }
}
