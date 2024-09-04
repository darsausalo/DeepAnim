using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Core.Animations
{
    public class AnimationStatesPlayable : PlayableBehaviour
    {
        private const int InitialStateCount = 20;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private bool _forceUpdateWeights = true;

        private PlayableState[] _states = new PlayableState[InitialStateCount];
        private int _stateCount;

        public OverlayWeights OverlayWeights; // TODO: add AnimationOverlayStatesPlayable?
        public OverlayWeights OverlayWeightsMask; // TODO: remove OverlayWeightsMask?

        public void Play(Playable playable, float fadeDuration = 0.2f, in OverlayWeights stateOverlayWeights = default)
        {
            var index = -1;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var currentState = ref _states[i];
                if (currentState.Playable.IsValid() && currentState.Playable.Equals(playable))
                {
                    index = i;
                    continue;
                }

                if (!currentState.Enabled)
                {
                    continue;
                }

                SetupLerp(ref currentState, 0.0f, fadeDuration);
            }

            if (index == -1)
            {
                if (_stateCount == _states.Length)
                {
                    Array.Resize(ref _states, _states.Length * 2);
                }

                index = _stateCount++;
            }

            ref var state = ref _states[index];

            if (index == _mixer.GetInputCount())
            {
                _mixer.SetInputCount(index + 1);
            }

            state.Playable = playable;

            SetupLerp(ref state, 1.0f, fadeDuration);

            if (!state.Enabled)
            {
                state.Enabled = true;
                if (_stateCount == 1)
                {
                    state.Weight = 1.0f;
                    state.FadeSpeed = 0.0f;
                    state.Fading = false;
                }

                _graph.Connect(state.Playable, 0, _mixer, index);

                _forceUpdateWeights = true;
            }

            state.OverlayWeights = stateOverlayWeights;
        }

        public bool UpdateOverlayWeights(Playable playable, in OverlayWeights overlayWeights)
        {
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                if (!state.Playable.Equals(playable))
                    continue;
                state.OverlayWeights = overlayWeights;
                return true;
            }

            return false;
        }

        public override void OnPlayableCreate(Playable playable)
        {
            _graph = playable.GetGraph();

            _mixer = AnimationMixerPlayable.Create(_graph, 1);
            playable.AddInput(_mixer, 0, 1.0f);
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            var totalWeight = 0.0f;
            var updateWeights = _forceUpdateWeights;

            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                if (!state.Enabled || !state.Fading)
                {
                    continue;
                }

                state.Weight = Mathf.MoveTowards(state.Weight, state.TargetWeight, state.FadeSpeed * info.deltaTime);

                if (Mathf.Approximately(state.Weight, state.TargetWeight))
                {
                    state.Weight = state.TargetWeight;
                    state.Fading = false;
                    if (state.Weight == 0.0f)
                    {
                        state.Enabled = false;
                        _graph.Disconnect(_mixer, i);
                    }
                }

                totalWeight += state.Weight;
                updateWeights = true;
            }

            if (_forceUpdateWeights)
            {
                totalWeight = 0.0f;
                for (var i = 0; i < _stateCount; ++i)
                {
                    ref var state = ref _states[i];
                    if (!state.Enabled)
                    {
                        continue;
                    }

                    totalWeight += state.Weight;
                }
            }

            if (updateWeights)
            {
                var hasAnyWeight = totalWeight > 0.0f;
                for (var i = 0; i < _stateCount; ++i)
                {
                    ref var state = ref _states[i];
                    if (!state.Enabled)
                    {
                        continue;
                    }

                    var weight = hasAnyWeight ? state.Weight / totalWeight : 0.0f;
                    _mixer.SetInputWeight(i, weight);
                }
            }

            UpdateLayering();
        }

        // TODO: extract to LayeringMixer
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateLayering()
        {
            // TODO: remove mask?
            OverlayWeights.Hips = OverlayWeightsMask.Hips;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.Hips += state.OverlayWeights.Hips * state.Weight;
            }

            OverlayWeights.Legs = OverlayWeightsMask.Legs;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.Legs += state.OverlayWeights.Legs * state.Weight;
            }

            OverlayWeights.Spine = OverlayWeightsMask.Spine;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.Spine += state.OverlayWeights.Spine * state.Weight;
            }

            OverlayWeights.Head = OverlayWeightsMask.Head;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.Head += state.OverlayWeights.Head * state.Weight;
            }

            OverlayWeights.ArmL = OverlayWeightsMask.ArmL;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.ArmL += state.OverlayWeights.ArmL * state.Weight;
            }

            OverlayWeights.ArmLOverride = OverlayWeightsMask.ArmLOverride;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.ArmLOverride += state.OverlayWeights.ArmLOverride * state.Weight;
            }

            OverlayWeights.ArmR = OverlayWeightsMask.ArmR;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.ArmR += state.OverlayWeights.ArmR * state.Weight;
            }

            OverlayWeights.ArmROverride = OverlayWeightsMask.ArmROverride;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.ArmROverride += state.OverlayWeights.ArmROverride * state.Weight;
            }

            OverlayWeights.HandL = OverlayWeightsMask.HandL;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.HandL += state.OverlayWeights.HandL * state.Weight;
            }

            OverlayWeights.HandR = OverlayWeightsMask.HandR;
            for (var i = 0; i < _stateCount; ++i)
            {
                ref var state = ref _states[i];
                OverlayWeights.HandR += state.OverlayWeights.HandR * state.Weight;
            }
        }

        private static void SetupLerp(ref PlayableState state, float targetWeight, float duration)
        {
            Debug.Assert(duration != 0.0f);
            var travel = Mathf.Abs(state.Weight - targetWeight);
            var fadeSpeed = travel / duration;

            if (Mathf.Approximately(state.TargetWeight, targetWeight) && fadeSpeed < state.FadeSpeed)
            {
                return;
            }

            state.TargetWeight = targetWeight;
            state.FadeSpeed = fadeSpeed;
            state.Fading = Mathf.Abs(fadeSpeed) > 0.0f;
        }

        private struct PlayableState
        {
            public Playable Playable;
            public bool Enabled;
            public float Weight;
            public float TargetWeight;
            public float FadeSpeed;
            public bool Fading;
            public OverlayWeights OverlayWeights;
        }
    }
}