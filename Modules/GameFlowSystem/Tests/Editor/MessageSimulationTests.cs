using System;
using System.Collections.Generic;
using KahaGameCore.Foundation.Messaging;
using KahaGameCore.Foundation.Messaging.Editor;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;
using NUnit.Framework;

namespace KahaGameCore.GameFlowSystem.Tests
{
    public class MessageSimulationTests
    {
        [TearDown]
        public void TearDown()
        {
            MessageBus.ForceClearAll();
        }

        [Test]
        public void every_production_message_has_a_supported_automatic_form()
        {
            foreach (Type messageType in MessageTypeDiscovery.FindMessageTypes())
            {
                AutomaticMessageForm form = new AutomaticMessageForm(messageType);

                Assert.That(
                    form.IsSupported,
                    Is.True,
                    $"{messageType.FullName}: {form.UnsupportedReason}");
            }
        }

        [Test]
        public void every_automatic_form_creates_its_message_without_a_definition()
        {
            foreach (Type messageType in MessageTypeDiscovery.FindMessageTypes())
            {
                AutomaticMessageForm form = new AutomaticMessageForm(messageType);

                Assert.That(
                    form.TryCreateMessage(out MessageBase message, out string error),
                    Is.True,
                    error);
                Assert.That(message.GetType(), Is.EqualTo(messageType));
            }
        }

        [Test]
        public void automatic_publisher_dispatches_by_the_concrete_message_type()
        {
            int receivedCount = 0;
            MessageBus.Subscribe<MonologueRequestedEvent>(_ => receivedCount++);
            AutomaticMessageForm form = new AutomaticMessageForm(typeof(MonologueRequestedEvent));
            Assert.That(
                form.TryCreateMessage(out MessageBase message, out string error),
                Is.True,
                error);

            AutomaticMessagePublisher.Publish(message);

            Assert.That(receivedCount, Is.EqualTo(1));
        }
    }
}
