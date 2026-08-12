using KahaGameCore.Dialogue.DefaultImplements.Command;
using NUnit.Framework;

namespace KahaGameCore.Dialogue.Tests
{
    public sealed class DialogueCommandFactoryContainerTests
    {
        [Test]
        public void CreateDefault_ContainsBuiltInCommands()
        {
            DialogueCommandFactoryContainer container =
                DialogueCommandFactoryContainer.CreateDefault();

            Assert.That(container.GetDialogueCommand("Say"), Is.TypeOf<Say>());
            Assert.That(container.GetDialogueCommand("ScaleCharacter"), Is.TypeOf<ScaleCharacter>());
        }

        [Test]
        public void CreateDefault_AllowsAddingCustomCommand()
        {
            DialogueCommandFactoryContainer container =
                DialogueCommandFactoryContainer.CreateDefault();

            container.RegisterFactory("Custom", new CustomCommandFactory());

            Assert.That(container.GetDialogueCommand("Say"), Is.TypeOf<Say>());
            Assert.That(container.GetDialogueCommand("Custom"), Is.TypeOf<CustomCommand>());
        }

        private sealed class CustomCommand : DialogueCommandBase
        {
            public override void Process(string[] args, DialogueContext context)
            {
            }
        }

        private sealed class CustomCommandFactory : DialogueCommandFactoryBase
        {
            public override DialogueCommandBase Create()
            {
                return new CustomCommand();
            }
        }
    }
}
