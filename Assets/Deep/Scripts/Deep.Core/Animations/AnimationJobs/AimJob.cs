using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace AurigaGames.Deep.Core.Animations
{
    public struct AimJob : IAnimationJob
    {
        internal NativeArray<TransformStreamHandle> Bones;
        internal NativeArray<float> Yaws;

        public void ProcessAnimation(AnimationStream stream)
        {
            var numBones = Bones.Length;
            for (var i = 0; i < numBones; ++i)
            {
                var bone = Bones[i];
                var yaw = Yaws[i];

                bone.GetGlobalTR(stream, out var position, out var rotation);
                bone.SetGlobalTR(stream, position, Quaternion.Euler(0f, yaw, 0f) * rotation, false);
            }
        }

        public void ProcessRootMotion(AnimationStream stream) { }
    }

    public static class AimJobBinder
    {
        private const int NumAimBones = 7;

        private const float YawScale = 0.2f;

        public static AimJob Create(Animator animator)
        {
            var job = new AimJob
            {
                Bones = new NativeArray<TransformStreamHandle>(NumAimBones, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory),
                Yaws = new NativeArray<float>(NumAimBones, Allocator.Persistent)
            };

            job.Bones[0] = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.Hips));
            job.Bones[1] = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.Spine));
            job.Bones[2] = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.Chest));
            job.Bones[3] = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.UpperChest));
            job.Bones[4] = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.Neck));

            job.Bones[5] = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.LeftShoulder));
            job.Bones[6] = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.RightShoulder));

            return job;
        }

        public static void Destroy(this AimJob job)
        {
            job.Yaws.Dispose();
            job.Bones.Dispose();
        }

        public static void Update(this AimJob job, float yaw)
        {
            var scaledYaw = yaw * YawScale;

            for (var i = 0; i < job.Yaws.Length; ++i)
            {
                job.Yaws[i] = scaledYaw;
            }
        }
    }
}
