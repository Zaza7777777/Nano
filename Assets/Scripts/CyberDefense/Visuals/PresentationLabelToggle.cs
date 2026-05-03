using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CyberDefense.Visuals
{
    public sealed class PresentationLabelToggle : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private bool logToggle;

        private void Update()
        {
            if (WasTogglePressed())
            {
                EntityVisualController.LabelsVisible = !EntityVisualController.LabelsVisible;
                if (logToggle)
                {
                    Debug.Log($"Presentation labels: {(EntityVisualController.LabelsVisible ? "ON" : "OFF")}");
                }
            }
        }

        private bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(toggleKey);
#else
            return false;
#endif
        }
    }
}
