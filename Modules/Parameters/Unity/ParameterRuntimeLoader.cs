using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KahaGameCore.Parameters
{
    public static class ParameterRuntimeLoader
    {
        public static ParameterStore Load(IReadOnlyList<TextAsset> tableAssets)
        {
            if (tableAssets == null) throw new ArgumentNullException(nameof(tableAssets));
            if (tableAssets.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one Parameter Table is required.");
            }

            ParameterTableJsonCodec codec = new ParameterTableJsonCodec();
            return new ParameterStore(tableAssets
                .Select((asset, index) => asset != null
                    ? codec.Read(asset.text)
                    : throw new InvalidOperationException(
                        $"Parameter Table at index {index} is missing."))
                .SelectMany(table => table.Definitions));
        }
    }
}
