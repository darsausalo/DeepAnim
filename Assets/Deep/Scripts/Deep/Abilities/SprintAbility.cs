using AurigaGames.Deep.Characters;
using AurigaGames.Deep.Core.AbilitySystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AurigaGames.Deep.Abilities
{
    [CreateAssetMenu(menuName = "Deep/Abilities/Base/Sprint")]
    public class SprintAbility : Ability
    {
        [Range(1f, 50f), SuffixLabel("m/s", true)]
        public float MaxSpeed = 7.5f;

        [Range(0f, 90f), SuffixLabel("\u00B0", true)]
        public float MaxAngle = 25f;

        public bool OnlyGrounded = true; // TODO: respect to other movement modes?

        public override IAbilityInstance Create(GameObject owner)
        {
            return new Instance(owner, this);
        }

        private class Instance : IAbilityContinualInstance, IAbilityTask
        {
            private readonly AbilityController _abilityController;
            private readonly CharacterBody _body;
            private readonly CharacterMovement _movement;
            private readonly Transform _transform;
            private readonly SprintAbility _ability;

            private float _movementStableMoveSpeed;

            public Instance(GameObject owner, SprintAbility ability)
            {
                _abilityController = owner.GetComponent<AbilityController>();
                _body = owner.GetComponent<CharacterBody>();
                _movement = owner.GetComponent<CharacterMovement>();
                _transform = owner.transform;
                _ability = ability;
            }

            public bool Execute()
            {
                if (!_body.IsSprinting && _movement.IsGrounded && _movement.HasMoveInput)
                {
                    _movementStableMoveSpeed = _movement.MaxStableMoveSpeed;
                    _movement.MaxStableMoveSpeed = _ability.MaxSpeed;

                    _body.IsSprinting = true;

                    return true;
                }

                return false;
            }

            public void Update()
            {
                var planarVelocity = Vector3.ProjectOnPlane(_movement.Velocity, _movement.Motor.CharacterUp);
                var planarForward = Vector3.ProjectOnPlane(_transform.forward, _movement.Motor.CharacterUp);
                if (!_movement.HasMoveInput ||
                    _ability.OnlyGrounded && !_movement.IsGrounded || 
                    Vector3.Angle(planarVelocity, planarForward) > _ability.MaxAngle)
                {
                    _abilityController.EndAbility(this);
                }
            }

            public bool End()
            {
                if (_body.IsSprinting)
                {
                    _body.IsSprinting = false;
                    _movement.MaxStableMoveSpeed = _movementStableMoveSpeed;

                    return true;
                }

                return false;
            }
        }
    }
}
