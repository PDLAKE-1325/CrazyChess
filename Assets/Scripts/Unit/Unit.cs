using UnityEngine;

public class Unit : MonoBehaviour, IHoverable
{
    public UnitSO thisUnit;
    // Per-instance equipped item. Stored on Unit (instance), not on UnitSO which is shared.
    public ItemSO equipped_item;
    public bool is_white_unit;

    [SerializeField] Renderer targetRenderer;           // 비어있으면 같은 오브젝트의 Renderer 사용
    [SerializeField] Material replacementMaterial;      // 에디터에서 할당할 머테리얼

    Material originalMaterial;

    [Range(0, 7)] public int x;
    [Range(0, 7)] public int y;
    // 이동 속도(유닛 거리 단위 / 초). 이전 Lerp 기반 로직을 MoveTowards로 바꿔 직관적으로 설정합니다.
    [SerializeField] float moveSpeed = 10f;
    public bool hasMoved = false;

    protected virtual void Move()
    {
        if (thisUnit == null) return;
        Vector3 yAxis = new Vector3(0, thisUnit.base_type != UnitType.Pawn ? 0.13f : 0, 0);
        Vector3 target = UT_UnitMovements.GetPos(x, y) + yAxis;
        // 프레임 독립적 이동
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    public void OnHoverEnter()
    {
        ToggleSecond(true);
    }

    public void OnHoverExit()
    {
        ToggleSecond(false);
    }

    public void ToggleSecond(bool useReplacement)
    {
        if (targetRenderer == null) return;
        var mats = targetRenderer.materials;
        if (mats.Length <= 1) return;

        if (useReplacement)
        {
            // 교체
            mats[1] = replacementMaterial != null ? replacementMaterial : mats[1];
        }
        else
        {
            // 원복
            if (originalMaterial != null) mats[1] = originalMaterial;
        }
        targetRenderer.materials = mats;
    }

    #region Unity Methods
    protected virtual void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null) return;

        var mats = targetRenderer.materials;
        if (mats.Length > 1) originalMaterial = mats[1];
    }
    protected virtual void Update()
    {
        Move();
    }
    protected virtual void Start()
    {
        Invoke("SetTileUnit", 0.5f);
    }
    void SetTileUnit()
    {
        GameStreamManager.Instance.SetTileUnit(this, x, y);
    }
    #endregion
}