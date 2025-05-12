using UnityEditor;
using UnityEngine;

namespace UI.Menu.States
{
    internal class Main : AMenuState
    {
        public Main() : base("Menus/MainMenu") { }

        protected override void OnOptionClicked(MenuButtons option)
        {
            switch (option)
            {
                case MenuButtons.Lobby:
                    //MenuManager.Instance.SetState(new MapSelector());
                    Debug.Log("Looby clicked");
                    break;
                case MenuButtons.Options:
                    //MenuManager.Instance.SetState(new Options());
                    Debug.Log("Options clicked");
                    break;
                case MenuButtons.Credits:
                    //MenuManager.Instance.SetState(new Credits());
                    Debug.Log("Credits clicked");
                    break;
                case MenuButtons.Exit:
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
                default:
                    Debug.LogError($"ERROR_UNKOWN_OPTION: {option}");
                    return;
            }
        }
        public override void Update(float deltaTime) { }
    }
}
