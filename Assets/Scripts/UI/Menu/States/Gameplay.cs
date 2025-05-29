

using UnityEngine;

namespace UI.Menu.States
{
    public class Gameplay : IState
    {
        public void Enter()
        {
            GameInput.InputReader.Instance.EnablePlayer();
        }

        public void Exit()
        {
            GameInput.InputReader.Instance.DisablePlayer();
        }

        public void Update(float deltaTime)
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                MenuManager.Instance.SetState(new Pause());
            }
        }
    }
}
