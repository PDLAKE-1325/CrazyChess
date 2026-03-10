using UnityEngine;

public class ActionState : ITurnState
{
    private TurnManager manager;
    public ActionState(TurnManager manager) => this.manager = manager;

    public void Enter()
    {
        manager.currentStateName = "Action State";
    }
    public void Update()
    {
        var gsm = GameStreamManager.Instance;
        if (gsm == null) manager.ChangeState(manager.applyEffectState);


        gsm.ClearMarkerAction();

        if (gsm.input_data.input_type == InputType.Move)
        {
            bool finished = ProcessMove(gsm.input_data);

            // If the move finished and there's no active promotion waiting, advance turn/state
            if (finished && !GameStreamManager.Instance.promotionActive)
            {
                gsm.ToggleTurn();
                gsm.SetInput(false);
                manager.ChangeState(manager.applyEffectState);
            }
            else if (GameStreamManager.Instance.promotionCompleted)
            {
                // Promotion was completed by UI; finish the move
                GameStreamManager.Instance.ClearPromotionCompleted();
                gsm.ToggleTurn();
                gsm.SetInput(false);
                manager.ChangeState(manager.applyEffectState);
            }
            else
            {
                // Move paused (e.g. waiting for promotion). Do not advance state here.
            }
        }
        else
        {
            manager.ChangeState(manager.applyEffectState);
        }
    }

    bool ProcessMove(InputData data)
    {
        var gsm = GameStreamManager.Instance;
        if (gsm == null) return true;

        var unit = data.target_unit;
        if (unit == null) return true;

        // If a promotion is active for this pawn, don't re-apply the move; we're waiting for UI selection
        if (gsm.promotionActive && gsm.pendingPromotionPawn == unit)
        {
            return false;
        }

        int toX = Mathf.RoundToInt(data.move_pos.x);
        int toY = Mathf.RoundToInt(data.move_pos.y);

        if (toX < 0 || toY < 0 || toX > 7 || toY > 7)
        {
            Debug.LogWarning("Invalid move target out of bounds");
            return true;
        }

        var so = unit.thisUnit;
        if (so == null)
        {
            Debug.LogWarning("UnitSO missing on unit when processing move.");
            return true;
        }

        bool useFirst = !unit.hasMoved;
        bool[,] mask = so.GetMoveMask(unit.x, unit.y, unit.is_white_unit, useFirst, unit.equipped_item);
        const int C = 8;
        int ix = C + (toX - unit.x);
        int iy = C + (toY - unit.y);
        if (ix < 0 || iy < 0 || ix > 16 || iy > 16 || !mask[ix, iy])
        {
            Debug.LogWarning($"Move to ({toX},{toY}) is not allowed for {unit.name}");
            return true;
        }

        // handle capture
        var target = gsm.board_data.board[toX][toY].unit;
        if (target != null && target != unit)
        {
            if (target.is_white_unit == unit.is_white_unit)
            {
                Debug.LogWarning("Attempt to capture own unit prevented.");
                return true;
            }
            else
            {
                Debug.Log($"{unit.name} captures {target.name} at ({toX},{toY})");
                // Remove captured unit
                gsm.board_data.board[toX][toY].unit = null;
                GameObject.Destroy(target.gameObject);
            }
        }

        // move on board
        gsm.board_data.board[unit.x][unit.y].unit = null;
        unit.x = toX;
        unit.y = toY;
        gsm.SetTileUnit(unit, toX, toY);
        unit.hasMoved = true;

        // 즉시 위치를 반영(필요하면 애니메이션 대신 바로 이동)
        if (unit.thisUnit != null)
        {
            Vector3 yAxis = new Vector3(0, unit.thisUnit.base_type != UnitType.Pawn ? 0.13f : 0, 0);
            unit.transform.position = UT_UnitMovements.GetPos(unit.x, unit.y) + yAxis;
        }

        Debug.Log($"Moved {unit.name} to ({toX},{toY})");

        // Promotion check: if pawn reached opponent's back rank, start promotion and pause further processing
        if (so.base_type == UnitType.Pawn)
        {
            bool reached = unit.is_white_unit ? (toY == 7) : (toY == 0);
            if (reached)
            {
                gsm.StartPromotion(unit);
                return false; // paused until promotion selection
            }
        }

        // 간단한 게임 종료 감지(상대가 합법적 움직임이 없으면 스테일/체크)
        bool opponentIsWhite = !unit.is_white_unit;
        bool opponentHasMoves = OpponentHasAnyMoves(opponentIsWhite);
        bool opponentKingInCheck = IsKingUnderAttack(opponentIsWhite, unit.is_white_unit);

        if (!opponentHasMoves)
        {
            if (opponentKingInCheck)
            {
                Debug.Log("Checkmate!");
                GameStreamManager.Instance.TriggerEndBoom(1); // checkmate
            }
            else
            {
                Debug.Log("Stalemate!");
                GameStreamManager.Instance.TriggerEndBoom(2); // stalemate
            }
        }
        else
        {
            if (opponentKingInCheck)
            {
                Debug.Log("Check!");
                GameStreamManager.Instance.CheckNotify();
                GameStreamManager.Instance.in_check = true;
            }
            else
            {
                GameStreamManager.Instance.in_check = false;

            }
        }

        return true;
    }

    bool OpponentHasAnyMoves(bool opponentIsWhite)
    {
        var gsm = GameStreamManager.Instance;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var u = gsm.board_data.board[x][y].unit;
                if (u == null) continue;
                if (u.is_white_unit != opponentIsWhite) continue;

                var so = u.thisUnit;
                if (so == null) continue;
                bool useFirst = !u.hasMoved;
                var mask = so.GetMoveMask(u.x, u.y, u.is_white_unit, useFirst, u.equipped_item);
                const int C = 8;
                for (int i = 0; i < 17; i++)
                {
                    for (int j = 0; j < 17; j++)
                    {
                        if (!mask[i, j]) continue;
                        int bx = u.x + (i - C);
                        int by = u.y + (j - C);
                        if (bx < 0 || by < 0 || bx > 7 || by > 7) continue;

                        // 시뮬레이션: 이 이동을 적용했을 때 이동을 시도하는 측의 왕이 체크 상태가 되는지 확인
                        var fromX = u.x;
                        var fromY = u.y;
                        var captured = gsm.board_data.board[bx][by].unit;

                        // 적용
                        gsm.board_data.board[fromX][fromY].unit = null;
                        gsm.board_data.board[bx][by].unit = u;
                        u.x = bx; u.y = by;

                        // 이동한 쪽(=u.is_white_unit)의 왕이 체크인지 검사
                        bool kingInCheckAfterMove = IsKingUnderAttack(u.is_white_unit, !u.is_white_unit);

                        // 복구
                        u.x = fromX; u.y = fromY;
                        gsm.board_data.board[fromX][fromY].unit = u;
                        gsm.board_data.board[bx][by].unit = captured;

                        if (!kingInCheckAfterMove)
                        {
                            // 합법적인 이동 하나라도 있으면 상대는 움직일 수 있음
                            return true;
                        }
                        // 아니면 불법 이동(왕이 체크되므로)으로 간주하고 계속 검사
                    }
                }
            }
        }
        return false;
    }

    bool IsKingUnderAttack(bool kingIsWhite, bool attackerIsWhite)
    {
        var gsm = GameStreamManager.Instance;
        // find king
        int kx = -1, ky = -1;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var u = gsm.board_data.board[x][y].unit;
                if (u == null) continue;
                if (u.thisUnit == null) continue;
                if (u.thisUnit.base_type == UnitType.King && u.is_white_unit == kingIsWhite)
                {
                    kx = x; ky = y; break;
                }
            }
            if (kx != -1) break;
        }
        if (kx == -1) return false;

        // check if any attacker can reach king
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var u = gsm.board_data.board[x][y].unit;
                if (u == null) continue;
                if (u.is_white_unit != attackerIsWhite) continue;
                var so = u.thisUnit;
                if (so == null) continue;
                bool useFirst = !u.hasMoved;
                var mask = so.GetMoveMask(u.x, u.y, u.is_white_unit, useFirst, u.equipped_item);
                const int C = 8;
                int ix = C + (kx - u.x);
                int iy = C + (ky - u.y);
                if (ix < 0 || iy < 0 || ix > 16 || iy > 16) continue;
                if (mask[ix, iy]) return true;
            }
        }
        return false;
    }
    public void Exit() { }
}