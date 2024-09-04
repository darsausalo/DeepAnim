using AurigaGames.Deep.Characters.Animations.Layers;
using AurigaGames.Deep.Core.StateSystem;

namespace AurigaGames.Deep.Characters.Animations.States
{
    public class BaseStateMachine : StateMachine<BaseAnimLayer.Instance, BaseStateMachine>
    {
        public readonly GroundedSubMachine Grounded = new ();
        public readonly InAirSubMachine InAir = new();

        public override void Initialize(BaseAnimLayer.Instance outer)
        {
            base.Initialize(outer);

            Grounded.Initialize(outer);
            InAir.Initialize(outer);
            
            OnEnter();
        }

        public override void OnEnter()
        {
            // TODO: Choose between Grounded/InAir/Swimming/Climbing
            SetState(Grounded);
        }
    }
}