using AurigaGames.Deep.Characters;
using AurigaGames.Deep.Core.AbilitySystem;
using UnityEngine;

namespace AurigaGames.Deep.Abilities
{
    [CreateAssetMenu(menuName = "Deep/Abilities/Base/Jump")]
    public class JumpAbility : Ability
    {
        public override IAbilityInstance Create(GameObject owner)
        {
            return new Instance(owner);
        }

        // TODO: respect to MovementMode + end/cancel?
        private class Instance : IAbilityInstance
        {
            private readonly CharacterMovement _movement;

            public Instance(GameObject owner)
            {
                _movement = owner.GetComponent<CharacterMovement>();
            }

            public bool Execute()
            {
                _movement.RequestJump();
                return true; // TODO: check can jump!
            }
        }
    }
}
