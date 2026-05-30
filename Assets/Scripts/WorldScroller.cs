using UnityEngine;

// Scrolls a whole environment root toward the player at the game's moveSpeed,
// so a curated (non-tiled) diorama reads as a moving world. Optional wrap loops
// it back; leave wrapLength = 0 for a single pass.
public class WorldScroller : MonoBehaviour
{
    [Tooltip("Distance to travel before snapping back to start. 0 = no wrap (single pass).")]
    public float wrapLength = 0f;

    private float startZ;

    void Start()
    {
        startZ = transform.position.z;
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        float speed = GameManager.Instance ? GameManager.Instance.moveSpeed : 12f;
        Vector3 p = transform.position;
        p.z -= speed * Time.deltaTime;
        if (wrapLength > 0f && (startZ - p.z) >= wrapLength)
        {
            p.z = startZ;
        }
        transform.position = p;
    }
}
