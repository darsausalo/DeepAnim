using System;
using UnityEngine;

namespace AurigaGames.Deep.Core.StateSystem
{
    public abstract class StateMachine<TOuter, TSelf>
        where TOuter : class
        where TSelf : StateMachine<TOuter, TSelf>
    {
        [NonSerialized] protected TOuter Outer;
        [NonSerialized] protected IState<TOuter, TSelf> CurrentState;

        public virtual void Initialize(TOuter outer)
        {
            Outer = outer;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public void SetState(IState<TOuter, TSelf> nextState)
        {
            Debug.Assert(nextState != null);

            nextState.SetOwner((TSelf) this);

            CurrentState?.OnExit();
            CurrentState = nextState;
            CurrentState.OnEnter();
        }

        public virtual void OnUpdate()
        {
            CurrentState.OnUpdate();
        }
    }
}