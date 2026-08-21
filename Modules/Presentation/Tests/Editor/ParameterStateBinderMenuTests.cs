using KahaGameCore.Presentation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Presentation.Tests
{
    public sealed class ParameterStateBinderMenuTests
    {
        [Test]
        public void CreateParameterStateBinder_CreatesBinderChildAndSelectsIt()
        {
            GameObject parent = new GameObject("Parent") { layer = 8 };
            UnityEngine.Object previousSelection = Selection.activeObject;

            try
            {
                ParameterStateBinder binder =
                    ParameterStateBinderMenu.CreateParameterStateBinder(parent);

                Assert.That(binder.name, Is.EqualTo("Parameter State Binder"));
                Assert.That(binder.transform.parent, Is.EqualTo(parent.transform));
                Assert.That(binder.gameObject.layer, Is.EqualTo(parent.layer));
                Assert.That(
                    binder.GetComponent<ParameterStateBinder>(),
                    Is.EqualTo(binder));
                Assert.That(Selection.activeGameObject, Is.EqualTo(binder.gameObject));
            }
            finally
            {
                Selection.activeObject = previousSelection;
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }
    }
}
