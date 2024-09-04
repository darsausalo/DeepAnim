using System;

namespace AurigaGames.Deep.Core.StateSystem
{
    public class SubStateMachine<TOuter, TOwner, TSelf> : StateMachine<TOuter, TSelf>, IState<TOuter, TOwner>
        where TOuter : class
        where TSelf : StateMachine<TOuter, TSelf>
        where TOwner : StateMachine<TOuter, TOwner>
    {
        [NonSerialized] protected TOwner Owner;

        public void SetOwner(TOwner owner)
        {
            Owner = owner;
        }

        public override void OnExit()
        {
            CurrentState?.OnExit();
        }
    }
}