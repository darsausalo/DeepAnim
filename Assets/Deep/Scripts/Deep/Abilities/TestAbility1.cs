using AurigaGames.Deep.Characters;
using AurigaGames.Deep.Characters.Animations;
using AurigaGames.Deep.Core.AbilitySystem;
using UnityEngine;

namespace AurigaGames.Deep.Abilities
{
    [CreateAssetMenu]
    public class TestAbility1 : Ability
    {
        public OverlayKind Overlay = OverlayKind.Pistol;
        
        public override IAbilityInstance Create(GameObject owner)
        {
            return new Instance(owner, this);
        }

        private class Instance : IAbilityInstance
        {
            private readonly CharacterBody _body;
            private readonly TestAbility1 _settings;

            public Instance(GameObject owner, TestAbility1 settings)
            {
                _body = owner.GetComponent<CharacterBody>();
                _settings = settings;
            }

            public bool Execute()
            {
                _body.Overlay = _body.Overlay == _settings.Overlay ? OverlayKind.Default : _settings.Overlay;
                return true;
            }
        }
    }
}
