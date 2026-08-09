using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KahaGameCore.Parameters
{
    public sealed class ParameterTable
    {
        private readonly ReadOnlyCollection<ParameterDefinition> definitions;

        public ParameterTable(
            string tableGuid,
            string displayName,
            IEnumerable<ParameterDefinition> definitions)
        {
            if (!Guid.TryParse(tableGuid, out _))
            {
                throw new InvalidParameterTableException(tableGuid, "TableGuid must be a valid GUID.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new InvalidParameterTableException(tableGuid, "DisplayName is required.");
            }

            if (definitions == null)
            {
                throw new InvalidParameterTableException(tableGuid, "Parameters are required.");
            }

            List<ParameterDefinition> copiedDefinitions = new List<ParameterDefinition>();
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (ParameterDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new InvalidParameterTableException(tableGuid, "Parameter cannot be null.");
                }

                if (!keys.Add(definition.Key))
                {
                    throw new InvalidParameterTableException(
                        tableGuid,
                        $"Parameter key '{definition.Key}' is duplicated.");
                }

                copiedDefinitions.Add(definition);
            }

            TableGuid = tableGuid;
            DisplayName = displayName;
            this.definitions = copiedDefinitions.AsReadOnly();
        }

        public string TableGuid { get; }
        public string DisplayName { get; }
        public IReadOnlyList<ParameterDefinition> Definitions => definitions;
    }
}
