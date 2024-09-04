using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Core.Animations
{
    [RequireComponent(typeof(Animator))]
    public class SoloAnimation : MonoBehaviour
    {
        private PlayableGraph _graph;
        
        public AnimationClip Clip;

        public bool ApplyFootIK;
        public bool ApplyPlayableFootIK;

        private void Awake()
        {
            var animator = GetComponent<Animator>();
            _graph = PlayableGraph.Create("Solo");

            var anim = AnimationClipPlayable.Create(_graph, Clip);
            anim.SetApplyFootIK(ApplyFootIK);
            anim.SetApplyPlayableIK(ApplyPlayableFootIK);
            
            var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
            output.SetSourcePlayable(anim);
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
            _graph.Destroy();
        }
    }
}