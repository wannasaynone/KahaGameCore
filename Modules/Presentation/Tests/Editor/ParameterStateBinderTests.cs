using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Presentation.Tests
{
    public sealed class ParameterStateBinderTests
    {
        [Test]
        public void InitializeAndChanges_ApplyZeroToAOneToBTwoToA()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 2)
            });
            GameObject host = new GameObject("Binder Host");
            GameObject stateRoot = new GameObject("State Root");
            GameObject stateA = new GameObject("A");
            GameObject stateB = new GameObject("B");
            stateRoot.transform.SetParent(host.transform);
            stateA.transform.SetParent(stateRoot.transform);
            stateB.transform.SetParent(stateRoot.transform);

            try
            {
                ParameterStateBinder binder = host.AddComponent<ParameterStateBinder>();
                binder.Configure(
                    "Stage",
                    stateRoot.transform,
                    new[]
                    {
                        new ParameterChildStateMapping(0, 0),
                        new ParameterChildStateMapping(1, 1),
                        new ParameterChildStateMapping(2, 0)
                    });
                binder.Initialize(parameters);

                AssertState(stateA, stateB, aIsActive: true);

                parameters.Set("Stage", 1);
                AssertState(stateA, stateB, aIsActive: false);

                parameters.Set("Stage", 2);
                AssertState(stateA, stateB, aIsActive: true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static void AssertState(
            GameObject stateA,
            GameObject stateB,
            bool aIsActive)
        {
            Assert.That(stateA.activeSelf, Is.EqualTo(aIsActive));
            Assert.That(stateB.activeSelf, Is.EqualTo(!aIsActive));
        }
    }
}
