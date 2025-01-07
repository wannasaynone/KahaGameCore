using System.Collections;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;
public class TestGameEvent : GameEventBase { }
public class Example : MonoBehaviour
{
    private void Start()
    {
        EventBus.Subscribe<TestGameEvent>(OnTestGameEventReceived);
        EventBus.Publish(new TestGameEvent());
    }

    private void OnTestGameEventReceived(TestGameEvent e)
    {
        EventBus.Unsubscribe<TestGameEvent>(OnTestGameEventReceived);
        Debug.Log("TestGameEvent received");
    }
}
