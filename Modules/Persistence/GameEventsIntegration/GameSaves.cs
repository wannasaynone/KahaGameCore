using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameEvents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Save slots for the current play session. Parameters already outlive a
    /// Scene load because GameEventSession owns them, so this is only file IO
    /// plus the Scene the player was standing in.
    /// </summary>
    public static class GameSaves
    {
        private static readonly GameSaveDocumentJsonCodec Codec =
            new GameSaveDocumentJsonCodec();

        private static GameSaveSlotStore slots;

        private static GameSaveSlotStore Slots => slots ??= new GameSaveSlotStore(
            Path.Combine(Application.persistentDataPath, "Saves"));

        public static bool Exists(int slot)
        {
            return Slots.Exists(slot);
        }

        public static bool Delete(int slot)
        {
            return Slots.Delete(slot);
        }

        /// <summary>
        /// Writes the slot immediately. The caller owns the timing, which is
        /// what a SaveGame effect command needs: it already runs at the point
        /// in the Game Event the designer chose, and waiting for the queue it
        /// is itself part of would deadlock.
        /// </summary>
        public static void Save(int slot)
        {
            GameEventRuntime runtime = RequireRuntime();
            Slots.Save(slot, Codec.Write(
                SceneManager.GetActiveScene().path,
                runtime.Parameters.Capture(),
                SaveableObjects.Registry.Capture()));
        }

        /// <summary>
        /// Waits for the Game Event queue to drain, then writes the slot. Use
        /// this from menus and hotkeys, where a save must never capture a
        /// half-applied event. Never call it from inside an effect command.
        /// </summary>
        public static async UniTask SaveAsync(
            int slot,
            CancellationToken cancellationToken = default)
        {
            await RequireRuntime().Events.WaitUntilIdleAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Save(slot);
        }

        /// <summary>
        /// Restores the slot into the live session, then loads its Scene. The
        /// saved objects respawn from SaveableObjects once that Scene is up.
        /// </summary>
        public static void Load(int slot)
        {
            GameSaveSnapshot snapshot = Codec.Read(Slots.Load(slot));
            RequireRuntime().Parameters.Restore(snapshot.Parameters);
            SaveableObjects.Registry.Restore(snapshot.Objects);
            SceneManager.LoadScene(snapshot.SceneKey);
        }

        private static GameEventRuntime RequireRuntime()
        {
            // ponytail: assumes a launcher has already run, which is true for
            // every gameplay Scene. A title screen with no launcher needs the
            // session built first: GameEventSession.GetOrCreate(catalog).
            GameEventRuntime runtime = GameEventSession.Runtime;
            if (runtime == null)
            {
                throw new System.InvalidOperationException(
                    "[GameSaves] No Game Event session yet; a launcher must run first.");
            }

            return runtime;
        }
    }
}
