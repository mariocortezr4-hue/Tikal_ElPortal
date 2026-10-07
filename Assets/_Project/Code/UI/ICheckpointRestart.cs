namespace Tikal.UI
{
    // Adapter for an existing checkpoint authority; Pause never stores a second checkpoint.
    public interface ICheckpointRestart
    {
        bool CanRestartFromCheckpoint { get; }
        void RestartFromCheckpoint();
    }
}
