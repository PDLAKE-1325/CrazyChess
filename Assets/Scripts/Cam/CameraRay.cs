using UnityEngine;

public class CameraRay : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] LayerMask layerMask = ~0;
    [SerializeField] LayerMask layerMask_marker = ~0;
    [SerializeField] float maxDistance = float.PositiveInfinity;

    GameObject lastHit;
    GameObject now;

    void Update()
    {
        ShotRay();
        MarkWay();
        MarkerRay();
    }

    void MarkerRay()
    {
        if (!GameStreamManager.Instance.clickable || !Input.GetMouseButtonUp(0)) return;
        if (cam == null) cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask_marker))
        {
            var hitObj = hit.collider.gameObject;
            bool tagMatchesTurn = (GameStreamManager.Instance.turn_white && hitObj.CompareTag("WhiteMarker")) ||
                                   (!GameStreamManager.Instance.turn_white && hitObj.CompareTag("BlackMarker"));

            var markedOwner = GameStreamManager.Instance.marked_unit;
            bool ownerIsCurrentTurn = markedOwner != null && markedOwner.is_white_unit == GameStreamManager.Instance.turn_white;

            // Accept marker click if the marker's tag matches current turn OR if the marked_unit owner belongs to current turn
            if (tagMatchesTurn || ownerIsCurrentTurn)
            {
                print("마커 클릭: " + hitObj.name);

                PathMarker pm = hitObj.GetComponent<PathMarker>();

                // 우선순위: GameStreamManager.marked_unit(마커를 만든 유닛) -> cur_selected_unit -> now_marker
                Unit movingUnit = GameStreamManager.Instance.marked_unit;
                if (movingUnit == null) movingUnit = GameStreamManager.Instance.cur_selected_unit;
                if (movingUnit == null && now_marker != null)
                {
                    movingUnit = now_marker.GetComponent<Unit>();
                }

                if (movingUnit == null)
                {
                    Debug.LogWarning("No moving unit found when marker clicked.");
                    return;
                }

                GameStreamManager.Instance.SetInputData(new InputData(InputType.Move, movingUnit,
                    new Vector2(pm.x, pm.y)));

                GameStreamManager.Instance.SetInput(true);
            }
            else
            {
                // Debug info to help diagnose why marker was rejected
                Debug.Log($"Wrong marker clicked. tagMatchesTurn={tagMatchesTurn} ownerIsCurrentTurn={ownerIsCurrentTurn} markerTag={hitObj.tag} turn_white={GameStreamManager.Instance.turn_white} marked_unit={(markedOwner != null ? markedOwner.name : "null")}");
            }
        }
    }

    void ShotRay()
    {
        if (!GameStreamManager.Instance.clickable) return;
        if (cam == null) cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // 우선 마커 레이어를 체크해서, 마커가 레이 위에 있으면 기본적으로 마커 우선 처리
        // 단, 해당 마커의 소유자가 현재 턴 플레이어가 아니라면 마커는 무시하고 유닛 선택을 우선시함
        if (Physics.Raycast(ray, out RaycastHit markerHit, maxDistance, layerMask_marker))
        {
            bool markerBelongsToCurrentTurn = (GameStreamManager.Instance.turn_white && markerHit.collider.gameObject.CompareTag("WhiteMarker")) ||
                                               (!GameStreamManager.Instance.turn_white && markerHit.collider.gameObject.CompareTag("BlackMarker"));

            var owner = GameStreamManager.Instance.marked_unit;
            bool ownerIsCurrentTurn = owner != null && owner.is_white_unit == GameStreamManager.Instance.turn_white;

            if (markerBelongsToCurrentTurn || ownerIsCurrentTurn)
            {
                // 마커가 현재 턴 플레이어의 것이면 유닛 하이라이트/선택을 중단
                if (lastHit != null)
                {
                    NotifyExit(lastHit);
                    lastHit = null;
                }
                GameStreamManager.Instance.SetSelectedUnit();
                return;
            }
            // 그렇지 않으면 마커는 무시하고 아래의 유닛 레이어 레이캐스트로 계속 진행
        }

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        {
            var gsm = GameStreamManager.Instance;
            var unitComp = hit.collider.gameObject.GetComponent<Unit>();
            GameStreamManager.Instance.SetSelectedUnit(unitComp);
            now = hit.collider.gameObject;
            // If we're awaiting unit selection for item use, only apply the pending item when the player actually clicks (mouse up)
            if (gsm != null && gsm.awaitingUnitSelection)
            {
                if (unitComp == null)
                {
                    Debug.LogWarning("Awaiting unit selection but clicked object has no Unit component.");
                }
                else
                {
                    // Only trigger on an explicit left-click release to avoid hover-triggered uses
                    if (Input.GetMouseButtonUp(0))
                    {
                        // Check fit type
                        if (gsm.pendingSelectedItem != null && unitComp.thisUnit != null && unitComp.thisUnit.base_type == gsm.pendingSelectedItem.fit_unit_type)
                        {
                            gsm.UseItemOnUnit(unitComp);
                            // we consumed the selection; clear hovering state so UI doesn't keep highlight
                            if (lastHit != null)
                            {
                                NotifyExit(lastHit);
                                lastHit = null;
                            }
                            return; // do not proceed with normal selection/marker logic
                        }
                        else
                        {
                            Debug.LogWarning("Selected unit does not match item fit type or no pending item.");
                        }
                    }
                    // If not clicked, skip applying and continue to allow hover/preview as usual
                }
            }
            if (now != lastHit)
            {
                if (lastHit != null)
                    NotifyExit(lastHit);

                NotifyEnter(now);
                lastHit = now;
            }
        }
        else
        {
            GameStreamManager.Instance.SetSelectedUnit();
            if (lastHit != null)
            {
                NotifyExit(lastHit);
                lastHit = null;
            }

            // If the raycast hits nothing, only clear markers when the user actually clicked (mouse up)
            if (Input.GetMouseButtonUp(0))
            {
                var gsm = GameStreamManager.Instance;
                if (gsm != null)
                {
                    // clear global marked unit reference
                    gsm.ClearMarkedUnit();
                    // invoke marker clear action if assigned
                    gsm.ClearMarkerAction?.Invoke();
                }
                now_marker = null;
                marked = false;
            }
        }
    }

    bool marked = false;
    GameObject now_marker = null;

    void MarkWay()
    {
        var gsm = GameStreamManager.Instance;
        if (gsm == null) return;
        if (!gsm.clickable || (now_marker == null && marked))
        {
            if (marked)
            {
                gsm.ClearMarkerAction();
                marked = false;
            }
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (now == null)
            {
                gsm.ClearMarkerAction();
                marked = false;
                return;
            }
            else
            {
                // do not make markers if awaiting unit selection
                if (gsm.awaitingUnitSelection) return;
                gsm.MakeMarkerAction();
                now_marker = now;
                marked = true;
            }
        }
    }

    void NotifyEnter(GameObject go)
    {
        if (go.TryGetComponent<IHoverable>(out var h)) h.OnHoverEnter();
        // 필요하면 메시지 방식도 사용: go.SendMessage("OnHoverEnter", SendMessageOptions.DontRequireReceiver);
    }

    void NotifyExit(GameObject go)
    {
        if (go.TryGetComponent<IHoverable>(out var h)) h.OnHoverExit();
    }
}