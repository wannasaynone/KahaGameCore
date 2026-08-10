using System;
using System.Linq;
using System.Reflection;
using KahaGameCore.Dialogue.View;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Dialogue.Tests
{
    public sealed class CGLayerTests
    {
        private static readonly FieldInfo SpeedStateChangedField =
            typeof(DialogueView).GetField(
                "OnSpeedStateChanged",
                BindingFlags.Static | BindingFlags.NonPublic);

        [Test]
        public void DisableAndReenable_MaintainsSingleSpeedStateSubscription()
        {
            Assert.That(SpeedStateChangedField, Is.Not.Null);
            object originalHandlers = SpeedStateChangedField.GetValue(null);
            GameObject host = new GameObject("CG Layer");
            host.SetActive(false);
            CGLayer layer = host.AddComponent<CGLayer>();

            try
            {
                Assert.That(CountSubscriptions(layer), Is.Zero);

                InvokeLifecycle(layer, "OnEnable");
                Assert.That(CountSubscriptions(layer), Is.EqualTo(1));

                InvokeLifecycle(layer, "OnDisable");
                Assert.That(CountSubscriptions(layer), Is.Zero);

                InvokeLifecycle(layer, "OnEnable");
                Assert.That(CountSubscriptions(layer), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                SpeedStateChangedField.SetValue(null, originalHandlers);
            }
        }

        private static int CountSubscriptions(CGLayer layer)
        {
            Delegate handlers = SpeedStateChangedField.GetValue(null) as Delegate;
            return handlers == null
                ? 0
                : handlers.GetInvocationList()
                    .Count(handler => ReferenceEquals(handler.Target, layer));
        }

        private static void InvokeLifecycle(CGLayer layer, string methodName)
        {
            MethodInfo method = typeof(CGLayer).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"CGLayer requires {methodName}().");
            method.Invoke(layer, null);
        }
    }
}
