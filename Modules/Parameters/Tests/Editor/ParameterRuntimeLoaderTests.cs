using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Parameters.Tests
{
    public sealed class ParameterRuntimeLoaderTests
    {
        [Test]
        public void Load_FlattensTablesIntoOneStoreAndKeepsDefinitions()
        {
            TextAsset first = new TextAsset(@"{
  ""SchemaVersion"": 1,
  ""TableGuid"": ""11111111-1111-1111-1111-111111111111"",
  ""DisplayName"": ""First"",
  ""Parameters"": [
    { ""Key"": ""Day"", ""DisplayName"": ""Day"", ""Type"": ""Int"", ""InitialValue"": ""1"", ""MinValue"": ""0"", ""MaxValue"": ""99"" }
  ]
}");
            TextAsset second = new TextAsset(@"{
  ""SchemaVersion"": 1,
  ""TableGuid"": ""22222222-2222-2222-2222-222222222222"",
  ""DisplayName"": ""Second"",
  ""Parameters"": [
    { ""Key"": ""Open"", ""DisplayName"": ""Open"", ""Type"": ""Bool"", ""InitialValue"": ""true"" }
  ]
}");

            try
            {
                ParameterStore store = ParameterRuntimeLoader.Load(
                    new[] { first, second });

                Assert.That(store.GetInt("Day"), Is.EqualTo(1));
                Assert.That(store.GetBool("Open"), Is.True);
                Assert.That(store.Definitions, Has.Count.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }
    }
}
