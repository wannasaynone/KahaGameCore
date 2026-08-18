using System;
using System.Collections.Generic;
using KahaGameCore.Effects;

namespace KahaGameCore.GameEvents.Editor
{
    public static class GameEventEditorCommandCatalog
    {
        private static Func<IReadOnlyList<EffectCommandDescriptor>> provider;
        private static GameEventEditorDataSource dataSource;

        public static void Register(
            Func<IReadOnlyList<EffectCommandDescriptor>> descriptorProvider,
            GameEventEditorDataSource editorDataSource)
        {
            provider = descriptorProvider ??
                throw new ArgumentNullException(nameof(descriptorProvider));
            dataSource = editorDataSource ??
                throw new ArgumentNullException(nameof(editorDataSource));
        }

        internal static IReadOnlyList<EffectCommandDescriptor> GetDescriptors()
        {
            return provider?.Invoke() ?? Array.Empty<EffectCommandDescriptor>();
        }

        internal static GameEventEditorDataSource GetDataSource()
        {
            return dataSource;
        }
    }
}
