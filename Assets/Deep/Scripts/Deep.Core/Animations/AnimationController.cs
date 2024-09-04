using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Core.Animations
{
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class AnimationController : MonoBehaviour
    {
        private readonly List<AnimLayerEntry> _layers = new();
        private PlayableGraph _graph;
        
        [Required]
        public AnimationGraph AnimationGraph;

        private void Awake()
        {
            var animator = GetComponent<Animator>();
            
            _graph = PlayableGraph.Create(name);

            foreach (var layer in AnimationGraph.Layers)
            {
                var instance = layer.Create(animator, _graph);
                _layers.Add(new AnimLayerEntry
                {
                    Instance = instance,
                    Logic = instance as IAnimationLayerLogic,
                    LateLogic = instance as IAnimationLayerLateLogic
                });
            }

            var outputPlayable = Playable.Null;
            var outputPort = 0;
            foreach (var layerEntry in _layers)
            {
                layerEntry.Instance.SetPlayableInput(outputPlayable, outputPort);
                layerEntry.Instance.GetPlayableOutput(out outputPlayable, out outputPort);
            }
            
            var animationOutput = AnimationPlayableOutput.Create(_graph, "Locomotion", animator);
            animationOutput.SetSourcePlayable(outputPlayable, outputPort);
        }

        private void OnEnable()
        {
            _graph.Play();
        }

        private void OnDisable()
        {
            _graph.Stop();
        }

        private void OnDestroy()
        {
            foreach (var layerEntry in _layers)
            {
                layerEntry.Instance.Destroy();
            }

            _graph.Destroy();
        }

        private void Update()
        {
            foreach (var layerEntry in _layers)
            {
                layerEntry.Logic?.Update();
            }
        }

        private void LateUpdate()
        {
            foreach (var layerEntry in _layers)
            {
                layerEntry.LateLogic?.LateUpdate();
            }
        }

        private struct AnimLayerEntry
        {
            public IAnimationLayerInstance Instance;
            public IAnimationLayerLogic Logic;
            public IAnimationLayerLateLogic LateLogic;
        }
    }
}