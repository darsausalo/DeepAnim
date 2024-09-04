using UnityEngine;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Core.Animations
{
    public abstract class AnimationLayer : ScriptableObject
    {
        public abstract IAnimationLayerInstance Create(Animator animator, PlayableGraph graph);
    }
}