using System.Collections.Generic;
using UnityEngine;

namespace AurigaGames.Deep.Core.AbilitySystem
{
    public class AbilityController : MonoBehaviour
    {
        private readonly List<IAbilityInstance> _abilities = new();
        private readonly List<IAbilityTask> _abilityTasks = new();
        private readonly List<IAbilityLateTask> _abilityLateTasks = new();

        private readonly List<IAbilityTask> _abilityTasksToAdd = new();
        private readonly List<IAbilityLateTask> _abilityLateTasksToAdd = new();
        private readonly List<IAbilityTask> _abilityTasksToRemove = new();
        private readonly List<IAbilityLateTask> _abilityLateTasksToRemove = new();

        public Ability[] Abilities;

        private void Awake()
        {
            var binder = GetComponent<IAbilityBinder>();
            foreach (var ability in Abilities)
            {
                var instance = ability.Create(gameObject);
                binder?.BindAbility(ability.Action, instance);
                _abilities.Add(instance);
            }
        }

        private void Update()
        {
            foreach (var abilityTask in _abilityTasks)
            {
                abilityTask.Update();
            }

            foreach (var abilityTask in _abilityTasksToAdd)
            {
                _abilityTasks.Add(abilityTask);
            }

            _abilityTasksToAdd.Clear();

            foreach (var abilityTask in _abilityTasksToRemove)
            {
                _abilityTasks.Remove(abilityTask);
            }

            _abilityTasksToRemove.Clear();
        }

        private void LateUpdate()
        {
            foreach (var abilityLateTask in _abilityLateTasks)
            {
                abilityLateTask.LateUpdate();
            }

            foreach (var abilityLateTask in _abilityLateTasksToAdd)
            {
                _abilityLateTasks.Add(abilityLateTask);
            }

            _abilityLateTasksToAdd.Clear();

            foreach (var abilityLateTask in _abilityLateTasksToRemove)
            {
                _abilityLateTasks.Remove(abilityLateTask);
            }

            _abilityLateTasksToRemove.Clear();
        }

        public void ExecuteAbility(IAbilityInstance abilityInstance)
        {
            // TODO: Network - replicate ability execution
            if (abilityInstance.Execute())
            {
                if (abilityInstance is IAbilityTask abilityTask)
                {
                    Debug.Assert(!_abilityTasks.Contains(abilityTask));
                    _abilityTasksToAdd.Add(abilityTask);
                }

                if (abilityInstance is IAbilityLateTask abilityLateTask)
                {
                    Debug.Assert(!_abilityLateTasks.Contains(abilityLateTask));
                    _abilityLateTasksToAdd.Add(abilityLateTask);
                }
            }
        }

        public bool CancelAbility(IAbilityCancelableInstance abilityInstance)
        {
            if (abilityInstance.CanCancel())
            {
                return EndAbility(abilityInstance);
            }

            return false;
        }

        // TODO: protect to call only from ability instance
        public bool EndAbility(IAbilityContinualInstance abilityInstance)
        {
            // TODO: Network - replicate ability ending
            if (abilityInstance.End())
            {
                if (abilityInstance is IAbilityTask abilityTask)
                {
                    Debug.Assert(_abilityTasks.Contains(abilityTask));
                    _abilityTasksToRemove.Add(abilityTask);
                }

                if (abilityInstance is IAbilityLateTask abilityLateTask)
                {
                    Debug.Assert(_abilityLateTasks.Contains(abilityLateTask));
                    _abilityLateTasksToRemove.Add(abilityLateTask);
                }

                return true;
            }

            return false;
        }

        public void AddAbilityTask(IAbilityTask abilityTask)
        {
            Debug.Assert(!_abilityTasks.Contains(abilityTask));
            _abilityTasksToAdd.Add(abilityTask);
        }

        public void RemoveAbilityTask(IAbilityTask abilityTask)
        {
            _abilityTasksToRemove.Add(abilityTask);
        }

        public void AddAbilityTask(IAbilityLateTask abilityLateTask)
        {
            Debug.Assert(!_abilityLateTasks.Contains(abilityLateTask));
            _abilityLateTasksToAdd.Add(abilityLateTask);
        }

        public void RemoveAbilityLateTask(IAbilityLateTask abilityLateTask)
        {
            Debug.Assert(_abilityLateTasks.Contains(abilityLateTask));
            _abilityLateTasksToRemove.Add(abilityLateTask);
        }
    }
}
