using System;
using UnityEngine;

namespace AurigaGames.Deep.Core.Animations
{
    [Serializable]
    public struct OverlayWeights
    {
        [Range(0f, 1f)] public float Hips;
        [Range(0f, 1f)] public float Legs;
        [Range(0f, 1f)] public float Spine;
        [Range(0f, 1f)] public float Head;
        [Range(0f, 1f)] public float ArmL;
        [Range(0f, 1f)] public float ArmLOverride;
        [Range(0f, 1f)] public float ArmR;
        [Range(0f, 1f)] public float ArmROverride;
        [Range(0f, 1f)] public float HandL;
        [Range(0f, 1f)] public float HandR;
    }
}