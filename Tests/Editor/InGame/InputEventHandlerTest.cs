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
            InputEventHanlder.Mouse.OnSingleTapped += delegate
            {
                counter++;
            };
            InputEventHanlder.Mouse.RiseSingleTapped();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void Pressing()
        {
            int counter = 0;
            InputEventHanlder.Mouse.OnPressing += delegate
            {
                counter++;
            };
            InputEventHanlder.Mouse.RisePressing();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void Swipe()
        {
            UnityEngine.Vector2 direction = default;
            InputEventHanlder.Mouse.OnSwiped += delegate (UnityEngine.Vector2 dir)
            {
                direction = dir;
            };
            InputEventHanlder.Mouse.RiseSwiped(new UnityEngine.Vector2(1f, 1f));
            Assert.IsTrue(direction != default);
        }

        [Test]
        public void Drag()
        {
            UnityEngine.Vector2 direction = default;
            InputEventHanlder.Mouse.OnDrag += delegate (UnityEngine.Vector2 dir)
            {
                direction = dir;
            };
            InputEventHanlder.Mouse.RiseDrag(new UnityEngine.Vector2(1f, 1f));
            Assert.IsTrue(direction != default);
        }

        [Test]
        public void Double_tap()
        {
            int counter = 0;
            InputEventHanlder.Mouse.OnDoubleTapped += delegate
            {
                counter++;
            };
            InputEventHanlder.Mouse.RiseDoubleTapped();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsMovingUp()
        {
            int counter = 0;
            InputEventHanlder.Movement.IsMovingUp += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.RiseMovingUp();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsMovingDown()
        {
            int counter = 0;
            InputEventHanlder.Movement.IsMovingDown += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.RiseMovingDown();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsMovingLeft()
        {
            int counter = 0;
            InputEventHanlder.Movement.IsMovingLeft += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.RiseMovingLeft();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsMovingRight()
        {
            int counter = 0;
            InputEventHanlder.Movement.IsMovingRight += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.RiseMovingRight();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void IsInteracting()
        {
            int counter = 0;
            InputEventHanlder.Movement.IsInteracting += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.RiseInteracting();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void SelectedOptionInView()
        {
            int counter = 0;
            InputEventHanlder.UserInterface.OnOptionInViewSelected += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.RiseOptionInViewSelected();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void MoveToPreviousOptionInView()
        {
            int counter = 0;
            InputEventHanlder.UserInterface.OnMoveToPreviousOptionInView += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.RiseMoveToPreviousOptionInView();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void MoveToNextOptionInView()
        {
            int counter = 0;
            InputEventHanlder.UserInterface.MoveToNextOptionInView += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.RiseMoveToNextOptionInView();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void InventoryCalled()
        {
            int counter = 0;
            InputEventHanlder.UserInterface.OnInventoryCalled += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.RiseInventoryCalled();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void HideInventoryCalled()
        {
            int counter = 0;
            InputEventHanlder.UserInterface.OnHideInventoryCalled += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.RiseHideInventoryCalled();
            Assert.AreEqual(1, counter);
        }
    }
}
