using System;

namespace AurigaGames.Deep.Core.StateSystem
{
    public abstract class State<TOuter, TOwner> : IState<TOuter, TOwner>
        where TOuter : class
        where TOwner : StateMachine<TOuter, TOwner>
    {
        [NonSerialized] protected TOuter Outer;
        [NonSerialized] protected TOwner Owner;

        public virtual void Initialize(TOuter outer)
        {
            Outer = outer;
        }

        public void SetOwner(TOwner owner)
        {
            Owner = owner;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void OnUpdate()
        {
        }
    }
}