using System;
using UnityEngine;

namespace KahaGameCore.Package.PlayerControlable
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerControlableObject : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;

        private Rigidbody2D rb;

        private enum MoveDirection
        {
            Up,
            Down,
            Left,
            Right,
            None
        }

        private MoveDirection moveDirection = MoveDirection.None;

        private void OnEnable()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

        }

        private void OnDisable()
        {

        }

        private void OnReleased()
        {
            moveDirection = MoveDirection.None;
        }

        private void OnMovingRight()
        {
            moveDirection = MoveDirection.Right;
        }

        private void OnMovingLeft()
        {
            moveDirection = MoveDirection.Left;
        }

        private void OnMovingDown()
        {
            moveDirection = MoveDirection.Down;
        }

        private void OnMovingUp()
        {
            moveDirection = MoveDirection.Up;
        }

        private void FixedUpdate()
        {
            if (moveDirection == MoveDirection.Up)
                rb.MovePosition(transform.position + speed * Time.deltaTime * Vector3.up);

            if (moveDirection == MoveDirection.Down)
                rb.MovePosition(transform.position + speed * Time.deltaTime * Vector3.down);

            if (moveDirection == MoveDirection.Left)
                rb.MovePosition(transform.position + speed * Time.deltaTime * Vector3.left);

            if (moveDirection == MoveDirection.Right)
                rb.MovePosition(transform.position + speed * Time.deltaTime * Vector3.right);
        }
    }
}