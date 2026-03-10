using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class GameStreamManager : MonoBehaviour
{
    public static GameStreamManager Instance { get; private set; }
    public bool input { get; private set; } = false;
    public Unit cur_selected_unit { get; private set; } = null;
    public BoardData board_data { get; private set; }
    public bool clickable { get; private set; } = false;
    public bool turn_white { get; private set; } = true;
    public InputData input_data { get; private set; } = new InputData();
    public Unit marked_unit { get; private set; } = null;

    [Header("Promotion")]
    public GameObject[] unitPrefabsWhite = new GameObject[6];
    public GameObject[] unitPrefabsBlack = new GameObject[6];
    public GameObject promotionUI;
    public bool promotionActive { get; private set; } = false;
    // Pawn unit awaiting promotion
    public Unit pendingPromotionPawn { get; private set; } = null;
    // Flag set when promotion completed (used by ActionState to advance turn)
    public bool promotionCompleted { get; private set; } = false;

    // Item system
    [Header("Items")]
    // All possible items (author assigns ItemSO assets)
    public ItemSO[] allItems;
    // Per-team inventories
    public System.Collections.Generic.List<ItemSO> whiteItems = new System.Collections.Generic.List<ItemSO>();
    public System.Collections.Generic.List<ItemSO> blackItems = new System.Collections.Generic.List<ItemSO>();
    public int maxItemsPerTeam = 6;

    [Header("Combine rules")]
    // List of combine rules (created as CombineSO assets) to evaluate. Each rule's from offsets are interpreted
    // as absolute board coordinates (x,y). The system will evaluate these rules and perform combines.
    public System.Collections.Generic.List<CombineSO> combineRules = new System.Collections.Generic.List<CombineSO>();
    // Optional UI hook for combine button (set active when any combine is available)
    [Header("Combine UI")]
    public GameObject combineButton;

    // Track whether each team has already completed their very first turn.
    // We won't award an item on the first EndTurn for each team.
    public bool firstTurnCompletedWhite = false;
    public bool firstTurnCompletedBlack = false;
    public bool in_check = false;

    // Optional UI hooks: parent to populate, prefab for item UI entries, description UI and optional text fields
    public Transform itemUIParent;
    public GameObject itemUIPrefab;
    public GameObject itemDescriptionUI;
    public UnityEngine.UI.Text itemDescNameText;
    public UnityEngine.UI.Text itemDescBodyText;

    // Item selection state
    public bool itemSelectingActive { get; private set; } = false;
    public ItemSO pendingSelectedItem { get; private set; } = null;
    // last item that was used (for ApplyEffectState debug)
    public ItemSO lastUsedItem { get; private set; } = null;
    // Unit selection for item use
    public GameObject selectUnitPanel;
    public bool awaitingUnitSelection { get; private set; } = false;

    public void SetInput(bool parm) => input = parm;
    public void SetSelectedUnit(Unit obj = null) => cur_selected_unit = obj;
    public void SetBoard(BoardData data) => board_data = data;
    public void SetTileUnit(Unit unit_script, int x, int y)
    {
        board_data.board[x][y].unit = unit_script;
    }

    // Try to perform a combine (fusion) at a given base tile (baseX,baseY) using the rules in CombineSO.
    // Returns true if combine succeeded and the new unit was spawned.
    public bool TryCombineAt(int baseX, int baseY, CombineSO combine)
    {
        // New behavior: CombineSO uses lists of absolute from-offsets (board x,y coordinates).
        if (combine == null) return false;
        if (board_data == null) return false;
        // Only allow combines that belong to the current player's side
        if (combine.is_white_unit != turn_white) return false;

        if (combine.combine_from_offsets == null || combine.combine_from_types == null) return false;
        if (combine.combine_from_offsets.Count != combine.combine_from_types.Count) return false;

        // check all source positions and types
        for (int i = 0; i < combine.combine_from_offsets.Count; i++)
        {
            int sx = Mathf.RoundToInt(combine.combine_from_offsets[i].x);
            int sy = Mathf.RoundToInt(combine.combine_from_offsets[i].y);
            if (!InBounds(sx, sy)) return false;
            var tile = board_data.board[sx][sy];
            if (tile == null || tile.unit == null) return false;
            if (tile.unit.thisUnit == null) return false;
            if (tile.unit.thisUnit.base_type != combine.combine_from_types[i]) return false;
            if (tile.unit.is_white_unit != combine.is_white_unit) return false;
        }

        // target
        int tx = Mathf.RoundToInt(combine.combine_to_offset.x);
        int ty = Mathf.RoundToInt(combine.combine_to_offset.y);
        if (!InBounds(tx, ty)) return false;

        // capture team of first source unit (if any) then remove all source units
        bool teamWhite = true;
        {
            int sx0 = Mathf.RoundToInt(combine.combine_from_offsets[0].x);
            int sy0 = Mathf.RoundToInt(combine.combine_from_offsets[0].y);
            var t0 = board_data.board[sx0][sy0];
            if (t0 != null && t0.unit != null) teamWhite = t0.unit.is_white_unit;
        }
        for (int i = 0; i < combine.combine_from_offsets.Count; i++)
        {
            int sx = Mathf.RoundToInt(combine.combine_from_offsets[i].x);
            int sy = Mathf.RoundToInt(combine.combine_from_offsets[i].y);
            var tile = board_data.board[sx][sy];
            var u = tile.unit;
            tile.unit = null;
            try { if (u != null) Destroy(u.gameObject); } catch { }
        }

        // spawn result
        if (combine.combine_to_object_prefab == null) return false;
        Vector3 spawnPos = UT_UnitMovements.GetPos(tx, ty);
        GameObject go = Instantiate(combine.combine_to_object_prefab, spawnPos, Quaternion.identity);
        Unit newUnit = go.GetComponent<Unit>();
        if (newUnit != null)
        {
            newUnit.x = tx; newUnit.y = ty;
            newUnit.is_white_unit = teamWhite;
            SetTileUnit(newUnit, tx, ty);
        }
        else
        {
            Debug.LogWarning("Combine spawned object has no Unit component.");
        }

        Debug.Log($"Combine succeeded -> spawned {combine.combine_to_object_prefab.name} at ({tx},{ty})");
        return true;
    }

    // --- New API: Treat CombineSO positions as absolute board coordinates (x,y)
    // Attempt to apply the given combine rule where the combine_from_offset1/2 are absolute tile coordinates.
    public bool TryCombineBySO(CombineSO combine)
    {
        // Delegate to TryCombineAt which now expects combine.combine_from_offsets as absolute positions
        return TryCombineAt(0, 0, combine);
    }

    // Execute all combine rules (from combineRules list) repeatedly until no more applies or maxIterations reached.
    // Returns number of combines applied.
    public int ExecuteCombineRulesIterative(int maxIterations = 100)
    {
        if (combineRules == null || combineRules.Count == 0) return 0;
        int applied = 0;
        for (int iter = 0; iter < maxIterations; iter++)
        {
            bool any = false;
            // iterate over rules; if one applies, break to restart from first rule to allow chaining
            foreach (var rule in combineRules)
            {
                if (rule == null) continue;
                if (TryCombineBySO(rule))
                {
                    applied++; any = true; break;
                }
            }
            if (!any) break;
        }
        if (applied > 0) Debug.Log($"ExecuteCombineRulesIterative applied {applied} combines.");
        return applied;
    }

    // Return list of combine rules that are currently applicable based on board state.
    public System.Collections.Generic.List<CombineSO> GetApplicableCombineRules()
    {
        var res = new System.Collections.Generic.List<CombineSO>();
        if (combineRules == null || combineRules.Count == 0) return res;
        if (board_data == null) return res;

        foreach (var rule in combineRules)
        {
            if (rule == null) continue;
            // only consider rules that belong to the current player's side
            if (rule.is_white_unit != turn_white) continue;
            // rule combines from multiple absolute positions: combine_from_offsets and combine_from_types
            if (rule.combine_from_offsets == null || rule.combine_from_types == null) continue;
            if (rule.combine_from_offsets.Count != rule.combine_from_types.Count) continue;

            bool ok = true;
            for (int i = 0; i < rule.combine_from_offsets.Count; i++)
            {
                int sx = Mathf.RoundToInt(rule.combine_from_offsets[i].x);
                int sy = Mathf.RoundToInt(rule.combine_from_offsets[i].y);
                if (!InBounds(sx, sy)) { ok = false; break; }
                var tile = board_data.board[sx][sy];
                if (tile == null || tile.unit == null) { ok = false; break; }
                var ut = tile.unit.thisUnit;
                if (ut == null || ut.base_type != rule.combine_from_types[i]) { ok = false; break; }
                // also ensure the unit belongs to the same side as the rule
                if (tile.unit.is_white_unit != rule.is_white_unit) { ok = false; break; }
            }
            if (ok) res.Add(rule);
        }
        return res;
    }

    // Enable/disable combine button based on currently applicable rules
    public void EvaluateCombineAvailability()
    {
        if (combineButton == null) return;
        var list = GetApplicableCombineRules();
        combineButton.SetActive(list != null && list.Count > 0 && !in_check);
    }

    // Called by UI button: pick the applicable combine with highest combine_property_id and apply it.
    public void OnCombineButtonPressed()
    {
        var applicable = GetApplicableCombineRules();
        if (applicable == null || applicable.Count == 0) return;
        // pick highest property id
        CombineSO chosen = null;
        int best = int.MinValue;
        foreach (var r in applicable)
        {
            if (r == null) continue;
            if (r.combine_property_id > best) { best = r.combine_property_id; chosen = r; }
        }
        if (chosen == null) return;

        // Apply chosen combine
        bool ok = TryCombineBySO(chosen);
        if (!ok)
        {
            Debug.LogWarning("Combine attempt failed despite being reported applicable.");
            EvaluateCombineAvailability();
            return;
        }

        // Clear markers/selection
        ClearMarkedUnit();
        ClearMarkerAction?.Invoke();
        SetSelectedUnit(null);

        // Consume turn: toggle turn and advance to EndTurnState
        ToggleTurn();
        var tm = UnityEngine.Object.FindObjectOfType<TurnManager>();
        if (tm != null) tm.ChangeState(tm.endTurnState);
        EvaluateCombineAvailability();
    }

    bool InBounds(int x, int y)
    {
        if (board_data == null) return false;
        if (x < 0 || y < 0) return false;
        if (x >= board_data.board.Count) return false;
        if (y >= board_data.board[x].Count) return false;
        return true;
    }
    public void SetClickable(bool parm) => clickable = parm;
    public void SetInputData(InputData data) => input_data = data;

    public void SetMarkedUnit(Unit u) => marked_unit = u;
    public void ClearMarkedUnit() => marked_unit = null;

    public void ToggleTurn() => turn_white = !turn_white;

    public Action MakeMarkerAction;
    public Action ClearMarkerAction;

    public int CheckUnit(int x, int y)
    {
        int res = 0;
        if (board_data.board[x][y].unit != null)
        {
            if (board_data.board[x][y].unit.is_white_unit) res = 1;
            else
                res = -1;
        }
        return res;
    }

    // Start promotion flow for the given pawn unit (shows UI and waits)
    public void StartPromotion(Unit pawn)
    {
        if (pawn == null) return;
        pendingPromotionPawn = pawn;
        promotionActive = true;
        promotionCompleted = false;
        if (promotionUI != null) promotionUI.SetActive(true);
        Debug.Log($"Promotion started for {pawn.name} at ({pawn.x},{pawn.y})");
    }

    // Called by UI (EventTrigger) when player selects a promotion piece
    public void Promotion(UnitType type)
    {
        if (!promotionActive || pendingPromotionPawn == null)
        {
            Debug.LogWarning("No promotion pending.");
            return;
        }

        int x = pendingPromotionPawn.x;
        int y = pendingPromotionPawn.y;
        bool isWhite = pendingPromotionPawn.is_white_unit;

        // Remove old pawn object from scene and board
        try
        {
            if (board_data.board[x][y].unit == pendingPromotionPawn)
                board_data.board[x][y].unit = null;
        }
        catch (System.Exception) { }
        GameObject.Destroy(pendingPromotionPawn.gameObject);

        // Instantiate new unit prefab
        GameObject prefab = null;
        int idx = (int)type;
        if (isWhite)
        {
            if (idx >= 0 && idx < unitPrefabsWhite.Length) prefab = unitPrefabsWhite[idx];
        }
        else
        {
            if (idx >= 0 && idx < unitPrefabsBlack.Length) prefab = unitPrefabsBlack[idx];
        }
        if (prefab == null)
        {
            Debug.LogError($"No prefab assigned for promotion type {type}");
            // still hide UI and clear pending to avoid blocking
            if (promotionUI != null) promotionUI.SetActive(false);
            pendingPromotionPawn = null;
            promotionActive = false;
            promotionCompleted = true;
            return;
        }

        Vector3 spawnPos = UT_UnitMovements.GetPos(x, y);
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        Unit newUnit = go.GetComponent<Unit>();
        if (newUnit == null)
        {
            Debug.LogError("Promoted prefab does not contain a Unit component.");
            GameObject.Destroy(go);
            if (promotionUI != null) promotionUI.SetActive(false);
            pendingPromotionPawn = null;
            promotionActive = false;
            promotionCompleted = true;
            return;
        }

        newUnit.x = x;
        newUnit.y = y;
        newUnit.is_white_unit = isWhite;
        newUnit.hasMoved = true; // promoted piece has effectively moved
        SetTileUnit(newUnit, x, y);

        // hide UI and clear pending state
        if (promotionUI != null) promotionUI.SetActive(false);
        pendingPromotionPawn = null;
        promotionActive = false;
        promotionCompleted = true;

        Debug.Log($"Promotion complete: spawned {type} at ({x},{y})");
    }

    // Unity UI-friendly wrappers so Buttons/EventTriggers can call promotion directly from inspector.
    // Inspector's UnityEvent doesn't always show methods that take custom enum types, so expose int and no-arg wrappers.
    public void PromotionByIndex(int idx)
    {
        Promotion((UnitType)idx);
    }

    public void PromotionToQueen() { Promotion(UnitType.Queen); }
    public void PromotionToRook() { Promotion(UnitType.Rook); }
    public void PromotionToBishop() { Promotion(UnitType.Bishop); }
    public void PromotionToKnight() { Promotion(UnitType.Knight); }

    // Called by game flow to acknowledge we've processed the promotion completion
    public void ClearPromotionCompleted()
    {
        promotionCompleted = false;
    }

    // --- Item system methods ---
    public void AddRandomItemToCurrentPlayer()
    {
        AddRandomItemToPlayer(turn_white);
    }

    public void AddRandomItemToPlayer(bool white)
    {
        if (allItems == null || allItems.Length == 0) return;
        var list = white ? whiteItems : blackItems;
        if (list.Count >= maxItemsPerTeam)
        {
            Debug.Log("Player item inventory full, no item awarded.");
            return;
        }
        int idx = UnityEngine.Random.Range(0, allItems.Length);
        var item = allItems[idx];
        list.Add(item);
        Debug.Log($"Awarded item '{item.itemName}' to {(white ? "White" : "Black")} team.");
    }

    public System.Collections.Generic.List<ItemSO> GetItemsForPlayer(bool white)
    {
        return white ? whiteItems : blackItems;
    }

    // Populate UI parent with item entries using itemUIPrefab (expects ItemObj on prefab)
    public void PopulateItemsUI(Transform parent, GameObject prefab)
    {
        if (parent == null || prefab == null) return;
        // clear existing
        for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        var items = GetItemsForPlayer(turn_white);
        foreach (var it in items)
        {
            var go = UnityEngine.Object.Instantiate(prefab, parent);
            var obj = go.GetComponent<ItemObj>();
            if (obj != null) obj.item = it;
        }
    }

    // Show item description UI and pause interactions
    public void ShowItemDescription(ItemSO item)
    {
        if (item == null) return;
        pendingSelectedItem = item;
        itemSelectingActive = true;
        SetClickable(false);
        if (itemDescriptionUI != null) itemDescriptionUI.SetActive(true);
        if (itemDescNameText != null) itemDescNameText.text = item.itemName;
        if (itemDescBodyText != null) itemDescBodyText.text = item.description;
    }

    public void CancelItemSelection()
    {
        pendingSelectedItem = null;
        itemSelectingActive = false;
        if (itemDescriptionUI != null) itemDescriptionUI.SetActive(false);
        SetClickable(true);
    }

    // Called when player confirms 'Use' in the item description UI but before unit selection.
    // The game will expect the player to select an appropriate unit next.
    public void BeginItemUse()
    {
        // keep pendingSelectedItem set; allow unit selection but block other interactions if needed.
        awaitingUnitSelection = true;
        itemSelectingActive = true;
        // show select-unit UI panel if assigned
        if (selectUnitPanel != null) selectUnitPanel.SetActive(true);
        // allow clicking (CameraRay will respect awaitingUnitSelection to avoid marker creation)
        SetClickable(true);
        if (itemDescriptionUI != null) itemDescriptionUI.SetActive(false);
    }

    // Called by Select Unit panel 'Use' button to confirm using item on currently selected unit
    public void ConfirmUseOnSelectedUnit()
    {
        var u = cur_selected_unit;
        if (u == null)
        {
            Debug.LogWarning("No unit selected to apply item to.");
            return;
        }
        UseItemOnUnit(u);
        // hide panel
        if (selectUnitPanel != null) selectUnitPanel.SetActive(false);
        awaitingUnitSelection = false;
    }

    // Called by Select Unit panel 'Cancel' button
    public void CancelUnitSelectionPanel()
    {
        // cancel item use and close panel
        CancelItemSelection();
        if (selectUnitPanel != null) selectUnitPanel.SetActive(false);
        awaitingUnitSelection = false;
    }

    // Apply the pending item to the chosen unit, remove the item from inventory, and advance to Effect state.
    public void UseItemOnUnit(Unit unit)
    {
        if (pendingSelectedItem == null || unit == null) return;
        if (unit.thisUnit == null)
        {
            Debug.LogWarning("Target unit has no UnitSO to equip.");
            return;
        }
        if (unit.thisUnit.base_type != pendingSelectedItem.fit_unit_type)
        {
            Debug.LogWarning("Item does not fit this unit type.");
            return;
        }

        // Equip on the specific Unit instance (not the shared UnitSO)
        unit.equipped_item = pendingSelectedItem;

        // remove from player's list
        var list = unit.is_white_unit ? whiteItems : blackItems;
        if (!list.Remove(pendingSelectedItem))
        {
            // try removing from other list just in case
            whiteItems.Remove(pendingSelectedItem);
            blackItems.Remove(pendingSelectedItem);
        }

        lastUsedItem = pendingSelectedItem;
        pendingSelectedItem = null;
        itemSelectingActive = false;
        if (itemDescriptionUI != null) itemDescriptionUI.SetActive(false);

        // set Effect input and advance state machine
        SetInputData(new InputData(InputType.Effect, unit, Vector2.zero));
        SetInput(true);

        // Immediately switch the TurnManager to ActionState so the effect is processed without waiting a frame.
        var tm = UnityEngine.Object.FindObjectOfType<TurnManager>();
        if (tm != null)
        {
            tm.ChangeState(tm.actionState);
        }

        // ensure select panel closed and flags reset
        if (selectUnitPanel != null) selectUnitPanel.SetActive(false);
        awaitingUnitSelection = false;
    }

    [SerializeField] GameObject check_pannel;
    [SerializeField] Transform check_pannel_parent;

    public void CheckNotify()
    {
        Instantiate(check_pannel, check_pannel_parent);
    }

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        StartCoroutine(StartClickDelay());
    }
    IEnumerator StartClickDelay()
    {
        yield return new WaitForSeconds(1f);
        SetClickable(true);
    }
    [SerializeField] BoardEndBoom board_boom;
    public void TriggerEndBoom(int parm = 0) => board_boom.TriggerBoom(parm);
}

// using UnityEngine;

// public class CallbackManager : MonoBehaviour
// {
//     public void RunAllCallbacks()
//     {
//         // 씬 내부의 모든 MonoBehaviour 탐색
//         MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>(true);

//         foreach (var obj in all)
//         {
//             if (obj is ICallback callback)
//             {
//                 callback.ThisC();
//             }
//         }
//     }
// }
