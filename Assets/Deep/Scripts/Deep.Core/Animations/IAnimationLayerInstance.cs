using UnityEngine.Playables;

namespace AurigaGames.Deep.Core.Animations
{
    public interface IAnimationLayerInstance
    {
        void SetPlayableInput(Playable playable, int port);

        void GetPlayableOutput(out Playable playable, out int port);
        
        void Destroy();
    }
    
    public interface IAnimationLayerLogic
    {
        void Update();
    }

    public interface IAnimationLayerLateLogic
    {
        void LateUpdate();
    }
}