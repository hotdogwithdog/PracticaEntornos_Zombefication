using System;
using UnityEngine;

namespace UI.Menu.Navigation
{
    public class MenuOptionsGroup : MonoBehaviour
    {
        public Action<MenuButtons> onOptionClicked;
        private MenuOption[] _options;

        private void Start()
        {
            _options = GetComponentsInChildren<MenuOption>();

            foreach (MenuOption option in _options)
            {
                option.onOptionClicked += OnOptionClicked;
            }
        }

        private void OnOptionClicked(MenuButtons option)
        {
            onOptionClicked?.Invoke(option);
        }
    }
}
