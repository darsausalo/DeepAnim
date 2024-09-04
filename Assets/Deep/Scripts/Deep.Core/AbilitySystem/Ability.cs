using UnityEngine;

namespace AurigaGames.Deep.Core.AbilitySystem
{
    public abstract class Ability : ScriptableObject
    {
        public string Action;

        public abstract IAbilityInstance Create(GameObject owner);
    }
}
