using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KahaGameCore.Interaction.Tests
{
    public sealed class Interactable2DTests : InputTestFixture
    {
        private const int ActivatorLayer = 8;

        private readonly List<Object> created = new();
        private Keyboard keyboard;
        private InputActionAsset asset;
        private InputActionMap map;
        private InputActionReference up;
        private InputActionReference down;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            map = asset.AddActionMap("Test");
            up = InputActionReference.Create(map.AddAction("Up", InputActionType.Button, "<Keyboard>/upArrow"));
            down = InputActionReference.Create(map.AddAction("Down", InputActionType.Button, "<Keyboard>/downArrow"));
            map.Enable();
            created.Add(asset);
            created.Add(up);
            created.Add(down);
        }

        [TearDown]
        public override void TearDown()
        {
            map.Disable();
            foreach (Object item in created)
            {
                Object.DestroyImmediate(item);
            }

            created.Clear();
            base.TearDown();
        }

        [Test]
        public void Enter_OnlyActivatorLayersRaiseEntered()
        {
            Interactable2D interactable = CreateInteractable("Door", x: 0f, out _);
            var entered = new List<Collider2D>();
            interactable.Entered += entered.Add;

            Collider2D player = CreateActivator(x: 0f);
            Collider2D other = CreateActivator(x: 0f, layer: 0);
            Enter(interactable, player);
            Enter(interactable, other);

            Assert.That(entered, Is.EqualTo(new[] { player }));
        }

        [Test]
        public void Press_InRange_ShowsPromptAndRaisesInteractedWithBindingIndex()
        {
            Interactable2D interactable = CreateInteractable("Altar", x: 0f, out GameObject[] prompts);
            var presses = new List<(int, Collider2D)>();
            interactable.Interacted += (index, activator) => presses.Add((index, activator));
            Collider2D player = CreateActivator(x: 0f);

            Enter(interactable, player);
            Tick(interactable);
            Assert.That(prompts[0].activeSelf, Is.True);
            Assert.That(prompts[1].activeSelf, Is.True);

            Press(keyboard.downArrowKey);
            Tick(interactable);
            Release(keyboard.downArrowKey);
            Tick(interactable);

            Assert.That(presses, Is.EqualTo(new[] { (1, player) }));
        }

        [Test]
        public void DisabledAction_HidesPromptAndIgnoresKey()
        {
            Interactable2D interactable = CreateInteractable("Altar", x: 0f, out GameObject[] prompts);
            int presses = 0;
            interactable.Interacted += (_, _) => presses++;
            Enter(interactable, CreateActivator(x: 0f));

            map.Disable();
            Press(keyboard.upArrowKey);
            Tick(interactable);

            Assert.That(presses, Is.Zero);
            Assert.That(prompts[0].activeSelf, Is.False);
        }

        [Test]
        public void DisabledBinding_HidesItsPromptAndIgnoresItsKeyOnly()
        {
            Interactable2D interactable = CreateInteractable("Altar", x: 0f, out GameObject[] prompts);
            var indices = new List<int>();
            interactable.Interacted += (index, _) => indices.Add(index);
            Enter(interactable, CreateActivator(x: 0f));
            interactable.SetBindingEnabled(1, false);

            Press(keyboard.downArrowKey);
            Tick(interactable);
            Release(keyboard.downArrowKey);
            Press(keyboard.upArrowKey);
            Tick(interactable);

            Assert.That(indices, Is.EqualTo(new[] { 0 }));
            Assert.That(prompts[0].activeSelf, Is.True);
            Assert.That(prompts[1].activeSelf, Is.False);
        }

        [Test]
        public void Focus_NearestInteractableAnswersAndTheOtherHidesItsPrompt()
        {
            Interactable2D near = CreateInteractable("Near", x: 0f, out GameObject[] nearPrompts);
            Interactable2D far = CreateInteractable("Far", x: 2f, out GameObject[] farPrompts);
            int nearPresses = 0;
            int farPresses = 0;
            near.Interacted += (_, _) => nearPresses++;
            far.Interacted += (_, _) => farPresses++;
            Collider2D player = CreateActivator(x: 0.5f);
            Enter(far, player);
            Enter(near, player);

            Press(keyboard.upArrowKey);
            Tick(near);
            Tick(far);

            Assert.That(nearPresses, Is.EqualTo(1));
            Assert.That(farPresses, Is.Zero);
            Assert.That(nearPrompts[0].activeSelf, Is.True);
            Assert.That(farPrompts[0].activeSelf, Is.False);
        }

        [Test]
        public void Focus_EnterOnlyInteractablesDoNotCompete()
        {
            Interactable2D zone = CreateInteractable("Zone", x: 0f, out _);
            zone.Configure(1 << ActivatorLayer, new InteractionBinding[0]);
            Interactable2D altar = CreateInteractable("Altar", x: 2f, out GameObject[] prompts);
            int presses = 0;
            altar.Interacted += (_, _) => presses++;
            Collider2D player = CreateActivator(x: 0.2f);
            Enter(zone, player);
            Enter(altar, player);

            Press(keyboard.upArrowKey);
            Tick(altar);

            Assert.That(presses, Is.EqualTo(1));
            Assert.That(prompts[0].activeSelf, Is.True);
        }

        [Test]
        public void Awake_MakesTheColliderATrigger()
        {
            var host = new GameObject("Volume");
            created.Add(host);
            BoxCollider2D volume = host.AddComponent<BoxCollider2D>();
            volume.isTrigger = false;
            Interactable2D interactable = host.AddComponent<Interactable2D>();
            interactable.Configure(1 << ActivatorLayer, new InteractionBinding[0]);

            Invoke(interactable, "Awake");

            Assert.That(volume.isTrigger, Is.True);
        }

        [Test]
        public void Disabled_IgnoresEnter()
        {
            Interactable2D interactable = CreateInteractable("Door", x: 0f, out _);
            int entered = 0;
            interactable.Entered += _ => entered++;
            interactable.enabled = false;

            Enter(interactable, CreateActivator(x: 0f));

            Assert.That(entered, Is.Zero);
        }

        [Test]
        public void Focus_OnATieTheLastEnteredWins()
        {
            Interactable2D first = CreateInteractable("First", x: 0f, out _);
            Interactable2D second = CreateInteractable("Second", x: 0f, out _);
            Collider2D player = CreateActivator(x: 0f);

            Enter(first, player);
            Enter(second, player);

            Assert.That(second.HasFocusOf(player), Is.True);
            Assert.That(first.HasFocusOf(player), Is.False);
        }

        [Test]
        public void Exit_HidesPromptAndStopsAnswering()
        {
            Interactable2D interactable = CreateInteractable("Door", x: 0f, out GameObject[] prompts);
            int presses = 0;
            interactable.Interacted += (_, _) => presses++;
            Collider2D player = CreateActivator(x: 0f);
            Enter(interactable, player);
            Tick(interactable);

            Exit(interactable, player);
            Press(keyboard.upArrowKey);
            Tick(interactable);

            Assert.That(presses, Is.Zero);
            Assert.That(prompts[0].activeSelf, Is.False);
        }

        private Interactable2D CreateInteractable(string name, float x, out GameObject[] prompts)
        {
            var host = new GameObject(name);
            host.transform.position = new Vector3(x, 0f, 0f);
            created.Add(host);
            prompts = new[] { new GameObject(name + " Up"), new GameObject(name + " Down") };
            created.AddRange(prompts);

            host.AddComponent<BoxCollider2D>();
            Interactable2D interactable = host.AddComponent<Interactable2D>();
            interactable.Configure(
                1 << ActivatorLayer,
                new[]
                {
                    new InteractionBinding(up, prompts[0]),
                    new InteractionBinding(down, prompts[1])
                });
            Invoke(interactable, "OnEnable");
            return interactable;
        }

        private Collider2D CreateActivator(float x, int layer = ActivatorLayer)
        {
            var activator = new GameObject("Activator") { layer = layer };
            activator.transform.position = new Vector3(x, 0f, 0f);
            created.Add(activator);
            return activator.AddComponent<BoxCollider2D>();
        }

        private static void Enter(Interactable2D interactable, Collider2D activator)
        {
            Invoke(interactable, "OnTriggerEnter2D", activator);
        }

        private static void Exit(Interactable2D interactable, Collider2D activator)
        {
            Invoke(interactable, "OnTriggerExit2D", activator);
        }

        private static void Tick(Interactable2D interactable)
        {
            Invoke(interactable, "Update");
        }

        private static void Invoke(Interactable2D target, string method, params object[] arguments)
        {
            MethodInfo info = typeof(Interactable2D).GetMethod(
                method,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, method);
            info.Invoke(target, arguments);
        }
    }
}
