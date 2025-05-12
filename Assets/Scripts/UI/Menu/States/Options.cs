using UnityEditor;
using UnityEngine;

namespace UI.Menu.States
{
    internal class Options : AMenuState
    {
        public Options() : base("Menus/Options") { }

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
            MenuManager.Instance.SetState(new Main());
        }

        public override void Update(float deltaTime) { }
    }
}
