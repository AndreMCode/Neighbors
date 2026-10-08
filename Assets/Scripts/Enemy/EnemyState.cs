public abstract class EnemyState
{
    protected EnemyStateMachine stateMachine;

    public EnemyState(EnemyStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public abstract void Enter();
    public abstract void Tick();       // Runs in Update()
    public abstract void FixedTick();  // Runs in FixedUpdate()
    public abstract void Exit();
}