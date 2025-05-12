namespace UI.Menu.States
{
    public interface IState
    {
        public void Enter();

        public void Exit();

        public void Update(float deltaTime);
    }
}
