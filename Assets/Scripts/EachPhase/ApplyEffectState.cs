using UnityEngine;

public class ApplyEffectState : ITurnState
{
    private TurnManager manager;
    public ApplyEffectState(TurnManager manager) => this.manager = manager;

    public void Enter()
    {
        manager.currentStateName = "Apply Effect State";
    }
    public void Update()
    {
        var gsm = GameStreamManager.Instance;

        if (gsm.input_data.input_type == InputType.Effect)
        {
            // 효과 처리 로직: 현재는 단순 디버그 출력
            if (gsm.lastUsedItem != null)
            {
                Debug.Log($"ApplyEffectState: applied item {gsm.lastUsedItem.itemName} to {gsm.input_data.target_unit?.name}");
            }
            // Using an item consumes the current player's turn. Toggle the turn here
            // because Move processing already toggles the turn in ActionState.
            gsm.ToggleTurn();
            gsm.SetInput(false);
            manager.ChangeState(manager.endTurnState);
        }
        else
        {
            manager.ChangeState(manager.endTurnState);
        }
    }
    public void Exit() { }
}