using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
public class GamepadDebugger : MonoBehaviour
{
    void Update()
    {
        if (Gamepad.current != null)
        {
            for (int i = 0; i < Gamepad.current.allControls.Count; i++)
            {
                var control = Gamepad.current.allControls[i];
                if (control is ButtonControl btn && btn.isPressed)
                {
                    Debug.Log($"Pressed: {control.name} Value: {btn.ReadValue()}");
                }
            }
        }
    }
}
