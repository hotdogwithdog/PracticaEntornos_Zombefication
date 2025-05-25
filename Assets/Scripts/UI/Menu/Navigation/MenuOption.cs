using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Menu.Navigation
{
    public class MenuOption : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private MenuButtons _action;
        public MenuButtons Action { get { return _action; } }

        public Action<MenuButtons> onOptionClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            onOptionClicked?.Invoke(_action);
        }
    }
}
