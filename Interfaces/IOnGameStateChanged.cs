namespace Hikaria.Core.Interfaces
{
    public interface IOnGameStateChanged
    {
        void OnGameStateChanged(eGameStateName preState, eGameStateName nextState);
    }
}
