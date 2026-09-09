using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using UnityEngine.Scripting;

namespace KahaGameCore.Persistence.GameEventsIntegration
{
    internal static class SaveGameEffectCommandManifest
    {
        public static readonly EffectCommandDescriptor SaveGame =
            new EffectCommandDescriptor(
                "SaveGame",
                "Save Game",
                "Persistence",
                new[]
                {
                    new EffectCommandParameterDefinition(
                        "slot",
                        EffectCommandParameterKind.Literal)
                });

        public static readonly IReadOnlyList<EffectCommandDescriptor> Descriptors =
            Array.AsReadOnly(new[] { SaveGame });
    }

    /// <summary>
    /// SaveGame(slot) writes the current session to a save slot. Putting it in
    /// a Game Event turns every trigger the project already has into a save
    /// point: a collider, the end of a conversation, entering a Scene.
    /// </summary>
    public static class SaveGameEffectCommandModule
    {
        public static IReadOnlyList<EffectCommandDefinition> CreateDefinitions()
        {
            return new[]
            {
                new EffectCommandDefinition(
                    SaveGameEffectCommandManifest.SaveGame,
                    new SaveGameCommand())
            };
        }

        private sealed class SaveGameCommand : IEffectCommand
        {
            public UniTask ExecuteAsync(
                EffectExecutionContext context,
                IReadOnlyList<string> arguments,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!int.TryParse(
                        arguments[0],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int slot))
                {
                    throw new FormatException(
                        $"SaveGame slot is invalid: '{arguments[0]}'.");
                }

                // Deliberately not GameSaves.SaveAsync: this command runs inside
                // the Game Event queue that SaveAsync waits to drain.
                GameSaves.Save(slot);
                return UniTask.CompletedTask;
            }
        }
    }

    [Preserve]
    public sealed class SaveGameEffectCommandModuleFactory :
        IEffectCommandModuleFactory
    {
        public IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return SaveGameEffectCommandManifest.Descriptors;
        }

        public IReadOnlyList<EffectCommandDefinition> Create(
            EffectCommandDependencies services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return SaveGameEffectCommandModule.CreateDefinitions();
        }
    }
}
