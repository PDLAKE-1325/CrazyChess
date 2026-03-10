using UnityEngine;

public class EndTurnState : ITurnState
{
    private TurnManager manager;
    public EndTurnState(TurnManager manager) => this.manager = manager;

    public void Enter()
    {
        manager.currentStateName = "End Turn State";
        // Award an item to the player who just finished their turn
        var gsm = GameStreamManager.Instance;
        if (gsm != null)
        {
            // Award to the player who just finished their turn (previous player),
            // because turn flag was already toggled during move processing.
            bool previousPlayerWasWhite = !gsm.turn_white;
            if (previousPlayerWasWhite)
            {
                // If this is the white team's first completed turn, mark it and skip awarding an item.
                if (!gsm.firstTurnCompletedWhite)
                {
                    gsm.firstTurnCompletedWhite = true;
                    Debug.Log("White completed their first turn — no item awarded.");
                }
                else
                {
                    gsm.AddRandomItemToPlayer(true);
                }
            }
            else
            {
                // Black team
                if (!gsm.firstTurnCompletedBlack)
                {
                    gsm.firstTurnCompletedBlack = true;
                    Debug.Log("Black completed their first turn — no item awarded.");
                }
                else
                {
                    gsm.AddRandomItemToPlayer(false);
                }
            }
        }
    }
    public void Update()
    {
        manager.ChangeState(manager.waitInputState);
    }
    public void Exit() { }
}
