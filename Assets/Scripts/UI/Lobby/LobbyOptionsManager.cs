using System;
using TMPro;
using UI.Menu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Lobby
{
    public class LobbyOptionsManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gameModeText;
        private OptionMove[] _gameModeMoves;

        private SliderController _maxTimeSldController;
        private SliderController _coinsDensitySldController;

        private void Start()
        {
            _gameModeMoves = _gameModeText.gameObject.GetComponentsInChildren<OptionMove>();

            foreach (OptionMove optionMove in _gameModeMoves)
            {
                optionMove.OnClick += GameModeChange;
            }

            foreach (SliderController controller in GetComponentsInChildren<SliderController>())
            {
                if (controller.GetSldOptionToControl() == sldOptionToControl.MaxTime) _maxTimeSldController = controller;
                else if (controller.GetSldOptionToControl() == sldOptionToControl.CoinsDensity) _coinsDensitySldController = controller;
            }
            _maxTimeSldController.OnDropSlider += MaxTimeChange;
            _coinsDensitySldController.OnDropSlider += CoinsDensityChange;
        }
        private void OnDestroy()
        {
            foreach (OptionMove optionMove in _gameModeMoves)
            {
                optionMove.OnClick -= GameModeChange;
            }
            _maxTimeSldController.OnValueChanged -= MaxTimeChange;
            _coinsDensitySldController.OnValueChanged -= CoinsDensityChange;
        }

        private void CoinsDensityChange(float value, sldOptionToControl control)
        {
            Debug.Log($"The control of {control} has the value {value}");
            MenuManager.Instance.GameManager.SetCoinsDensity(value);
        }

        private void MaxTimeChange(float value, sldOptionToControl control)
        {
            Debug.Log($"The control of {control} has the value {value}");
            MenuManager.Instance.GameManager.SetMaxTime(value);
        }


        private void GameModeChange(OptionMoveDir dir)
        {
            Debug.Log($"Direction of move the game mode is {dir}");
            // cambiar el texto
            // cambiar el modo de juego en el GameManager
        }
    }
}
