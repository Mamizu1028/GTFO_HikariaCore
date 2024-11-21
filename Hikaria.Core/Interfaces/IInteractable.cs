using Player;
using UnityEngine;

namespace Hikaria.Core.Interfaces
{
    public interface IInteractable
    {
        bool PlayerCanInteract(PlayerAgent source);

        bool PlayerDoInteract(PlayerAgent source);

        void PlayerSetSelected(bool sel, PlayerAgent agent);

        bool PlayerCheckInput(PlayerAgent agent);

        Transform Root { get; }
    }
}
