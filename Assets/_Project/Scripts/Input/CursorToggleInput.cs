using _Project.Scripts.GameFlow;
using UnityEngine;

namespace _Project.Scripts.Input
{
    public sealed class CursorToggleInput : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private void Update()
        {
            if (!UnityEngine.Input.GetKeyDown(toggleKey))
            {
                return;
            }

            if (CursorStateService.Instance == null)
            {
                Debug.LogWarning("CursorToggleInput: CursorStateService instance not found.", this);
                return;
            }

            CursorStateService.Instance.ToggleMode();
        }
    }
}
