using KahaGameCore.Presentation.Editor;
using NUnit.Framework;

namespace KahaGameCore.Presentation.Tests
{
    public sealed class ParameterStateBinderEditorTests
    {
        [TestCase(
            false,
            0,
            (int)ParameterStateBinderEditor.SetupState.MissingEventCatalog)]
        [TestCase(
            true,
            0,
            (int)ParameterStateBinderEditor.SetupState.MissingConditionParameters)]
        [TestCase(
            true,
            1,
            (int)ParameterStateBinderEditor.SetupState.Ready)]
        public void GetSetupState_ReturnsExpectedState(
            bool hasEventCatalog,
            int conditionParameterCount,
            int expectedValue)
        {
            ParameterStateBinderEditor.SetupState expected =
                (ParameterStateBinderEditor.SetupState)expectedValue;
            Assert.That(
                ParameterStateBinderEditor.GetSetupState(
                    hasEventCatalog,
                    conditionParameterCount),
                Is.EqualTo(expected));
        }
    }
}
