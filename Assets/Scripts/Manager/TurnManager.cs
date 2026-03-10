using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private ITurnState currentState;

    public WaitInputState waitInputState;
    public ActionState actionState;
    public ApplyEffectState applyEffectState;
    public EndTurnState endTurnState;

    public string currentStateName = "Starting State";

    private void Awake()
    {
        waitInputState = new WaitInputState(this);
        actionState = new ActionState(this);
        applyEffectState = new ApplyEffectState(this);
        endTurnState = new EndTurnState(this);
    }

    private void Start()
    {
        ChangeState(waitInputState);
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(ITurnState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
}

