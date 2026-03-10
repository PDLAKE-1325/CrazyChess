using UnityEngine;

public class WaitInputState : ITurnState
{
    private TurnManager manager;
    public WaitInputState(TurnManager manager) => this.manager = manager;

    public void Enter()
    {
        var gsm = GameStreamManager.Instance;
        if (gsm != null && gsm.itemSelectingActive)
            gsm.SetClickable(false);
        else
            GameStreamManager.Instance.SetClickable(true);

        manager.currentStateName = "입력대기 : " + (GameStreamManager.Instance.turn_white ? "백" : "흑");

        if (gsm != null && gsm.itemUIParent != null && gsm.itemUIPrefab != null)
        {
            gsm.PopulateItemsUI(gsm.itemUIParent, gsm.itemUIPrefab);
        }
        // Evaluate combine availability and toggle combine button if configured
    }
    public void Update()
    {
        if (GameStreamManager.Instance != null)
            GameStreamManager.Instance.EvaluateCombineAvailability();
        if (GameStreamManager.Instance.input)
        {
            manager.ChangeState(manager.actionState);
        }
    }
    public void Exit()
    {
        GameStreamManager.Instance.SetClickable(false);
    }
}