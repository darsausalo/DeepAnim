using Sirenix.OdinInspector;
using UnityEngine;

namespace AurigaGames.Deep.Core.Animations
{
    [CreateAssetMenu(menuName = "Deep/Animation/Graph", order = 0)]
    public class AnimationGraph : ScriptableObject
    {
        [Required]
        public AnimationLayer[] Layers;
    }
}