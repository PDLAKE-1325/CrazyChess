using UnityEngine;

public class ImgBobble : MonoBehaviour
{
    public float amplitudeX = 5f;  // 좌우 흔들림 강도
    public float amplitudeY = 5f;  // 상하 흔들림 강도
    public float speedX = 3f;      // 좌우 속도
    public float speedY = 2f;      // 상하 속도

    RectTransform rect;
    Vector2 originPos;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        originPos = rect.anchoredPosition;
    }

    void Update()
    {
        float x = Mathf.Sin(Time.time * speedX) * amplitudeX;
        float y = Mathf.Cos(Time.time * speedY) * amplitudeY;

        rect.anchoredPosition = originPos + new Vector2(x, y);
    }
}
