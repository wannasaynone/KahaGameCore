using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Presentation.Tests
{
    public sealed class ParameterStateBinderTests
    {
        [Test]
        public void InitializeAndChanges_EvaluateIndependentChildConditions()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 2),
                ParameterDefinition.Int("Spirit", "Spirit", 10, 0, 100)
            });
            GameObject host = new GameObject("Binder Host");
            GameObject stateA = CreateChild("A", host);
            GameObject stateB = CreateChild("B", host);
            GameObject warning = CreateChild("Warning", host);

            try
            {
                ParameterStateBinder binder = host.AddComponent<ParameterStateBinder>();
                binder.Configure(new[]
                {
                    new ParameterChildConditionBinding(
                        stateA,
                        "$Stage == 0 || $Stage == 2"),
                    new ParameterChildConditionBinding(stateB, "$Stage == 1"),
                    new ParameterChildConditionBinding(warning, "$Spirit < 20")
                });
                binder.Initialize(parameters);

                AssertState(stateA, stateB, warning, true, false, true);

                parameters.Set("Stage", 1);
                AssertState(stateA, stateB, warning, false, true, true);

                parameters.Set("Spirit", 20);
                AssertState(stateA, stateB, warning, false, true, false);

                parameters.Set("Stage", 2);
                AssertState(stateA, stateB, warning, true, false, false);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void BehaviourBinding_InitializeAndChangesToggleAllTargets()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Bool(
                    "EnemiesEnabled",
                    "Enemies Enabled",
                    false)
            });
            GameObject host = new GameObject("Binder Host");
            GameObject child = CreateChild("Action", host);

            try
            {
                ParameterStateBinderTestBehaviour first =
                    host.AddComponent<ParameterStateBinderTestBehaviour>();
                ParameterStateBinderTestBehaviour second =
                    child.AddComponent<ParameterStateBinderTestBehaviour>();
                ParameterStateBinder binder =
                    host.AddComponent<ParameterStateBinder>();
                binder.ConfigureBehaviourBinding(
                    new Behaviour[] { first, second },
                    "$EnemiesEnabled");

                binder.Initialize(parameters);

                Assert.That(first.enabled, Is.False);
                Assert.That(second.enabled, Is.False);

                parameters.Set("EnemiesEnabled", true);
                Assert.That(first.enabled, Is.True);
                Assert.That(second.enabled, Is.True);

                parameters.Set("EnemiesEnabled", false);
                Assert.That(first.enabled, Is.False);
                Assert.That(second.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ManagedBehaviourBinding_RecordsDerivedAuthoringOwnership()
        {
            GameObject host = new GameObject("Binder Host");

            try
            {
                ParameterStateBinderTestBehaviour target =
                    host.AddComponent<ParameterStateBinderTestBehaviour>();
                ParameterStateBinder binder =
                    host.AddComponent<ParameterStateBinder>();

                binder.ConfigureManagedBehaviourBinding(
                    new Behaviour[] { target },
                    "$Enabled");

                Assert.That(binder.BehaviourTargetsManaged, Is.True);
                Assert.That(binder.BehaviourTargets, Does.Contain(target));

                binder.ConfigureBehaviourBinding(
                    new Behaviour[] { target },
                    "$Enabled");

                Assert.That(binder.BehaviourTargetsManaged, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void BehaviourBinding_TargetOutsideBinderHierarchyFails()
        {
            GameObject host = new GameObject("Binder Host");
            GameObject external = new GameObject("External");

            try
            {
                ParameterStateBinder binder =
                    host.AddComponent<ParameterStateBinder>();
                ParameterStateBinderTestBehaviour target =
                    external.AddComponent<ParameterStateBinderTestBehaviour>();

                Assert.Throws<System.InvalidOperationException>(() =>
                    binder.ConfigureBehaviourBinding(
                        new Behaviour[] { target },
                        "$EnemiesEnabled"));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(external);
            }
        }

        [Test]
        public void InitializeFailure_DoesNotSubscribeOrPartiallyApply()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 2)
            });
            GameObject host = new GameObject("Binder Host");
            GameObject stateA = CreateChild("A", host);
            GameObject stateB = CreateChild("B", host);
            stateA.SetActive(false);
            stateB.SetActive(true);

            try
            {
                ParameterStateBinder binder = host.AddComponent<ParameterStateBinder>();
                binder.Configure(new[]
                {
                    new ParameterChildConditionBinding(stateA, "$Stage == 0"),
                    new ParameterChildConditionBinding(stateB, "$Missing == 1")
                });

                Assert.Throws<System.InvalidOperationException>(
                    () => binder.Initialize(parameters));
                Assert.That(stateA.activeSelf, Is.False);
                Assert.That(stateB.activeSelf, Is.True);
                Assert.DoesNotThrow(() => parameters.Set("Stage", 1));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Configure_DuplicateTargetFails()
        {
            GameObject host = new GameObject("Binder Host");
            GameObject state = CreateChild("State", host);

            try
            {
                ParameterStateBinder binder = host.AddComponent<ParameterStateBinder>();

                Assert.Throws<System.InvalidOperationException>(() => binder.Configure(new[]
                {
                    new ParameterChildConditionBinding(state, "$Stage == 0"),
                    new ParameterChildConditionBinding(state, "$Stage == 1")
                }));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Configure_TargetOutsideBinderHierarchyFails()
        {
            GameObject host = new GameObject("Binder Host");
            GameObject external = new GameObject("External");

            try
            {
                ParameterStateBinder binder = host.AddComponent<ParameterStateBinder>();

                Assert.Throws<System.InvalidOperationException>(() => binder.Configure(new[]
                {
                    new ParameterChildConditionBinding(external, "$Stage == 0")
                }));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(external);
            }
        }

        [Test]
        public void Configure_AfterInitializeFailsAndKeepsExistingBinding()
        {
            ParameterStore parameters = new ParameterStore(new[]
            {
                ParameterDefinition.Int("Stage", "Stage", 0, 0, 1)
            });
            GameObject host = new GameObject("Binder Host");
            GameObject stateA = CreateChild("A", host);
            GameObject stateB = CreateChild("B", host);

            try
            {
                ParameterStateBinder binder = host.AddComponent<ParameterStateBinder>();
                binder.Configure(new[]
                {
                    new ParameterChildConditionBinding(stateA, "$Stage == 0")
                });
                binder.Initialize(parameters);

                Assert.Throws<System.InvalidOperationException>(() => binder.Configure(new[]
                {
                    new ParameterChildConditionBinding(stateB, "$Stage == 1")
                }));

                parameters.Set("Stage", 1);
                Assert.That(stateA.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static GameObject CreateChild(string name, GameObject parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child;
        }

        private static void AssertState(
            GameObject stateA,
            GameObject stateB,
            GameObject warning,
            bool aIsActive,
            bool bIsActive,
            bool warningIsActive)
        {
            Assert.That(stateA.activeSelf, Is.EqualTo(aIsActive));
            Assert.That(stateB.activeSelf, Is.EqualTo(bIsActive));
            Assert.That(warning.activeSelf, Is.EqualTo(warningIsActive));
        }
    }

    public sealed class ParameterStateBinderTestBehaviour : MonoBehaviour
    {
    }
}
