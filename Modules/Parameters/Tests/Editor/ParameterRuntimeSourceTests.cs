using System;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Parameters.Tests
{
    public sealed class ParameterRuntimeSourceTests
    {
        [Test]
        public void Initialize_ExposesLiveParameterStoreValues()
        {
            GameObject host = new GameObject("Parameter Runtime Source");

            try
            {
                TestParameterRuntimeSource source =
                    host.AddComponent<TestParameterRuntimeSource>();
                ParameterStore parameters = new ParameterStore(new[]
                {
                    ParameterDefinition.Int("Supplies", "物資", 10, 0, 100)
                });

                Assert.That(source.IsInitialized, Is.False);
                Assert.That(source.CaptureCurrentValues(), Is.Empty);

                source.Initialize(parameters);
                parameters.Set("Supplies", 25);

                Assert.That(source.IsInitialized, Is.True);
                Assert.That(source.CaptureCurrentValues()[0].Value.AsInt(), Is.EqualTo(25));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Initialize_RejectsNullStore()
        {
            GameObject host = new GameObject("Parameter Runtime Source");

            try
            {
                TestParameterRuntimeSource source =
                    host.AddComponent<TestParameterRuntimeSource>();

                Assert.Throws<ArgumentNullException>(() => source.Initialize(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }

    public sealed class TestParameterRuntimeSource : ParameterRuntimeSource
    {
    }
}
