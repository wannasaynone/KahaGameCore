namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Owns one authoritative piece of save state. Implementations must not use
    /// Restore to replay historical gameplay actions.
    /// </summary>
    public interface ISaveParticipant<TSnapshot>
    {
        /// <summary>A stable identity that must not change after registration.</summary>
        string SaveKey { get; }

        /// <summary>
        /// Returns a snapshot detached from mutable runtime state.
        /// </summary>
        TSnapshot Capture();

        /// <summary>Applies the authoritative state represented by the snapshot.</summary>
        void Restore(TSnapshot snapshot);
    }
}
