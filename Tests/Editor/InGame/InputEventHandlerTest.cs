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
        public void Lock_Mouse()
        {
            object locker = new object();
            InputEventHanlder.LockMouse(locker);
            int counter = 0;
            InputEventHanlder.Mouse.OnSingleTapped += delegate
            {
                counter++;
            };
            InputEventHanlder.Mouse.OnPressing += delegate
            {
                counter++;
            };
            InputEventHanlder.Mouse.OnSwiped += delegate (UnityEngine.Vector2 dir)
            {
                counter++;
            };
            InputEventHanlder.Mouse.OnDrag += delegate (UnityEngine.Vector2 dir)
            {
                counter++;
            };
            InputEventHanlder.Mouse.RiseSingleTapped();
            InputEventHanlder.Mouse.RisePressing();
            InputEventHanlder.Mouse.RiseSwiped(new UnityEngine.Vector2(1f, 1f));
            InputEventHanlder.Mouse.RiseDrag(new UnityEngine.Vector2(1f, 1f));
            Assert.AreEqual(0, counter);

            InputEventHanlder.UnlockMouse(locker);

            InputEventHanlder.Mouse.RiseSingleTapped();
            InputEventHanlder.Mouse.RisePressing();
            InputEventHanlder.Mouse.RiseSwiped(new UnityEngine.Vector2(1f, 1f));
            InputEventHanlder.Mouse.RiseDrag(new UnityEngine.Vector2(1f, 1f));
            Assert.AreEqual(4, counter);
        }


        [Test]
        public void IsMovingUp()
        {
            int counter = 0;
            InputEventHanlder.Movement.OnMovingUp += delegate
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
            InputEventHanlder.Movement.OnMovingDown += delegate
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
            InputEventHanlder.Movement.OnMovingLeft += delegate
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
            InputEventHanlder.Movement.OnMovingRight += delegate
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
            InputEventHanlder.Movement.OnInteracting += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.RiseInteracting();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void Released()
        {
            int counter = 0;
            InputEventHanlder.Movement.OnReleased += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.RiseReleased();
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void Lock_Movement()
        {
            object locker = new object();
            InputEventHanlder.LockMovement(locker);
            int counter = 0;
            InputEventHanlder.Movement.OnMovingUp += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.OnMovingDown += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.OnMovingLeft += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.OnMovingRight += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.OnInteracting += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.OnReleased += delegate
            {
                counter++;
            };
            InputEventHanlder.Movement.RiseMovingUp();
            InputEventHanlder.Movement.RiseMovingDown();
            InputEventHanlder.Movement.RiseMovingLeft();
            InputEventHanlder.Movement.RiseMovingRight();
            InputEventHanlder.Movement.RiseInteracting();
            InputEventHanlder.Movement.RiseReleased();
            Assert.AreEqual(0, counter);

            InputEventHanlder.UnlockMovement(locker);

            InputEventHanlder.Movement.RiseMovingUp();
            InputEventHanlder.Movement.RiseMovingDown();
            InputEventHanlder.Movement.RiseMovingLeft();
            InputEventHanlder.Movement.RiseMovingRight();
            InputEventHanlder.Movement.RiseInteracting();
            InputEventHanlder.Movement.RiseReleased();
            Assert.AreEqual(6, counter);
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

        [Test]
        public void Lock_UserInterface()
        {
            object locker = new object();
            InputEventHanlder.LockUserInterface(locker);
            int counter = 0;
            InputEventHanlder.UserInterface.OnOptionInViewSelected += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.OnMoveToPreviousOptionInView += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.MoveToNextOptionInView += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.OnInventoryCalled += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.OnHideInventoryCalled += delegate
            {
                counter++;
            };
            InputEventHanlder.UserInterface.RiseOptionInViewSelected();
            InputEventHanlder.UserInterface.RiseMoveToPreviousOptionInView();
            InputEventHanlder.UserInterface.RiseMoveToNextOptionInView();
            InputEventHanlder.UserInterface.RiseInventoryCalled();
            InputEventHanlder.UserInterface.RiseHideInventoryCalled();
            Assert.AreEqual(0, counter);

            InputEventHanlder.UnlockUserInterface(locker);

            InputEventHanlder.UserInterface.RiseOptionInViewSelected();
            InputEventHanlder.UserInterface.RiseMoveToPreviousOptionInView();
            InputEventHanlder.UserInterface.RiseMoveToNextOptionInView();
            InputEventHanlder.UserInterface.RiseInventoryCalled();
            InputEventHanlder.UserInterface.RiseHideInventoryCalled();
            Assert.AreEqual(5, counter);
        }
    }
}
