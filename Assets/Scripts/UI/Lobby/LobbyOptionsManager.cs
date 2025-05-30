using System;
using TMPro;
using UI.Menu;
using Unity.Netcode;
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
        private SliderController _numberOfRoomsSldController;
        private SliderController _roomWidthSldController;
        private SliderController _roomLenghtSldController;


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
                switch(controller.GetSldOptionToControl())
                {
                    case sldOptionToControl.MaxTime:
                        _maxTimeSldController = controller;
                        break;
                    case sldOptionToControl.CoinsDensity:
                        _coinsDensitySldController = controller;
                        break;
                    case sldOptionToControl.NumberOfRooms:
                        _numberOfRoomsSldController = controller;
                        break;
                    case sldOptionToControl.RoomWidth:
                        _roomWidthSldController = controller;
                        break;
                    case sldOptionToControl.RoomLenght:
                        _roomLenghtSldController = controller;
                        break;
                    default:
                        Debug.LogError($"ERROR UNKNOWN SLIDER TYPE: {controller.GetSldOptionToControl()}");
                        break;
                }
            }
            _maxTimeSldController.OnDropSlider += MaxTimeChange;
            _coinsDensitySldController.OnDropSlider += CoinsDensityChange;
            _numberOfRoomsSldController.OnDropSlider += NumberOfRoomsChange;
            _roomWidthSldController.OnDropSlider += RoomWidthChange;
            _roomLenghtSldController.OnDropSlider += RoomLenghtChange;

            if (MenuManager.Instance.GameManager.IsHostInit)
            {
                InitHost();
            }
            else
            {
                _maxTimeSldController.gameObject.SetActive(false);
                _coinsDensitySldController.gameObject.SetActive(false);
                _numberOfRoomsSldController.gameObject.SetActive(false);
                _roomWidthSldController.gameObject.SetActive(false);
                _roomLenghtSldController.gameObject.SetActive(false);

                MenuManager.Instance.GameManager.OnHostInit += InitHost;
            }
        }

        private void InitHost()
        {
            MenuManager.Instance.GameManager.SetMaxTimeRpc(300);
            MenuManager.Instance.GameManager.SetCoinsDensityRpc(30);
            MenuManager.Instance.GameManager.SetNumberOfRoomsRpc(4);
            MenuManager.Instance.GameManager.SetRoomWidthRpc(5);
            MenuManager.Instance.GameManager.SetRoomLenghtRpc(5);
            MenuManager.Instance.GameManager.SetGameModeRpc(_gameModes[_gameModeIndex]);

            _maxTimeSldController.gameObject.SetActive(true);
            _coinsDensitySldController.gameObject.SetActive(true);
            _numberOfRoomsSldController.gameObject.SetActive(true);
            _roomWidthSldController.gameObject.SetActive(true);
            _roomLenghtSldController.gameObject.SetActive(true);
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
            MenuManager.Instance.GameManager.OnHostInit -= InitHost;
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
        private void RoomLenghtChange(float value, sldOptionToControl control)
        {
            Debug.Log($"The control of {control} has the value {value}");
            if (((int)value) == 0) value++;    // The rooms must be even size
            MenuManager.Instance.GameManager.SetRoomLenghtRpc((int)value);
        }
        private void RoomWidthChange(float value, sldOptionToControl control)
        {
            Debug.Log($"The control of {control} has the value {value}");
            if (((int)value) % 2 == 0) value++;    // The rooms must be even size
            MenuManager.Instance.GameManager.SetRoomWidthRpc((int)value);
        }
        private void NumberOfRoomsChange(float value, sldOptionToControl control)
        {
            Debug.Log($"The control of {control} has the value {value}");
            MenuManager.Instance.GameManager.SetNumberOfRoomsRpc((int)value);
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
