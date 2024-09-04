using UnityEngine;

namespace AurigaGames.Deep.Core.Animations
{
    public static class AnimationClipExtensions
    {
        public static float GetTimeAtFrame(this AnimationClip clip, int numFrames, int frame)
        {
            var frameTime = numFrames > 1 ? clip.length / (numFrames - 1) : 0.0f;
            return Mathf.Clamp(frameTime * frame, 0.0f, clip.length);
        }
    }
}