using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class CharacterAnimationPlayer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Header("Walk")]
        [SerializeField] private Sprite[] walk_up;
        [SerializeField] private Sprite[] walk_down;
        [SerializeField] private Sprite[] walk_left;
        [SerializeField] private Sprite[] walk_right;

        private int walkIndex = 0;
        private float frameRate = 0.2f;
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

        private void Start()
        {
            idleSprite = walk_down[1];
        }

        private void Update()
        {
            if (InputDetector.IsMovingUp())
            {
                if (currentDirection != Direction.Up)
                {
                    walkIndex = 0;
                    frameChangeTimer = 0f;
                }
                Walk(walk_up);
                currentDirection = Direction.Up;
            }
            else if (InputDetector.IsMovingDown())
            {
                if (currentDirection != Direction.Down)
                {
                    walkIndex = 0;
                    frameChangeTimer = 0f;
                }
                Walk(walk_down);
                currentDirection = Direction.Down;
            }
            else if (InputDetector.IsMovingLeft())
            {
                if (currentDirection != Direction.Left)
                {
                    walkIndex = 0;
                    frameChangeTimer = 0f;
                }
                Walk(walk_left);
                currentDirection = Direction.Left;
            }
            else if (InputDetector.IsMovingRight())
            {
                if (currentDirection != Direction.Right)
                {
                    walkIndex = 0;
                    frameChangeTimer = 0f;
                }
                Walk(walk_right);
                currentDirection = Direction.Right;
            }
            else
            {
                spriteRenderer.sprite = idleSprite;
                currentDirection = Direction.None;
            }
        }

        private void Walk(Sprite[] walk_right)
        {
            if (frameChangeTimer > 0)
            {
                frameChangeTimer -= Time.deltaTime;
                return;
            }

            spriteRenderer.sprite = walk_right[walkIndex];
            walkIndex = (walkIndex + 1) % walk_right.Length;

            frameChangeTimer = frameRate;
            idleSprite = walk_right[1];
        }
    }
}