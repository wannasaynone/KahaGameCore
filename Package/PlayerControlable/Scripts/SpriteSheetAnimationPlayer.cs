using UnityEngine;

namespace KahaGameCore.Package.PlayerControlable
{
    public class SpriteSheetAnimationPlayer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private int idleSpriteIndex = 1;
        [SerializeField] private float frameRate = 0.25f;
        [Header("Walk")]
        [SerializeField] private Sprite[] walk_up;
        [SerializeField] private Sprite[] walk_down;
        [SerializeField] private Sprite[] walk_left;
        [SerializeField] private Sprite[] walk_right;

        private int walkIndex = 0;

        private float frameChangeTimer = 0f;
        private Sprite idleSprite;
        private enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            None
        }
        private Direction currentDirection = Direction.None;

        private void OnEnable()
        {
            idleSprite = walk_down[idleSpriteIndex];
            Input.InputEventHanlder.Movement.OnMovingUp += OnMovingUp;
            Input.InputEventHanlder.Movement.OnMovingDown += OnMovingDown;
            Input.InputEventHanlder.Movement.OnMovingLeft += OnMovingLeft;
            Input.InputEventHanlder.Movement.OnMovingRight += OnMovingRight;
            Input.InputEventHanlder.Movement.OnReleased += OnReleased;
        }

        private void OnReleased()
        {
            currentDirection = Direction.None;
            spriteRenderer.sprite = idleSprite;
        }

        private void ResetWalkIndex()
        {
            walkIndex = idleSpriteIndex + 1;
            if (walkIndex >= walk_right.Length)
            {
                walkIndex = 0;
            }
            frameChangeTimer = 0f;
        }

        private void OnMovingRight()
        {
            if (currentDirection != Direction.Right)
            {
                ResetWalkIndex();
            }
            else
            {
                return;
            }
            Walk(walk_right);
            currentDirection = Direction.Right;
        }

        private void OnMovingLeft()
        {
            if (currentDirection != Direction.Left)
            {
                ResetWalkIndex();
            }
            else
            {
                return;
            }
            Walk(walk_left);
            currentDirection = Direction.Left;
        }

        private void OnMovingDown()
        {
            if (currentDirection != Direction.Down)
            {
                ResetWalkIndex();
            }
            else
            {
                return;
            }
            Walk(walk_down);
            currentDirection = Direction.Down;
        }

        private void OnMovingUp()
        {
            if (currentDirection != Direction.Up)
            {
                ResetWalkIndex();
            }
            else
            {
                return;
            }
            Walk(walk_up);
            currentDirection = Direction.Up;
        }

        private void OnDisable()
        {
            spriteRenderer.sprite = idleSprite;
            Input.InputEventHanlder.Movement.OnMovingUp -= OnMovingUp;
            Input.InputEventHanlder.Movement.OnMovingDown -= OnMovingDown;
            Input.InputEventHanlder.Movement.OnMovingLeft -= OnMovingLeft;
            Input.InputEventHanlder.Movement.OnMovingRight -= OnMovingRight;
            Input.InputEventHanlder.Movement.OnReleased -= OnReleased;
        }

        private void Update()
        {
            switch (currentDirection)
            {
                case Direction.Up:
                    Walk(walk_up);
                    break;
                case Direction.Down:
                    Walk(walk_down);
                    break;
                case Direction.Left:
                    Walk(walk_left);
                    break;
                case Direction.Right:
                    Walk(walk_right);
                    break;
                case Direction.None:
                    break;
            }
        }

        private void Walk(Sprite[] walkSprites)
        {
            if (frameChangeTimer > 0)
            {
                frameChangeTimer -= Time.deltaTime;
                return;
            }

            spriteRenderer.sprite = walkSprites[walkIndex];
            walkIndex = (walkIndex + 1) % walkSprites.Length;

            frameChangeTimer = frameRate;
            idleSprite = walkSprites[idleSpriteIndex];
        }
    }
}