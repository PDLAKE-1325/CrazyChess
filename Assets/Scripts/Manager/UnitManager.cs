using UnityEngine;
using System.Collections.Generic;

public class UnitManager : MonoBehaviour
{
    public Unit cur_unit;
    [SerializeField] GameObject moveMarkerPrefab_white;
    [SerializeField] GameObject moveMarkerPrefab_black;
    [SerializeField] Transform markerParent;
    List<Vector2Int> CanMoves = new();

    private void Start()
    {
        GameStreamManager.Instance.MakeMarkerAction = MarkMovement;
        GameStreamManager.Instance.ClearMarkerAction = ClearMarks;

    }

    void Update()
    {
        CheckValidMoves();
    }

    void ClearMarks()
    {
        // Clear marked unit reference
        if (GameStreamManager.Instance != null) GameStreamManager.Instance.ClearMarkedUnit();

        foreach (Transform marker in markerParent)
        {
            Destroy(marker.gameObject);
        }
    }

    void MarkMovement()
    {
        // do not create markers while user is selecting a unit for item usage
        if (GameStreamManager.Instance != null && GameStreamManager.Instance.awaitingUnitSelection) return;
        ClearMarks();
        if (GameStreamManager.Instance != null) GameStreamManager.Instance.SetMarkedUnit(cur_unit);
        foreach (var move in CanMoves)
        {
            GameObject prefab = cur_unit.is_white_unit ? moveMarkerPrefab_white : moveMarkerPrefab_black;
            GameObject marker = Instantiate(prefab, markerParent);
            PathMarker pm = marker.GetComponent<PathMarker>();
            pm.x = move.x;
            pm.y = move.y;
        }
    }

    void CheckValidMoves()
    {
        if (!GameStreamManager.Instance.clickable) return;

        if (Input.GetMouseButtonDown(0))
        {
            var gsm = GameStreamManager.Instance;
            if (gsm == null) return;

            var selectedObj = gsm.cur_selected_unit;
            if (selectedObj == null) return;

            var unit = selectedObj.GetComponent<Unit>();
            if (unit == null) return;

            // 현재 선택 유닛 참조 보관
            cur_unit = unit;

            // UnitSO의 GetMoveMask을 사용하여 17x17 마스크 호출
            var so = unit.thisUnit;
            if (so == null)
            {
                Debug.LogWarning("thisUnit == null");
                return;
            }

            bool[,] mask = so.GetMoveMask(unit.x, unit.y, unit.is_white_unit, !unit.hasMoved, unit.equipped_item);
            if (mask == null)
            {
                Debug.Log("GetMoveMask가 없거나 null임");
                return;
            }

            // 킹이면 상대 공격 범위로 들어가는 이동은 제외 (킹이 체크를 피하도록)
            if (so.base_type == UnitType.King)
            {
                for (int i = 0; i < 17; i++)
                {
                    for (int j = 0; j < 17; j++)
                    {
                        if (!mask[i, j]) continue;
                        int bx = unit.x + (i - 8);
                        int by = unit.y + (j - 8);
                        if (bx < 0 || by < 0 || bx > 7 || by > 7) { mask[i, j] = false; continue; }
                        if (IsSquareAttacked(bx, by, !unit.is_white_unit))
                        {
                            mask[i, j] = false;
                        }
                    }
                }
            }

            // 완전 합법성 검사: 각 후보 이동을 시뮬레이션하여 자신의 킹이 체크 상태가 되는지 검사
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 17; j++)
                {
                    if (!mask[i, j]) continue;
                    int bx = unit.x + (i - 8);
                    int by = unit.y + (j - 8);
                    if (bx < 0 || by < 0 || bx > 7 || by > 7) { mask[i, j] = false; continue; }
                    if (!IsLegalMove(unit, bx, by))
                    {
                        mask[i, j] = false;
                    }
                }
            }

            List<Vector2Int> validMoves = new List<Vector2Int>();
            const int C = 8; // mask center index

            // mask -> board 좌표로 변환
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 17; j++)
                {
                    if (!mask[i, j]) continue;
                    int bx = unit.x + (i - C);
                    int by = unit.y + (j - C);
                    if (bx < 0 || by < 0 || bx > 7 || by > 7) continue;
                    validMoves.Add(new Vector2Int(bx, by));
                }
            }

            CanMoves = validMoves;

            // 디버그 출력: 좌표 리스트
            if (validMoves.Count == 0)
            {
                Debug.Log($"[{unit.name}] 유효한 이동 경로 없음");
            }
            else
            {
                string s = string.Join(", ", validMoves.ConvertAll(v => $"({v.x},{v.y})"));
                Debug.Log($"[{unit.name}] 이동가능 경로: {s}");
            }

            // 디버그 출력: 17x17 간단한 ASCII 그리드 ('.' 빈칸, 'X' 이동 가능, 'C'가 유닛 중심)
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int j = 16; j >= 0; j--)
            {
                for (int i = 0; i < 17; i++)
                {
                    if (i == C && j == C) sb.Append('▒');
                    else sb.Append(mask[i, j] ? '░' : '▓');
                }
                sb.AppendLine();
            }
            Debug.Log($"움직임 가능 경로 {unit.name}:\n" + sb.ToString());
            MarkMovement();
        }

        bool IsSquareAttacked(int tx, int ty, bool byWhite)
        {
            var g = GameStreamManager.Instance;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var u = g.board_data.board[x][y].unit;
                    if (u == null) continue;
                    if (u.is_white_unit != byWhite) continue;
                    var so = u.thisUnit;
                    if (so == null) continue;
                    bool useFirst = !u.hasMoved;
                    var mask = so.GetMoveMask(u.x, u.y, u.is_white_unit, useFirst, u.equipped_item);
                    int ix = 8 + (tx - u.x);
                    int iy = 8 + (ty - u.y);
                    if (ix < 0 || iy < 0 || ix > 16 || iy > 16) continue;
                    if (mask[ix, iy]) return true;
                }
            }
            return false;
        }

        // 유닛 이동을 시뮬레이션해서 자신의 왕이 체크되는지 검사한다. 안전하면 true.
        bool IsLegalMove(Unit unit, int toX, int toY)
        {
            var gsm = GameStreamManager.Instance;
            if (gsm == null) return false;

            // 저장
            int fromX = unit.x;
            int fromY = unit.y;
            var captured = gsm.board_data.board[toX][toY].unit;

            // 적용(시뮬레이션)
            gsm.board_data.board[fromX][fromY].unit = null;
            gsm.board_data.board[toX][toY].unit = unit;
            unit.x = toX;
            unit.y = toY;

            // 자신의 왕이 체크인지 검사
            bool kingUnderAttack = IsKingUnderAttackFor(unit.is_white_unit);

            // 복구
            unit.x = fromX;
            unit.y = fromY;
            gsm.board_data.board[fromX][fromY].unit = unit;
            gsm.board_data.board[toX][toY].unit = captured;

            return !kingUnderAttack;
        }

        // 자신의 왕이 공격받는지 검사 (ActionState의 IsKingUnderAttack와 유사하지만 여기선 자신 진영을 인자로 받음)
        bool IsKingUnderAttackFor(bool kingIsWhite)
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

            // 적군이 킹을 공격하는지 검사
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var u = gsm.board_data.board[x][y].unit;
                    if (u == null) continue;
                    if (u.is_white_unit == kingIsWhite) continue;
                    var so = u.thisUnit;
                    if (so == null) continue;
                    bool useFirst = !u.hasMoved;
                    var mask = so.GetMoveMask(u.x, u.y, u.is_white_unit, useFirst, u.equipped_item);
                    int ix = 8 + (kx - u.x);
                    int iy = 8 + (ky - u.y);
                    if (ix < 0 || iy < 0 || ix > 16 || iy > 16) continue;
                    if (mask[ix, iy]) return true;
                }
            }
            return false;
        }
    }
}
