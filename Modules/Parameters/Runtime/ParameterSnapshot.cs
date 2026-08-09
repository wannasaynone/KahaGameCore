using System;
using System.Collections.Generic;

namespace KahaGameCore.Parameters
{
    public sealed class ParameterSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        private readonly Dictionary<string, ParameterValue> values;

        public ParameterSnapshot(
            int schemaVersion,
            IEnumerable<KeyValuePair<string, ParameterValue>> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            SchemaVersion = schemaVersion;
            this.values = new Dictionary<string, ParameterValue>();
            foreach (KeyValuePair<string, ParameterValue> pair in values)
            {
                this.values.Add(pair.Key, pair.Value);
            }
        }

        public int SchemaVersion { get; }

        public bool TryGetValue(string key, out ParameterValue value)
        {
            return values.TryGetValue(key, out value);
        }

        internal IEnumerable<KeyValuePair<string, ParameterValue>> Values => values;
    }
}
