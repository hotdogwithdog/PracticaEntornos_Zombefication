using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Lobby
{
    public enum sldOptionToControl
    {
        None = 0,
        MaxTime,
        CoinsDensity,
        NumberOfRooms,
        RoomWidth,
        RoomLenght
    }

    public class SliderController : MonoBehaviour, IDragHandler, IDropHandler
    {
        [SerializeField] private sldOptionToControl _optionToControl;
        private Slider _slider;

        public Action<float, sldOptionToControl> OnValueChanged;
        public Action<float, sldOptionToControl> OnDropSlider;

        private void Start()
        {
            _slider = GetComponent<Slider>();
        }
        public void OnDrag(PointerEventData eventData)
        {
            OnValueChanged?.Invoke(_slider.value, _optionToControl);
        }
        public sldOptionToControl GetSldOptionToControl() { return _optionToControl; }

        public void OnDrop(PointerEventData eventData)
        {
            OnDropSlider?.Invoke(_slider.value, _optionToControl);
        }
    }
}
