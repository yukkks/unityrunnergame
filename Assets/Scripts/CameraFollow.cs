using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 4f, -6.5f);
    public float smooth = 8f;
    public float laneOffset = 1.2f;
    [Range(2, 5)]
    public int laneCount = 3;

    [Header("Tilt")]
    public float tiltAmount = 6f;
    public float tiltSmooth = 8f;

    [Header("Lane Feedback")]
    public float laneRollKick = 6f;
    public float laneRollReturn = 10f;
    public float fovKick = 3.5f;
    public float fovReturn = 6f;

    private float currentTilt;
    private float rollImpulse;
    private float fovOffset;
    private Camera cam;
    private float baseFov;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam) baseFov = cam.fieldOfView;
    }

    void LateUpdate()
    {
        if (!target) return;

        // follow
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);

        // tilt based on player x
        float laneMaxX = laneOffset * (Mathf.Max(2, laneCount) - 1) * 0.5f;
        float denom = Mathf.Max(0.01f, laneMaxX);
        float t = Mathf.Clamp(target.position.x / denom, -1f, 1f);
        float targetTilt = -t * tiltAmount + rollImpulse;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmooth * Time.deltaTime);

        Quaternion rot = Quaternion.LookRotation((target.position + Vector3.forward * 6f) - transform.position);
        transform.rotation = rot * Quaternion.Euler(0, 0, currentTilt);

        rollImpulse = Mathf.Lerp(rollImpulse, 0f, laneRollReturn * Time.deltaTime);

        if (cam)
        {
            fovOffset = Mathf.Lerp(fovOffset, 0f, fovReturn * Time.deltaTime);
            cam.fieldOfView = baseFov + fovOffset;
        }
    }

    public void OnLaneSwitch(int direction)
    {
        float dir = Mathf.Clamp(direction, -1, 1);
        rollImpulse = dir * laneRollKick;
        if (cam) fovOffset = fovKick;
    }
}
