using UnityEngine;
using UnityEngine.InputSystem;

namespace AurigaGames.Deep.Core
{
    public class ApplicationManager : Singleton<ApplicationManager>
    {
        private bool _cursorLocked;

        public static bool IsExiting { get; private set; }

        // TODO: rework cursor lock logic
        public bool IsLookAvailable => _cursorLocked && !Cursor.visible;
        
        private void OnEnable()
        {
            HideCursor();
        }

        private void OnDisable()
        {
            ShowCursor();
        }

        private void Update()
        {
            if (_cursorLocked && Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void ShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorLocked = false;
        }

        public void HideCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cursorLocked = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void OnSubsystemsInit()
        {
            IsExiting = false;
            Instance.WakeUp();
        }

        private void OnApplicationQuit()
        {
            IsExiting = true;
        }
    }
}
