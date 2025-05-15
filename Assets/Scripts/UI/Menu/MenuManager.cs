using UnityEngine;
using UI.Menu.States;
using Network;


namespace UI.Menu
{
    public class MenuManager : Utilities.Singleton<MenuManager>
    {
        // Context of the state pattern
        private IState _currentState;
        public GameManager GameManager {  get; private set; }

        private void Start()
        {
            GameManager = GetComponent<GameManager>();
            SetState(new Main());
        }

        public void SetState(IState newState)
        {
            _currentState?.Exit();

            _currentState = newState;

            _currentState.Enter();
        }

        public IState GetState() { return _currentState; }


        public void Update()
        {
            _currentState?.Update(Time.deltaTime);
        }

    }
}
