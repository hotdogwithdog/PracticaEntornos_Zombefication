using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInput
{
    public class InputReader : Utilities.Singleton<InputReader>, Actions.IPlayerActions, Actions.ICheatsActions
    {
        private Actions _actions;

        // The delegate { } are just for initialice them to something and ignore the null check on the invokes

        #region PlayerMapDelegates
        public Action<Vector2> onMove = delegate { };
        public Action<Vector2> onCameraMove = delegate { };
        #endregion

        #region CheatsMapDelegates
        public Action onCameraSwitch = delegate { };
        public Action onHumanConvert = delegate { };
        public Action onZombieConvert = delegate { };
        #endregion

        #region EnablersAndDisablers
        private void OnEnable()
        {
            if (_actions == null)
            {
                _actions = new Actions();

                _actions.Player.SetCallbacks(this);
                _actions.Cheats.SetCallbacks(this);
            }

            EnablePlayer();

#if UNITY_EDITOR
            _actions.Cheats.Enable();
#endif
        }

        private void OnDisable()
        {
            DisableAllInput();
        }

        public void EnablePlayer()
        {
            _actions.Player.Enable();
            // Disable the rest of inputs
        }

        public void DisableAllInput()
        {
            _actions.Player.Disable();
        }
        #endregion

        #region PlayerMap
        public void OnCameraMove(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Canceled)
            {
                onCameraMove.Invoke(context.ReadValue<Vector2>());
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Canceled)
            {
                onMove.Invoke(context.ReadValue<Vector2>());
            }
        }
        #endregion

        #region CheatsMap
        public void OnCameraSwitch(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                onCameraSwitch.Invoke();
            }
        }
        public void OnHumanConvert(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                onHumanConvert.Invoke();
            }
        }
        public void OnZombieConvert(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                onZombieConvert.Invoke();
            }
        }
        #endregion
    }
}
