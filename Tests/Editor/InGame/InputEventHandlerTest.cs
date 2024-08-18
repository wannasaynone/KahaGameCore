using KahaGameCore.Input;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public class InputEventHandlerTest
    {
        [Test]
        public void Singal_tap()
        {
            int counter = 0;
            InputEventHanlder.OnSingleTapped += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseSingleTapped();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void Pressing()
        {
            int counter = 0;
            InputEventHanlder.OnPressing += delegate
            {
                counter++;
            };
            InputEventHanlder.RisePressing();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void Swipe()
        {
            UnityEngine.Vector2 direction = default;
            InputEventHanlder.OnSwiped += delegate (UnityEngine.Vector2 dir)
            {
                direction = dir;
            };
            InputEventHanlder.RiseSwiped(new UnityEngine.Vector2(1f, 1f));
            Assert.IsTrue(direction != default);
        }

        [Test]
        public void Drag()
        {
            UnityEngine.Vector2 direction = default;
            InputEventHanlder.OnDrag += delegate (UnityEngine.Vector2 dir)
            {
                direction = dir;
            };
            InputEventHanlder.RiseDrag(new UnityEngine.Vector2(1f, 1f));
            Assert.IsTrue(direction != default);
        }

        [Test]
        public void Double_tap()
        {
            int counter = 0;
            InputEventHanlder.OnDoubleTapped += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseDoubleTapped();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsMovingUp()
        {
            int counter = 0;
            InputEventHanlder.IsMovingUp += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseMovingUp();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsMovingDown()
        {
            int counter = 0;
            InputEventHanlder.IsMovingDown += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseMovingDown();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsMovingLeft()
        {
            int counter = 0;
            InputEventHanlder.IsMovingLeft += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseMovingLeft();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsMovingRight()
        {
            int counter = 0;
            InputEventHanlder.IsMovingRight += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseMovingRight();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsInteracting()
        {
            int counter = 0;
            InputEventHanlder.IsInteracting += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseInteracting();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void SelectedOptionInView()
        {
            int counter = 0;
            InputEventHanlder.OnOptionInViewSelected += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseOptionInViewSelected();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void MoveToPreviousOptionInView()
        {
            int counter = 0;
            InputEventHanlder.OnMoveToPreviousOptionInView += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseMoveToPreviousOptionInView();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void MoveToNextOptionInView()
        {
            int counter = 0;
            InputEventHanlder.MoveToNextOptionInView += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseMoveToNextOptionInView();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void InventoryCalled()
        {
            int counter = 0;
            InputEventHanlder.OnInventoryCalled += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseInventoryCalled();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void HideInventoryCalled()
        {
            int counter = 0;
            InputEventHanlder.OnHideInventoryCalled += delegate
            {
                counter++;
            };
            InputEventHanlder.RiseHideInventoryCalled();
            Assert.AreEqual(1, counter);
        }
    }
}
