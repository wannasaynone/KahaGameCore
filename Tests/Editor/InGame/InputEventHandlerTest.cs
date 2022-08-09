using KahaGameCore.Input;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class InputEventHandlerTest
    {
        [Test]
        public void singal_tap()
        {
            int counter = 0;
            InputEventHanlder.OnSingleTapped += delegate
            {
                counter++;
            };
            InputEventHanlder.SendOnSingleTapped();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void pressing()
        {
            int counter = 0;
            InputEventHanlder.OnPressing += delegate
            {
                counter++;
            };
            InputEventHanlder.SendOnPressing();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void swipe()
        {
            UnityEngine.Vector2 direction = default;
            InputEventHanlder.OnSwiped += delegate(UnityEngine.Vector2 dir)
            {
                direction = dir;
            };
            InputEventHanlder.SendOnSwiped(new UnityEngine.Vector2(1f, 1f));
            Assert.IsTrue(direction != default);
        }

        [Test]
        public void drag()
        {
            UnityEngine.Vector2 direction = default;
            InputEventHanlder.OnDrag += delegate (UnityEngine.Vector2 dir)
            {
                direction = dir;
            };
            InputEventHanlder.SendOnDraged(new UnityEngine.Vector2(1f, 1f));
            Assert.IsTrue(direction != default);
        }

        [Test]
        public void double_tap()
        {
            int counter = 0;
            InputEventHanlder.OnDoubleTapped += delegate
            {
                counter++;
            };
            InputEventHanlder.SendOnDoubleTapped();
            Assert.AreEqual(1, counter);
        }
    }
}
