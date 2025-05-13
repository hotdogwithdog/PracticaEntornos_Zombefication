using UnityEditor;
using UnityEngine;

namespace UI.Menu.States
{
    internal class Credits : AMenuState
    {
        public Credits() : base("Menus/Credits") { }

        protected override void OnOptionClicked(MenuButtons option)
        {
            switch (option)
            {
                case MenuButtons.Back:
                    MenuManager.Instance.SetState(new Main());
                    break;
                default:
                    Debug.LogError($"ERROR_UNKOWN_OPTION: {option}");
                    return;
            }
        }
        public override void Update(float deltaTime) { }
    }
}
