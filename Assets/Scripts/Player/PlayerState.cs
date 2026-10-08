public abstract class PlayerState
{
    protected PlayerStateMachine stateMachine;

    public PlayerState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public abstract void Enter();
    public abstract void Tick();       // Runs every frame (Update)
    public abstract void FixedTick();  // Runs every physics step (FixedUpdate)
    public abstract void Exit();
}