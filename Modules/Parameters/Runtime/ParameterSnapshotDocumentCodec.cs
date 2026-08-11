using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KahaGameCore.Parameters
{
    public sealed class ParameterSnapshotDocumentCodec
    {
        public ParameterSnapshotDocument Encode(ParameterSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            EnsureSupportedSchema(snapshot.SchemaVersion);

            return new ParameterSnapshotDocument
            {
                SchemaVersion = snapshot.SchemaVersion,
                Values = snapshot.Values
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(EncodeValue)
                    .ToArray()
            };
        }

        private static ParameterSnapshotValueDocument EncodeValue(
            KeyValuePair<string, ParameterValue> pair)
        {
            ParameterSnapshotValueDocument value =
                new ParameterSnapshotValueDocument
                {
                    Key = pair.Key,
                    Type = pair.Value.Type.ToString(),
                    Value = Format(pair.Value)
                };
            Validate(value);
            return value;
        }

        public ParameterSnapshot Decode(ParameterSnapshotDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            EnsureSupportedSchema(document.SchemaVersion);
            if (document.Values == null)
            {
                throw new ParameterSnapshotException(
                    "Parameter snapshot document is missing Values.");
            }

            Dictionary<string, ParameterValue> values =
                new Dictionary<string, ParameterValue>(StringComparer.Ordinal);
            foreach (ParameterSnapshotValueDocument value in document.Values)
            {
                Validate(value);
                if (values.ContainsKey(value.Key))
                {
                    throw new ParameterSnapshotException(
                        $"Duplicate parameter snapshot key '{value.Key}'.");
                }

                values.Add(value.Key, Parse(value));
            }

            return new ParameterSnapshot(document.SchemaVersion, values);
        }

        private static void Validate(ParameterSnapshotValueDocument value)
        {
            if (value == null)
            {
                throw new ParameterSnapshotException(
                    "Parameter snapshot document contains a null value entry.");
            }

            if (string.IsNullOrWhiteSpace(value.Key))
            {
                throw new ParameterSnapshotException(
                    "Parameter snapshot value is missing Key.");
            }

            if (string.IsNullOrEmpty(value.Type))
            {
                throw new ParameterSnapshotException(
                    $"Parameter snapshot value '{value.Key}' is missing Type.");
            }

            if (value.Value == null)
            {
                throw new ParameterSnapshotException(
                    $"Parameter snapshot value '{value.Key}' is missing Value.");
            }
        }

        private static void EnsureSupportedSchema(int schemaVersion)
        {
            if (schemaVersion != ParameterSnapshot.CurrentSchemaVersion)
            {
                throw new ParameterSnapshotException(
                    $"Unsupported parameter snapshot schema version '{schemaVersion}'.");
            }
        }

        private static string Format(ParameterValue value)
        {
            switch (value.Type)
            {
                case ParameterType.Int:
                    return value.AsInt().ToString(CultureInfo.InvariantCulture);
                case ParameterType.Float:
                    return value.AsFloat().ToString("R", CultureInfo.InvariantCulture);
                case ParameterType.Bool:
                    return value.AsBool() ? "true" : "false";
                case ParameterType.String:
                    return value.AsString();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static ParameterValue Parse(ParameterSnapshotValueDocument value)
        {
            switch (value.Type)
            {
                case "Int":
                    return ParseInt(value);
                case "Float":
                    return ParseFloat(value);
                case "Bool":
                    return ParseBool(value);
                case "String":
                    return ParameterValue.FromString(value.Value);
                default:
                    throw new ParameterSnapshotException(
                        $"Unsupported parameter snapshot type '{value.Type}'.");
            }
        }

        private static ParameterValue ParseInt(
            ParameterSnapshotValueDocument value)
        {
            if (!int.TryParse(
                    value.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsed))
            {
                throw InvalidValue(value);
            }

            return ParameterValue.FromInt(parsed);
        }

        private static ParameterValue ParseFloat(
            ParameterSnapshotValueDocument value)
        {
            if (!float.TryParse(
                    value.Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float parsed))
            {
                throw InvalidValue(value);
            }

            return ParameterValue.FromFloat(parsed);
        }

        private static ParameterValue ParseBool(
            ParameterSnapshotValueDocument value)
        {
            if (!bool.TryParse(value.Value, out bool parsed))
            {
                throw InvalidValue(value);
            }

            return ParameterValue.FromBool(parsed);
        }

        private static ParameterSnapshotException InvalidValue(
            ParameterSnapshotValueDocument value)
        {
            return new ParameterSnapshotException(
                $"Parameter snapshot value '{value.Key}' is not a valid {value.Type}.");
        }
    }
}
