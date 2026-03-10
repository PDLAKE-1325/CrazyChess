using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class RainbowGlowRotateText : MonoBehaviour
{
    [Header("Rainbow Color")]
    public float hueSpeed = 0.25f;           // 무지개 색 전환 속도
    [Range(0f, 1f)] public float saturation = 1f;
    [Range(0f, 1f)] public float value = 1f;
    public bool useUnscaledTime = true;

    [Header("Glow (Outline/Shadow)")]
    public bool autoAddEffects = true;
    public Vector2 outlineDistance = new Vector2(2f, -2f);
    public float glowPulseSpeed = 3f;
    [Range(0f, 1f)] public float glowMinAlpha = 0.2f;
    [Range(0f, 1f)] public float glowMaxAlpha = 0.9f;

    [Header("Rotation")]
    public float rotateInterval = 2f;  // n초마다 회전
    public float rotateAmount = 15f;   // 회전할 각도 (매 인터벌마다 추가)
    public bool smoothRotate = true;   // true: 부드럽게 회전 / false: 순간 회전

    private Text _text;
    private Outline _outline;
    private Shadow _shadow;
    private float _nextRotateTime;

    void Awake()
    {
        _text = GetComponent<Text>();

        if (autoAddEffects)
        {
            _outline = GetComponent<Outline>();
            if (_outline == null) _outline = gameObject.AddComponent<Outline>();

            _shadow = GetComponent<Shadow>();
            if (_shadow == null) _shadow = gameObject.AddComponent<Shadow>();

            _outline.effectDistance = outlineDistance;
            _shadow.effectDistance = -outlineDistance * 0.5f;
        }
        else
        {
            _outline = GetComponent<Outline>();
            _shadow = GetComponent<Shadow>();
        }

        _nextRotateTime = Time.time + rotateInterval;
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        // 🌈 무지개 텍스트
        float hue = (t * hueSpeed) % 1f;
        Color baseColor = Color.HSVToRGB(hue, saturation, value);
        _text.color = baseColor;

        // ✨ 글로우 숨쉬기
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * glowPulseSpeed);
        float outlineAlpha = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, pulse);

        if (_outline != null)
        {
            Color oc = baseColor; oc.a = outlineAlpha;
            _outline.effectColor = oc;
        }

        if (_shadow != null)
        {
            float shadowAlpha = Mathf.Lerp(glowMinAlpha * 0.5f, glowMaxAlpha * 0.5f,
                                           0.5f + 0.5f * Mathf.Sin(t * glowPulseSpeed + 1.2f));
            Color sc = baseColor; sc.a = shadowAlpha;
            _shadow.effectColor = sc;
        }

        // 🔄 n초마다 회전
        if (Time.time >= _nextRotateTime)
        {
            _nextRotateTime = Time.time + rotateInterval;

            if (smoothRotate)
                StartCoroutine(SmoothRotation());
            else
                transform.Rotate(Vector3.forward, rotateAmount);
        }
    }

    // 부드러운 회전
    System.Collections.IEnumerator SmoothRotation()
    {
        float duration = 0.4f;
        float elapsed = 0f;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, rotateAmount);

        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Lerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRot;
    }
}
