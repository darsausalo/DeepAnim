using AurigaGames.Deep.Core.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Characters.Animations.Layers
{
    [CreateAssetMenu(menuName = "Deep/Animation/Aim Layer", order = 2)]
    public class AimAnimLayer : AnimationLayer
    {
        public override IAnimationLayerInstance Create(Animator animator, PlayableGraph graph)
        {
            return new Instance(animator, graph);
        }
        
        private class Instance : IAnimationLayerInstance, IAnimationLayerLogic
        {
            private readonly AnimationScriptPlayable _playable;
            private readonly AimJob _job;
            private readonly CharacterBody _body;
            
            public Instance(Animator animator, PlayableGraph graph)
            {
                _body = animator.GetComponentInParent<CharacterBody>();
                
                _job = AimJobBinder.Create(animator);
                _playable = AnimationScriptPlayable.Create(graph, _job, 1);
            }

            public void SetPlayableInput(Playable playable, int port)
            {
                _playable.ConnectInput(0, playable, port, 1f);
            }

            public void GetPlayableOutput(out Playable playable, out int port)
            {
                playable = _playable;
                port = 0;
            }

            public void Update()
            {
                _job.Update(_body.YawOffset);
            }

            public void Destroy()
            {
                _job.Destroy();
            }
        }
    }
}