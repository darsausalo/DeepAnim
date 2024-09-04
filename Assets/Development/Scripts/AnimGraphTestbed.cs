using AurigaGames.Deep.Core;
using AurigaGames.Deep.Core.Animations;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Development
{
    public class AnimGraphTestbed : MonoBehaviour
    {
        private Animator _animator;

        private PlayableGraph _graph;
        private LayeringJob _layeringJob;
        private AnimationScriptPlayable _layeringPlayable;
        private AnimationClipPlayable _baseClipPlayable;

        public AnimationClip basePoseClip;
        public AnimationClip BaseClip;
        public AnimationClip OverlayClip;
        [InlineButton("RestartBaseClip", "R")] public bool PlayBaseClipOnce;

        [LabelText("Layering")] public OverlayWeights overlayWeights = new()
        {
            Hips = 1f,
            Legs = 1f,
            Spine = 0.5f,
            Head = 0.5f,
            ArmL = 0.5f,
            ArmLOverride = 0f,
            ArmR = 1f,
            ArmROverride = 1f,
        };

        public AnimationClip BreathingClip;

        private AimJob _aimJob;
        private AnimationScriptPlayable _aimPlayable;
        [Range(-90f, 90f)] public float AimYaw;
        [Range(-90f, 90f)] public float AimPitch;

        private AnimationClipPlayable _aimSweepPlayable;

        [Required] public AnimationClip AimSweepClip;
        [Required] public AvatarMask AimSweepMask;

        private void OnEnable()
        {
            _animator = GetComponentInChildren<Animator>();

            _graph = PlayableGraph.Create();
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var basePlayable = AnimationClipPlayable.Create(_graph, BaseClip);
            if (PlayBaseClipOnce)
            {
                basePlayable.SetDuration(BaseClip.length);
            }
            _baseClipPlayable = basePlayable;
            var breathingPlayable = AnimationClipPlayable.Create(_graph, BreathingClip);
            breathingPlayable.SetApplyFootIK(false);
            var breatheMixer = AnimationLayerMixerPlayable.Create(_graph);
            breatheMixer.AddInput(basePlayable, 0, 1f);
            breatheMixer.AddInput(breathingPlayable, 0, 1f);
            breatheMixer.SetLayerAdditive(1, true);


            _layeringJob = LayeringJobBinder.Create(_animator);
            _layeringPlayable = AnimationScriptPlayable.Create(_graph, _layeringJob);
            _layeringPlayable.SetProcessInputs(false);
            var basePosePlayable = AnimationClipPlayable.Create(_graph, basePoseClip);
            basePosePlayable.SetApplyFootIK(false);
            _layeringPlayable.AddInput(basePosePlayable, 0, 1f);
            _layeringPlayable.AddInput(breatheMixer, 0, 1f);
            var overlayAnim = AnimationClipPlayable.Create(_graph, OverlayClip);
            overlayAnim.SetApplyFootIK(false);
            _layeringPlayable.AddInput(overlayAnim, 0, 1f);

            _aimSweepPlayable = AnimationClipPlayable.Create(_graph, AimSweepClip);
            _aimSweepPlayable.SetApplyFootIK(false);
            _aimSweepPlayable.Pause();

            var sweepMixer = AnimationLayerMixerPlayable.Create(_graph);
            sweepMixer.AddInput(_layeringPlayable, 0, 1f);
            sweepMixer.AddInput(_aimSweepPlayable, 0, 1f);
            sweepMixer.SetLayerAdditive(1, true);
            sweepMixer.SetLayerMaskFromAvatarMask(1, AimSweepMask);

            _aimJob = AimJobBinder.Create(_animator);
            _aimPlayable = AnimationScriptPlayable.Create(_graph, _aimJob);
            _aimPlayable.AddInput(sweepMixer, 0, 1.0f);

            var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
            output.SetSourcePlayable(_aimPlayable);

            _graph.Play();
        }

        private void Update()
        {
            var sweepTime = MathHelper.GetMappedRangeValueClamped(-90f, 90f, 0f, 1f, AimPitch);
            _aimSweepPlayable.SetTime(sweepTime);

            _layeringJob.Update(overlayWeights);
            _aimJob.Update(AimYaw);
        }

        private void OnDisable()
        {
            _aimJob.Destroy();
            _layeringJob.Destroy();

            _graph.Destroy();
        }

        [UsedImplicitly]
        public void RestartBaseClip()
        {
            _baseClipPlayable.SetTime(0f);
        }
    }
}
