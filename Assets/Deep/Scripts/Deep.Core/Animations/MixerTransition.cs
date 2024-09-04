using System;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Playables;

namespace AurigaGames.Deep.Core.Animations
{
    public class MixerTransition<T, TPort> where T : struct, IPlayable where TPort : struct, Enum
    {
        private readonly TPort[] _ports;
        private readonly T _target;

        public MixerTransition(T target)
        {
            _target = target;
            _ports = Enum.GetValues(typeof(TPort)).Cast<TPort>().ToArray();
        }

        public void Update(TPort activePortName, float duration, float deltaTime)
        {
            var blendVelocity = duration > 0 ? 1f / duration : 1f / deltaTime;
            var activePort = UnsafeUtility.EnumToInt(activePortName);
            var weight = _target.GetInputWeight(activePort);
            if (!Mathf.Approximately(weight, 1.0f))
            {
                weight = Mathf.Clamp01(weight + blendVelocity * deltaTime);
                _target.SetInputWeight(activePort, weight);
            }

            var weightLeft = 1.0f - weight;
            var totalWeight = 0f;
            for (var i = 0; i < _ports.Length; ++i)
            {
                var port = UnsafeUtility.EnumToInt(_ports[i]);
                if (port == activePort)
                {
                    continue;
                }

                totalWeight += _target.GetInputWeight(port);
            }

            if (totalWeight == 0)
            {
                return;
            }

            var fraction = weightLeft / totalWeight;
            for (var i = 0; i < _ports.Length; ++i)
            {
                var port = UnsafeUtility.EnumToInt(_ports[i]);
                if (port == activePort)
                {
                    continue;
                }

                _target.SetInputWeight(port, _target.GetInputWeight(port) * fraction);
            }
        }
    }
}