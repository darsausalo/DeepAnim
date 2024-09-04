using AurigaGames.Deep.Characters;
using AurigaGames.Deep.Core.AbilitySystem;
using UnityEngine;

namespace AurigaGames.Deep.Abilities
{
    [CreateAssetMenu]
    public class TestAbility2 : Ability
    {
        public override IAbilityInstance Create(GameObject owner)
        {
            return new Instance(owner);
        }
        
        private class Instance : IAbilityCancelableInstance
        {
            private readonly CharacterBody _body;
            
            public Instance(GameObject owner)
            {
                _body = owner.GetComponent<CharacterBody>();
            }

            public bool Execute()
            {
                _body.Aim = true;
                return true;
            }

            public bool End()
            {
                _body.Aim = false;
                return true;
            }

            public bool CanCancel() => true;
        }
    }
}
