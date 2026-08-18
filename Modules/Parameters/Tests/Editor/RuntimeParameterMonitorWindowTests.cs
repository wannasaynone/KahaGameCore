using System.Reflection;
using KahaGameCore.Parameters.Editor;
using NUnit.Framework;
using UnityEngine;

namespace KahaGameCore.Parameters.Tests
{
    public sealed class RuntimeParameterMonitorWindowTests
    {
        [Test]
        public void LoadDocumentationAsset_ReturnsParametersReadme()
        {
            MethodInfo loadDocumentationAsset =
                typeof(RuntimeParameterMonitorWindow).GetMethod(
                    "LoadDocumentationAsset",
                    BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(loadDocumentationAsset, Is.Not.Null);

            Object documentation =
                (Object)loadDocumentationAsset.Invoke(null, null);

            Assert.That(documentation, Is.Not.Null);
            Assert.That(documentation.name, Is.EqualTo("README"));
        }
    }
}
