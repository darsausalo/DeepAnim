namespace AurigaGames.Deep.Core.StateSystem
{
    public interface IState<in TOuter, in TOwner>
        where TOuter : class
        where TOwner : StateMachine<TOuter, TOwner>
    {
        void Initialize(TOuter outer);
        
        void SetOwner(TOwner owner);

        void OnEnter();

        void OnExit();

        void OnUpdate();
    }
}