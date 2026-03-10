using UnityEngine;

[DisallowMultipleComponent]
public class CameraMovement : MonoBehaviour
{
    [Header("Camera reference")]
    [SerializeField] Transform camTransform;

    [Header("Turn positions")]
    [SerializeField] Vector3 whitePosition = new Vector3(0f, 16f, -11f);
    [SerializeField] Vector3 whiteEuler = new Vector3(60f, 0f, 0f);
    [SerializeField] Vector3 blackPosition = new Vector3(0f, 16f, 11f);
    [SerializeField] Vector3 blackEuler = new Vector3(60f, 180f, 0f);

    [Header("Lerp settings")]
    [SerializeField] float positionLerpSpeed = 5f;
    [SerializeField] float rotationLerpSpeed = 5f;

    [Header("Idle bobbing")]
    [SerializeField] float horizontal_speed = 1f;
    [SerializeField] float vertical_speed = 0.7f;
    [SerializeField] float move_height = 0.2f;
    [SerializeField] float move_width = 0.2f;

    void Awake()
    {
        if (camTransform == null && Camera.main != null)
            camTransform = Camera.main.transform;
    }

    void Start()
    {
        // snap to initial turn immediately
        ApplyImmediate(GameStreamManager.Instance != null ? GameStreamManager.Instance.turn_white : true);
    }

    void Update()
    {
        if (camTransform == null) return;
        var gsm = GameStreamManager.Instance;
        bool isWhite = gsm == null ? true : gsm.turn_white;

        Vector3 basePos = isWhite ? whitePosition : blackPosition;
        Quaternion targetRot = Quaternion.Euler(isWhite ? whiteEuler : blackEuler);

        // bobbing offset
        Vector3 bob = new Vector3(0f, Mathf.Sin(Time.time * vertical_speed) * move_height, Mathf.Sin(Time.time * horizontal_speed) * move_width);

        Vector3 targetPos = basePos + bob;

        float posT = 1 - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
        float rotT = 1 - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);

        camTransform.position = Vector3.Lerp(camTransform.position, targetPos, posT);
        camTransform.rotation = Quaternion.Slerp(camTransform.rotation, targetRot, rotT);
    }

    public void ApplyImmediate(bool whiteTurn)
    {
        if (camTransform == null) return;
        camTransform.position = whiteTurn ? whitePosition : blackPosition;
        camTransform.rotation = Quaternion.Euler(whiteTurn ? whiteEuler : blackEuler);
    }
}
