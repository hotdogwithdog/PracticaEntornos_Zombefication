using UnityEngine;
using UI.Menu.Navigation;

namespace UI.Menu.States
{
    internal abstract class AMenuState : IState
    {
        protected string _menuName;
        protected GameObject _menu;
        protected Canvas _canvas;

        public AMenuState(string menuName)
        {
            _menuName = menuName;
        }

        public virtual void Enter()
        {
            _canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();
            GameObject menuPrefab = (GameObject)Resources.Load(_menuName);
            _menu = GameObject.Instantiate(menuPrefab, _canvas.transform);

            _menu.GetComponentInChildren<MenuOptionsGroup>().onOptionClicked += OnOptionClicked;
        }

        protected abstract void OnOptionClicked(MenuButtons option);

        public virtual void Exit()
        {
            if (_menu != null)
            {
                _menu.GetComponentInChildren<MenuOptionsGroup>().onOptionClicked -= OnOptionClicked;
                GameObject.Destroy(_menu);
            }
        }

        public abstract void Update(float deltaTime);
    }
}
