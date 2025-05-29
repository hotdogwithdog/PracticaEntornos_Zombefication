using UnityEditor;
using UnityEngine;

namespace UI.Menu.States
{
    internal class Options : AMenuState
    {
        private bool isMainMenu;
        public Options(bool isMainMenu = true) : base("Menus/Options")
        {
            this.isMainMenu = isMainMenu;
        }

        protected override void OnOptionClicked(MenuButtons option)
        {
            switch (option)
            {
                case MenuButtons.Back:
                    Back();
                    break;
                default:
                    Debug.LogError($"ERROR_UNKOWN_OPTION: {option}");
                    return;
            }
        }

        private void Back()
        {
            if (isMainMenu)
            {
                MenuManager.Instance.SetState(new Main());
            }
            else MenuManager.Instance.SetState(new Gameplay());
        }

        public override void Update(float deltaTime) { }
    }
}
