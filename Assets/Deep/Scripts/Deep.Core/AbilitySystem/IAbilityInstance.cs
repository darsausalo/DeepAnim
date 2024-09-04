namespace AurigaGames.Deep.Core.AbilitySystem
{
    public interface IAbilityInstance
    {
        bool Execute();
    }

    public interface IAbilityContinualInstance : IAbilityInstance
    {
        bool End();
    }

    public interface IAbilityCancelableInstance : IAbilityContinualInstance
    {
        bool CanCancel();
    }

    public interface IAbilityTask
    {
        void Update();
    }

    public interface IAbilityLateTask
    {
        void LateUpdate();
    }
}
