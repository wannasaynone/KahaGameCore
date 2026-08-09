using System;
using System.Globalization;
using System.Linq;
using JsonFx.Json;

namespace KahaGameCore.Parameters
{
    public sealed class ParameterTableJsonCodec
    {
        public const int CurrentSchemaVersion = 1;

        private sealed class TableDocument
        {
            public int SchemaVersion;
            public string TableGuid;
            public string DisplayName;
            public ParameterDocument[] Parameters;
        }

        private sealed class ParameterDocument
        {
            public string Key;
            public string DisplayName;
            public string Type;
            public string InitialValue;
            public string MinValue;
            public string MaxValue;
        }

        public ParameterTable Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidParameterTableException(string.Empty, "JSON document is required.");
            }

            TableDocument document = JsonReader.Deserialize<TableDocument>(json);
            ValidateDocument(document);
            ParameterDefinition[] definitions = document.Parameters
                .Select(ReadDefinition)
                .ToArray();
            return new ParameterTable(document.TableGuid, document.DisplayName, definitions);
        }

        public string Write(ParameterTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));

            TableDocument document = new TableDocument
            {
                SchemaVersion = CurrentSchemaVersion,
                TableGuid = table.TableGuid,
                DisplayName = table.DisplayName,
                Parameters = table.Definitions.Select(WriteDefinition).ToArray()
            };
            return JsonWriter.Serialize(document);
        }

        private static void ValidateDocument(TableDocument document)
        {
            if (document == null)
            {
                throw new InvalidParameterTableException(string.Empty, "JSON document is invalid.");
            }

            if (document.SchemaVersion != CurrentSchemaVersion)
            {
                throw new InvalidParameterTableException(
                    document.TableGuid,
                    $"Unsupported SchemaVersion {document.SchemaVersion}.");
            }

            if (!Guid.TryParse(document.TableGuid, out _))
            {
                throw new InvalidParameterTableException(
                    document.TableGuid,
                    "TableGuid must be a valid GUID.");
            }

            if (string.IsNullOrWhiteSpace(document.DisplayName))
            {
                throw new InvalidParameterTableException(document.TableGuid, "DisplayName is required.");
            }

            if (document.Parameters == null)
            {
                throw new InvalidParameterTableException(document.TableGuid, "Parameters are required.");
            }
        }

        private static ParameterDefinition ReadDefinition(ParameterDocument document)
        {
            if (document == null)
            {
                throw new InvalidParameterDefinitionException(string.Empty, "Parameter document is required.");
            }

            if ((document.Type == "Bool" || document.Type == "String") &&
                (!string.IsNullOrEmpty(document.MinValue) || !string.IsNullOrEmpty(document.MaxValue)))
            {
                throw new InvalidParameterDefinitionException(
                    document.Key,
                    "Bool and String parameters cannot declare MinValue or MaxValue.");
            }

            switch (document.Type)
            {
                case "Int":
                    return ParameterDefinition.Int(
                        document.Key,
                        document.DisplayName,
                        ParseInt(document.Key, "InitialValue", document.InitialValue),
                        ParseInt(document.Key, "MinValue", document.MinValue),
                        ParseInt(document.Key, "MaxValue", document.MaxValue));
                case "Float":
                    return ParameterDefinition.Float(
                        document.Key,
                        document.DisplayName,
                        ParseFloat(document.Key, "InitialValue", document.InitialValue),
                        ParseFloat(document.Key, "MinValue", document.MinValue),
                        ParseFloat(document.Key, "MaxValue", document.MaxValue));
                case "Bool":
                    return ParameterDefinition.Bool(
                        document.Key,
                        document.DisplayName,
                        ParseBool(document.Key, document.InitialValue));
                case "String":
                    return ParameterDefinition.String(
                        document.Key,
                        document.DisplayName,
                        document.InitialValue ?? string.Empty);
                default:
                    throw new InvalidParameterDefinitionException(
                        document.Key,
                        $"Unsupported Type '{document.Type}'.");
            }
        }

        private static ParameterDocument WriteDefinition(ParameterDefinition definition)
        {
            return new ParameterDocument
            {
                Key = definition.Key,
                DisplayName = definition.DisplayName,
                Type = definition.Type.ToString(),
                InitialValue = Format(definition.InitialValue),
                MinValue = definition.MinValue.HasValue ? Format(definition.MinValue.Value) : null,
                MaxValue = definition.MaxValue.HasValue ? Format(definition.MaxValue.Value) : null
            };
        }

        private static int ParseInt(string key, string field, string text)
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                throw new InvalidParameterDefinitionException(key, $"{field} must be an Int.");
            }

            return value;
        }

        private static float ParseFloat(string key, string field, string text)
        {
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                throw new InvalidParameterDefinitionException(key, $"{field} must be a Float.");
            }

            return value;
        }

        private static bool ParseBool(string key, string text)
        {
            if (!bool.TryParse(text, out bool value))
            {
                throw new InvalidParameterDefinitionException(key, "InitialValue must be a Bool.");
            }

            return value;
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
    }
}
