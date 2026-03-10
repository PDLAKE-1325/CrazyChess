using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "UnitSO", menuName = "Scriptable Objects/UnitSO")]
public class UnitSO : ScriptableObject
{
    public UnitType base_type;
    // public Vector2 position;

    public ItemSO equipped_item;

    // 기물을 넘을 수 있는지 여부
    public bool can_pass_over;

    // 상대 좌표(또는 절대 상대 위치) 목록. 예: (1,0),(2,0),(3,0) 등
    public List<Vector2> catchable_tiles;
    public List<Vector2> moveable_tiles_first;
    public List<Vector2> moveable_tiles;

    // 유닛을 중앙(8,8)으로 두고 17x17 그리드로 이동 가능 타일을 계산하여 반환합니다.
    // originX/Y는 보드 좌표(0..7)에서의 유닛 위치입니다.
    // unitIsWhite: 호출하는 Unit의 진영(흰=true, 검=false)
    // useFirst: 첫 이동 규칙을 사용할지 여부 (moveable_tiles_first 사용)
    // unitEquippedItem: pass the ItemSO equipped on the specific Unit instance (can be null).
    public bool[,] GetMoveMask(int originX, int originY, bool unitIsWhite, bool useFirst = false, ItemSO unitEquippedItem = null)
    {
        bool[,] mask = new bool[17, 17];

        var gsm = GameStreamManager.Instance;
        if (gsm == null)
            return mask; // 빈 마스크 반환

        // moveable list 선택: 첫 이동 목록을 우선 사용할 수 있음
        List<Vector2> tilesToUse = moveable_tiles;
        if (useFirst && moveable_tiles_first != null && moveable_tiles_first.Count > 0)
            tilesToUse = moveable_tiles_first;

        if (tilesToUse == null || tilesToUse.Count == 0)
            return mask;

        // 그룹화: 방향(normalized) -> list of step distances
        var dirMap = new Dictionary<Vector2Int, List<int>>();

        foreach (var v in tilesToUse)
        {
            int dx = Mathf.RoundToInt(v.x);
            int dy = Mathf.RoundToInt(v.y);
            if (dx == 0 && dy == 0) continue;

            int g = GCD(dx, dy);
            if (g == 0) g = 1; // 안전장치
            int nx = dx / g;
            int ny = dy / g;
            var dir = new Vector2Int(nx, ny);
            int steps = g; // g는 dir을 따라 몇 칸 떨어져있는지 (예: (2,0) -> g=2)

            if (!dirMap.TryGetValue(dir, out var list))
            {
                list = new List<int>();
                dirMap[dir] = list;
            }
            if (!list.Contains(steps)) list.Add(steps);
        }

        // 중심 인덱스
        const int C = 8;

        // determine whether this unit should be treated as passing-over based on equipped item
        // Use the per-instance equipped item (unitEquippedItem) passed from the Unit instance.
        bool localCanPassOver = this.can_pass_over || (unitEquippedItem != null && unitEquippedItem.item_type == ItemSO.ItemType.Jump);

        foreach (var kv in dirMap)
        {
            var dir = kv.Key;
            var stepsList = kv.Value;
            stepsList.Sort();

            foreach (var s in stepsList)
            {
                int tx = originX + dir.x * s;
                int ty = originY + dir.y * s;

                // 보드 범위 검사
                if (tx < 0 || ty < 0 || tx > 7 || ty > 7)
                    continue;

                // 마스크 범위(17x17) 인덱스
                int ix = C + dir.x * s;
                int iy = C + dir.y * s;
                if (ix < 0 || iy < 0 || ix > 16 || iy > 16)
                    continue;

                int occ = gsm.CheckUnit(tx, ty); // 0 empty, 1 white, -1 black

                if (occ == 0)
                {
                    mask[ix, iy] = true;
                    // 빈칸이면 계속 진행(다음 스텝이 있으면)
                    continue;
                }
                else
                {
                    bool occIsWhite = (occ == 1);
                    // 아군인지 적군인지 판단 (호출자 Unit의 is_white 값을 사용)
                    if (occIsWhite == unitIsWhite)
                    {
                        // 아군이 있음: 착지 불가
                        if (localCanPassOver)
                        {
                            // 착지는 불가(자기 칸 체크 안함), 계속 탐색
                            continue;
                        }
                        else
                        {
                            // 완전 차단
                            break;
                        }
                    }
                    else
                    {
                        // 적군: 착지는 가능(포착)
                        mask[ix, iy] = true;
                        if (localCanPassOver)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        // Pawn 특수 처리:
        // - 전방(앞으로 한 칸 등)에서 적을 잡을 수 없게 한다(전방에 적이 있어도 잡지 못함).
        //   또한 전방에 어떤 기물이 있으면 그 방향의 뒤쪽 칸은 모두 차단되어야 한다.
        // - 대신 catchable_tiles에 정의된 오프셋에 적이 있으면 그 위치로 잡을 수 있도록 허용한다.
        if (base_type == UnitType.Pawn)
        {
            // forward direction based on unitIsWhite
            Vector2Int forward = unitIsWhite ? new Vector2Int(0, 1) : new Vector2Int(0, -1);

            if (dirMap.TryGetValue(forward, out var fSteps))
            {
                fSteps.Sort();
                // find first occupied step in forward direction
                int firstOccStep = -1;
                foreach (var s in fSteps)
                {
                    int tx = originX + forward.x * s;
                    int ty = originY + forward.y * s;
                    if (tx < 0 || ty < 0 || tx > 7 || ty > 7)
                        break;
                    int occ = gsm.CheckUnit(tx, ty);
                    if (occ != 0)
                    {
                        firstOccStep = s;
                        break;
                    }
                }

                if (firstOccStep != -1)
                {
                    // remove any mask entries at and beyond the occupied step in this direction
                    foreach (var s in fSteps)
                    {
                        if (s >= firstOccStep)
                        {
                            int ix = C + forward.x * s;
                            int iy = C + forward.y * s;
                            if (ix >= 0 && iy >= 0 && ix <= 16 && iy <= 16)
                                mask[ix, iy] = false;
                        }
                    }
                }
                else
                {
                    // no occupied forward steps — nothing to change
                }
            }

            // catchable_tiles 처리: 정의된 오프셋에 적이 있으면 그 칸만 착지 허용
            if (catchable_tiles != null)
            {
                foreach (var cv in catchable_tiles)
                {
                    int dx = Mathf.RoundToInt(cv.x);
                    int dy = Mathf.RoundToInt(cv.y);
                    int tx = originX + dx;
                    int ty = originY + dy;
                    if (tx < 0 || ty < 0 || tx > 7 || ty > 7) continue;
                    int occ = gsm.CheckUnit(tx, ty);
                    if (occ != 0)
                    {
                        bool occIsWhite = (occ == 1);
                        if (occIsWhite != unitIsWhite)
                        {
                            int ix = C + dx;
                            int iy = C + dy;
                            if (ix >= 0 && iy >= 0 && ix <= 16 && iy <= 16)
                                mask[ix, iy] = true;
                        }
                    }
                }
            }
        }

        // Item: MoveBoost 추가 처리
        if (unitEquippedItem != null && unitEquippedItem.item_type == ItemSO.ItemType.MoveBoost && unitEquippedItem.moveBoost_tiles != null)
        {
            foreach (var bv in unitEquippedItem.moveBoost_tiles)
            {
                int dx = Mathf.RoundToInt(bv.x);
                int dy = Mathf.RoundToInt(bv.y);
                int tx = originX + dx;
                int ty = originY + dy;
                if (tx < 0 || ty < 0 || tx > 7 || ty > 7) continue;
                int ix = C + dx;
                int iy = C + dy;
                if (ix < 0 || iy < 0 || ix > 16 || iy > 16) continue;
                int occ = gsm.CheckUnit(tx, ty);
                if (occ == 0)
                {
                    mask[ix, iy] = true;
                }
                else
                {
                    bool occIsWhite = (occ == 1);
                    if (occIsWhite != unitIsWhite)
                        mask[ix, iy] = true;
                }
            }
        }

        return mask;
    }

    // 유클리드 GCD
    int GCD(int a, int b)
    {
        a = Mathf.Abs(a);
        b = Mathf.Abs(b);
        if (a == 0) return b;
        if (b == 0) return a;
        while (b != 0)
        {
            int t = b;
            b = a % b;
            a = t;
        }
        return a;
    }
}
