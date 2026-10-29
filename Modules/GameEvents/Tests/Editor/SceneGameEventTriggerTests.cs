using System.Collections.Generic;
using System.Threading;
using KahaGameCore.Interaction;
using KahaGameCore.Parameters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using static KahaGameCore.GameEvents.Tests.SceneGameEventTriggerTestSupport;

namespace KahaGameCore.GameEvents.Tests
{
    public sealed class SceneGameEventTriggerTests : InputTestFixture
    {
        private readonly List<Object> created = new();
        private ParameterStore parameters;
        private GameEventRunner runner;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            parameters = CreateParameters();
            runner = CreateRunner(parameters);
        }

        [TearDown]
        public override void TearDown()
        {
            foreach (Object item in created)
            {
                Object.DestroyImmediate(item);
            }

            created.Clear();
            base.TearDown();
        }

        [Test]
        public void Enter_WithoutBindings_RunsTheGameEvent()
        {
            Interactable3D interactable = CreateTrigger(bindingInput: null);

            Invoke(interactable, "OnTriggerEnter", CreateActivator(ActivatorLayer));
            WaitForEvents();

            Assert.That(parameters.GetInt("Stage"), Is.EqualTo(1));
        }

        [Test]
        public void Enter_FromAnotherLayer_DoesNothing()
        {
            Interactable3D interactable = CreateTrigger(bindingInput: null);

            Invoke(interactable, "OnTriggerEnter", CreateActivator(layer: 9));
            WaitForEvents();

            Assert.That(parameters.GetInt("Stage"), Is.Zero);
        }

        [Test]
        public void WithBinding_WaitsForTheKeyBeforeRunning()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = actions.AddActionMap("Test");
            InputActionReference confirm = InputActionReference.Create(
                map.AddAction("Confirm", InputActionType.Button, "<Keyboard>/upArrow"));
            map.Enable();
            created.Add(actions);
            created.Add(confirm);
            Interactable3D interactable = CreateTrigger(confirm);

            Invoke(interactable, "OnTriggerEnter", CreateActivator(ActivatorLayer));
            WaitForEvents();
            Assert.That(parameters.GetInt("Stage"), Is.Zero);

            Press(keyboard.upArrowKey);
            Invoke(interactable, "Update");
            WaitForEvents();
            map.Disable();

            Assert.That(parameters.GetInt("Stage"), Is.EqualTo(1));
        }

        private Interactable3D CreateTrigger(InputActionReference bindingInput)
        {
            var host = new GameObject("SceneGameEventTrigger Host");
            created.Add(host);
            TextAsset file = CreateEvent("50000000-0000-0000-0000-000000000001");
            created.Add(file);

            host.AddComponent<BoxCollider>();
            SceneGameEventTrigger trigger = host.AddComponent<SceneGameEventTrigger>();
            Interactable3D interactable = host.GetComponent<Interactable3D>();
            Assert.That(interactable, Is.Not.Null, "RequireComponent adds the Interactable3D.");
            interactable.Configure(
                1 << ActivatorLayer,
                bindingInput == null
                    ? new InteractionBinding[0]
                    : new[] { new InteractionBinding(bindingInput, prompt: null) });
            trigger.Configure(file);
            trigger.Initialize(runner, new EventContext(CancellationToken.None));
            Invoke(trigger, "Awake");
            Invoke(interactable, "OnEnable");
            return interactable;
        }

        private Collider CreateActivator(int layer)
        {
            var activator = new GameObject("Activator") { layer = layer };
            created.Add(activator);
            return activator.AddComponent<BoxCollider>();
        }

        private void WaitForEvents()
        {
            runner.WaitUntilIdleAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
