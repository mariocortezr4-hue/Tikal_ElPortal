using System;
using UnityEngine;

namespace Tikal.Input
{
    public interface IPlayerInputReader
    {
        Vector2 Move { get; }
        Vector2 Look { get; }
        bool SprintHeld { get; }
        bool CrouchHeld { get; }

        event Action JumpPressed;
        event Action InteractPressed;
        event Action SecretCommandPressed;
        event Action PausePressed;

        void EnableGameplay();
        void DisableGameplay();
    }
}
