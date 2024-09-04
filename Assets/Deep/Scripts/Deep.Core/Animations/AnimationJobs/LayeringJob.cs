using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace AurigaGames.Deep.Core.Animations
{
    [BurstCompile]
    public struct LayeringJob : IAnimationJob
    {
        internal NativeArray<TransformStreamHandle> Bones;
        internal NativeArray<float> Weights;
        internal NativeArray<float> Overrides;

        internal NativeArray<Vector3> Positions;
        internal NativeArray<Quaternion> Rotations;

        internal int OffsetBody;
        internal int OffsetArms;
        internal int OffsetHands;
        internal int OffsetLegs;

        public void ProcessAnimation(AnimationStream stream)
        {
            var streamBase = stream.GetInputStream(0);
            var streamLoco = stream.GetInputStream(1);
            var streamOver = stream.GetInputStream(2);

            for (var i = 0; i < OffsetBody; ++i)
            {
                var bone = Bones[i];
                var weight = Weights[i];

                bone.GetGlobalTR(streamBase, out var posBase, out var rotBase);
                bone.GetGlobalTR(streamLoco, out var posLoco, out var rotLoco);
                bone.GetGlobalTR(streamOver, out var posOver, out var rotOver);

                var posRes = posOver + (posLoco - posBase);
                var rotRes = rotLoco * Quaternion.Inverse(rotBase) * rotOver;
                rotRes.Normalize();

                Positions[i] = posRes;
                Rotations[i] = rotRes;

                bone.SetGlobalTR(stream,
                    i == 0 ? posRes : Vector3.Lerp(posLoco, posRes, weight),
                    Quaternion.Slerp(rotLoco, rotRes, weight), false);
            }

            for (var i = 0; i < OffsetBody; ++i)
            {
                var bone = Bones[i];
                var weight = Weights[i];

                bone.GetGlobalTR(streamLoco, out var posLoco, out var rotLoco);
                bone.SetGlobalTR(stream,
                    i == 0 ? Positions[i] : Vector3.Lerp(posLoco, Positions[i], weight),
                    Quaternion.Slerp(rotLoco, Rotations[i], weight), false);
            }

            for (var i = OffsetBody; i < OffsetHands; ++i)
            {
                var bone = Bones[i];
                var weight = Weights[i];

                var posLoco = bone.GetLocalPosition(streamLoco);
                var posOver = bone.GetLocalPosition(streamOver);

                var rotLoco = bone.GetLocalRotation(streamLoco);
                var rotOver = bone.GetLocalRotation(streamOver);

                bone.SetLocalPosition(stream, Vector3.Lerp(posLoco, posOver, weight));
                bone.SetLocalRotation(stream, Quaternion.Slerp(rotLoco, rotOver, weight));
            }

            for (var i = OffsetBody; i < OffsetArms; ++i)
            {
                var bone = Bones[i];
                var weight = Overrides[i];

                bone.GetGlobalTR(stream, out var posLoco, out var rotLoco);
                bone.GetGlobalTR(streamOver, out var posOver, out var rotOver);

                bone.SetGlobalTR(stream,
                    Vector3.Lerp(posLoco, posOver, weight),
                    Quaternion.Slerp(rotLoco, rotOver, weight), false);
            }

            {
                var weight = Weights[OffsetLegs];

                var humanLoco = streamLoco.AsHuman();
                var humanOver = streamOver.AsHuman();
                var human = stream.AsHuman();

                var rightPosLoco = humanLoco.GetGoalPosition(AvatarIKGoal.RightFoot);
                var rightPosOver = humanOver.GetGoalPosition(AvatarIKGoal.RightFoot);
                human.SetGoalPosition(AvatarIKGoal.RightFoot, Vector3.Lerp(rightPosLoco, rightPosOver, weight));
                var rightRotLoco = humanLoco.GetGoalRotation(AvatarIKGoal.RightFoot);
                var rightRotOver = humanOver.GetGoalRotation(AvatarIKGoal.RightFoot);
                human.SetGoalRotation(AvatarIKGoal.RightFoot, Quaternion.Slerp(rightRotLoco, rightRotOver, weight));

                var leftPosLoco = humanLoco.GetGoalPosition(AvatarIKGoal.LeftFoot);
                var leftPosOver = humanOver.GetGoalPosition(AvatarIKGoal.LeftFoot);
                human.SetGoalPosition(AvatarIKGoal.LeftFoot, Vector3.Lerp(leftPosLoco, leftPosOver, weight));
                var leftRotLoco = humanLoco.GetGoalRotation(AvatarIKGoal.LeftFoot);
                var leftRotOver = humanOver.GetGoalRotation(AvatarIKGoal.LeftFoot);
                human.SetGoalRotation(AvatarIKGoal.LeftFoot, Quaternion.Slerp(leftRotLoco, leftRotOver, weight));
            }
        }

        public void ProcessRootMotion(AnimationStream stream) { }
    }

    internal struct OverlayTransformGroup
    {
        public int First;
        public int Last;
    }

    public static class LayeringJobBinder
    {
        private static readonly List<HumanBodyBones> BodyBones = new();

        private static readonly OverlayTransformGroup HipsGroup;
        private static readonly OverlayTransformGroup LegsGroup;
        private static readonly OverlayTransformGroup SpineGroup;
        private static readonly OverlayTransformGroup HeadGroup;
        private static readonly OverlayTransformGroup ArmLGroup;
        private static readonly OverlayTransformGroup ArmRGroup;
        private static readonly OverlayTransformGroup HandLGroup;
        private static readonly OverlayTransformGroup HandRGroup;

        static LayeringJobBinder()
        {
            // Hips
            HipsGroup.First = BodyBones.Count;

            BodyBones.Add(HumanBodyBones.Hips);

            HipsGroup.Last = BodyBones.Count;

            // Legs
            LegsGroup.First = BodyBones.Count;

            BodyBones.Add(HumanBodyBones.LeftUpperLeg);
            BodyBones.Add(HumanBodyBones.LeftLowerLeg);
            BodyBones.Add(HumanBodyBones.LeftFoot);
            BodyBones.Add(HumanBodyBones.LeftToes);

            BodyBones.Add(HumanBodyBones.RightUpperLeg);
            BodyBones.Add(HumanBodyBones.RightLowerLeg);
            BodyBones.Add(HumanBodyBones.RightFoot);
            BodyBones.Add(HumanBodyBones.RightToes);

            LegsGroup.Last = BodyBones.Count;

            // Spine
            SpineGroup.First = BodyBones.Count;

            BodyBones.Add(HumanBodyBones.Spine);
            BodyBones.Add(HumanBodyBones.Chest);
            BodyBones.Add(HumanBodyBones.UpperChest);

            SpineGroup.Last = BodyBones.Count;

            // Head
            HeadGroup.First = BodyBones.Count;

            BodyBones.Add(HumanBodyBones.Neck);
            BodyBones.Add(HumanBodyBones.Head);

            HeadGroup.Last = BodyBones.Count;

            // ArmL
            ArmLGroup.First = BodyBones.Count;

            BodyBones.Add(HumanBodyBones.LeftShoulder);
            BodyBones.Add(HumanBodyBones.LeftUpperArm);
            BodyBones.Add(HumanBodyBones.LeftLowerArm);
            BodyBones.Add(HumanBodyBones.LeftHand);

            ArmLGroup.Last = BodyBones.Count;

            // ArmR
            ArmRGroup.First = BodyBones.Count;

            BodyBones.Add(HumanBodyBones.RightShoulder);
            BodyBones.Add(HumanBodyBones.RightUpperArm);
            BodyBones.Add(HumanBodyBones.RightLowerArm);
            BodyBones.Add(HumanBodyBones.RightHand);

            ArmRGroup.Last = BodyBones.Count;

            // Hands

            HandLGroup.First = BodyBones.Count;

            BodyBones.Add(HumanBodyBones.LeftThumbProximal);
            BodyBones.Add(HumanBodyBones.LeftThumbIntermediate);
            BodyBones.Add(HumanBodyBones.LeftThumbDistal);
            BodyBones.Add(HumanBodyBones.LeftIndexProximal);
            BodyBones.Add(HumanBodyBones.LeftIndexIntermediate);
            BodyBones.Add(HumanBodyBones.LeftIndexDistal);
            BodyBones.Add(HumanBodyBones.LeftMiddleProximal);
            BodyBones.Add(HumanBodyBones.LeftMiddleIntermediate);
            BodyBones.Add(HumanBodyBones.LeftMiddleDistal);
            BodyBones.Add(HumanBodyBones.LeftRingProximal);
            BodyBones.Add(HumanBodyBones.LeftRingIntermediate);
            BodyBones.Add(HumanBodyBones.LeftRingDistal);
            BodyBones.Add(HumanBodyBones.LeftLittleProximal);
            BodyBones.Add(HumanBodyBones.LeftLittleIntermediate);
            BodyBones.Add(HumanBodyBones.LeftLittleDistal);

            HandLGroup.Last = BodyBones.Count;

            HandRGroup.First = BodyBones.Count;

            BodyBones.Add(HumanBodyBones.RightThumbProximal);
            BodyBones.Add(HumanBodyBones.RightThumbIntermediate);
            BodyBones.Add(HumanBodyBones.RightThumbDistal);
            BodyBones.Add(HumanBodyBones.RightIndexProximal);
            BodyBones.Add(HumanBodyBones.RightIndexIntermediate);
            BodyBones.Add(HumanBodyBones.RightIndexDistal);
            BodyBones.Add(HumanBodyBones.RightMiddleProximal);
            BodyBones.Add(HumanBodyBones.RightMiddleIntermediate);
            BodyBones.Add(HumanBodyBones.RightMiddleDistal);
            BodyBones.Add(HumanBodyBones.RightRingProximal);
            BodyBones.Add(HumanBodyBones.RightRingIntermediate);
            BodyBones.Add(HumanBodyBones.RightRingDistal);
            BodyBones.Add(HumanBodyBones.RightLittleProximal);
            BodyBones.Add(HumanBodyBones.RightLittleIntermediate);
            BodyBones.Add(HumanBodyBones.RightLittleDistal);

            HandRGroup.Last = BodyBones.Count;
        }

        public static LayeringJob Create(Animator animator)
        {
            var job = new LayeringJob
            {
                Bones = new NativeArray<TransformStreamHandle>(BodyBones.Count, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory),
                Weights = new NativeArray<float>(BodyBones.Count, Allocator.Persistent),
                Overrides = new NativeArray<float>(BodyBones.Count, Allocator.Persistent),
                Positions = new NativeArray<Vector3>(BodyBones.Count, Allocator.Persistent),
                Rotations = new NativeArray<Quaternion>(BodyBones.Count, Allocator.Persistent),
                OffsetBody = HeadGroup.Last,
                OffsetArms = ArmRGroup.Last,
                OffsetHands = HandRGroup.Last,
                OffsetLegs = LegsGroup.First,
            };

            for (var i = 0; i < BodyBones.Count; ++i)
            {
                job.Bones[i] = animator.BindStreamTransform(animator.GetBoneTransform(BodyBones[i]));
            }

            return job;
        }

        public static void Destroy(this LayeringJob job)
        {
            job.Rotations.Dispose();
            job.Positions.Dispose();
            job.Overrides.Dispose();
            job.Weights.Dispose();
            job.Bones.Dispose();
        }

        public static void Update(this LayeringJob job, in OverlayWeights data)
        {
            for (var i = HipsGroup.First; i < HipsGroup.Last; ++i)
            {
                job.Weights[i] = data.Hips;
            }

            for (var i = LegsGroup.First; i < LegsGroup.Last; ++i)
            {
                job.Weights[i] = data.Legs;
            }

            for (var i = SpineGroup.First; i < SpineGroup.Last; ++i)
            {
                job.Weights[i] = data.Spine;
            }

            for (var i = HeadGroup.First; i < HeadGroup.Last; ++i)
            {
                job.Weights[i] = data.Head;
            }

            for (var i = ArmLGroup.First; i < ArmLGroup.Last; ++i)
            {
                job.Weights[i] = data.ArmL;
            }

            for (var i = ArmLGroup.First; i < ArmLGroup.Last; ++i)
            {
                job.Overrides[i] = data.ArmLOverride;
            }

            for (var i = ArmRGroup.First; i < ArmRGroup.Last; ++i)
            {
                job.Weights[i] = data.ArmR;
            }

            for (var i = ArmRGroup.First; i < ArmRGroup.Last; ++i)
            {
                job.Overrides[i] = data.ArmROverride;
            }

            for (var i = HandLGroup.First; i < HandLGroup.Last; ++i)
            {
                job.Weights[i] = data.HandL;
            }

            for (var i = HandRGroup.First; i < HandRGroup.Last; ++i)
            {
                job.Weights[i] = data.HandR;
            }
        }
    }
}
