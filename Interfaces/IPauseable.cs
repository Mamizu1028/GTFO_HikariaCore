namespace Hikaria.Core.Interfaces;

public interface IPauseable
{
    void PausedUpdate();

    void OnPaused();

    void OnUnpaused();
}
