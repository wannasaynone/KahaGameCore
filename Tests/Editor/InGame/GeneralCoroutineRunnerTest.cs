using NUnit.Framework;
using UnityEngine;
using KahaGameCore.Common;

namespace KahaGameCore.Tests
{
    public class GeneralCoroutineRunnerTest
    {
        [Test]
        public void Instance_should_return_same_object()
        {
            GeneralCoroutineRunner first = GeneralCoroutineRunner.Instance;
            GeneralCoroutineRunner second = GeneralCoroutineRunner.Instance;
            Assert.AreSame(first, second);
            Object.DestroyImmediate(first.gameObject);
        }
    }
}
