using UnityEngine;
using UnityEngine.UI;

public class UIScreensaverBounce : MonoBehaviour
{
    public RectTransform moveTarget;   // 움직일 UI (Image)
    public float speed = 300f;         // 이동 속도 (px/sec)
    private Vector2 direction;         // 현재 이동 방향 (단위 벡터)

    private RectTransform canvasRect;
    private Vector2 targetSize;

    void Start()
    {
        if (moveTarget == null)
            moveTarget = GetComponent<RectTransform>();

        canvasRect = moveTarget.GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        // 처음에 랜덤한 방향으로 시작
        direction = Random.insideUnitCircle.normalized;

        // UI 이미지 사이즈 캐싱 (회전 고려)
        targetSize = moveTarget.rect.size * moveTarget.lossyScale;
    }

    void Update()
    {
        Vector2 pos = moveTarget.anchoredPosition;
        pos += direction * speed * Time.deltaTime;

        // 바운더리 (캔버스 내부 크기)
        Vector2 canvasHalfSize = canvasRect.rect.size / 2f;

        // 좌/우 벽 충돌 체크
        if (pos.x + targetSize.x / 2f > canvasHalfSize.x || pos.x - targetSize.x / 2f < -canvasHalfSize.x)
        {
            direction.x = -direction.x; // 방향 반전
            pos.x = Mathf.Clamp(pos.x, -canvasHalfSize.x + targetSize.x / 2f, canvasHalfSize.x - targetSize.x / 2f);
        }

        // 위/아래 충돌 체크
        if (pos.y + targetSize.y / 2f > canvasHalfSize.y || pos.y - targetSize.y / 2f < -canvasHalfSize.y)
        {
            direction.y = -direction.y; // 방향 반전
            pos.y = Mathf.Clamp(pos.y, -canvasHalfSize.y + targetSize.y / 2f, canvasHalfSize.y - targetSize.y / 2f);
        }

        moveTarget.anchoredPosition = pos;
    }
}
