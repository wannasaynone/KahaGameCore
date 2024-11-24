using UnityEngine;

namespace KahaGameCore.Package.PlayerControlable
{
    [CreateAssetMenu(menuName = "Kaha Game Core/Player Controlable/InputDetector_Keyboard")]
    public class InputDetector_Keyboard : InputDetector
    {
        public override void Tick()
        {
            IsPressedUp = Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
            IsPressingUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            IsPressedDown = Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow);
            IsPressingDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            IsPressedLeft = Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow);
            IsPressingLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            IsPressedRight = Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow);
            IsPressingRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            Selected = Input.GetKeyUp(KeyCode.Space);
        }

        public override void Reset()
        {
            IsPressedUp = false;
            IsPressingUp = false;
            IsPressedDown = false;
            IsPressingDown = false;
            IsPressedLeft = false;
            IsPressingLeft = false;
            IsPressedRight = false;
            IsPressingRight = false;
            Selected = false;
        }
    }
}