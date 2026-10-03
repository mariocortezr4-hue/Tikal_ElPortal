using System;
using UnityEngine;

namespace Tikal.Input
{
    public interface IPlayerInputReader
    {
        bool GameplayEnabled { get; }
        Vector2 Move { get; }
        Vector2 Look { get; }
        LookInputMode LookMode { get; }
        bool SprintHeld { get; }
        bool CrouchHeld { get; }

        event Action JumpPressed;
        event Action InteractPressed;
        event Action SecretCommandPressed;
        event Action PausePressed;
        event Action JournalPressed;

        void EnableGameplay();
        void DisableGameplay();
    }
}
