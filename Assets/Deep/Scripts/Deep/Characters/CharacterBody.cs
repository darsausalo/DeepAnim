using System;
using AurigaGames.Deep.Characters.Animations;
using UnityEngine;

namespace AurigaGames.Deep.Characters
{
    public class CharacterBody : MonoBehaviour // TODO: NetworkBehaviour
    {
        // TODO: too many?
        // TODO: serialize?
        [NonSerialized] public float AimPitch;
        [NonSerialized] public float YawOffset;
        
        public OverlayKind Overlay; // TODO: NetworkVariable

        [NonSerialized] public bool Aim; // TODO: what about aim in CharacterBody?

        [NonSerialized] public bool IsSprinting;
    }
}
