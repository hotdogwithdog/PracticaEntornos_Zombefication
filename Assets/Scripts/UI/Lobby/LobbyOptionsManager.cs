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
        private Level.GameMode[] _gameModes;
        private int _gameModeIndex;

        private SliderController _maxTimeSldController;
        private SliderController _coinsDensitySldController;

        private void Start()
        {
             _gameModes = (Level.GameMode[])Enum.GetValues(typeof(Level.GameMode));
            _gameModeIndex = UnityEngine.Random.Range(0, _gameModes.Length);
            ChangeGameModeText(_gameModes[_gameModeIndex]);
            MenuManager.Instance.GameManager.SetGameModeRpc(_gameModes[_gameModeIndex]);

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
            // This ifs are because the clients are not subscribed (the object is disable and for that reason the Start is not call in the clients), but the destructor yes
            if (_gameModeMoves != null)
            {
                foreach (OptionMove optionMove in _gameModeMoves)
                {
                    optionMove.OnClick -= GameModeChange;
                }
            }
            if (_maxTimeSldController != null) _maxTimeSldController.OnDropSlider -= MaxTimeChange;
            if (_coinsDensitySldController != null) _coinsDensitySldController.OnDropSlider -= CoinsDensityChange;
        }

        private void ChangeGameModeText(Level.GameMode gameMode)
        {
            switch (gameMode)
            {
                case Level.GameMode.Tiempo:
                    _gameModeText.text = "Time";
                    break;
                case Level.GameMode.Monedas:
                    _gameModeText.text = "Coins";
                    break;
                default:
                    Debug.LogError($"ERROR UNKOWN GAMEMODE: {gameMode}");
                    return;
            }
        }

        private void CoinsDensityChange(float value, sldOptionToControl control)
        {
            Debug.Log($"The control of {control} has the value {value}");
            MenuManager.Instance.GameManager.SetCoinsDensityRpc(value);
        }

        private void MaxTimeChange(float value, sldOptionToControl control)
        {
            Debug.Log($"The control of {control} has the value {value}");
            MenuManager.Instance.GameManager.SetMaxTimeRpc(value);
        }


        private void GameModeChange(OptionMoveDir dir)
        {
            Debug.Log($"Direction of move the game mode is {dir}");
            if (dir == OptionMoveDir.Left )
            {
                _gameModeIndex--;
                if (_gameModeIndex < 0 ) _gameModeIndex = _gameModes.Length - 1;
            }
            else _gameModeIndex = (_gameModeIndex + 1) % _gameModes.Length;

            ChangeGameModeText(_gameModes[_gameModeIndex]);

            MenuManager.Instance.GameManager.SetGameModeRpc(_gameModes[_gameModeIndex]);
        }
    }
}
