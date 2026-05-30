using UnityEngine;

// Endless scrolling for a set of identical environment segments. All segments
// move toward the player at moveSpeed; when a segment's far edge passes behind
// the camera it leapfrogs to the front, so the world never runs out.
public class LoopingWorld : MonoBehaviour
{
    public Transform[] segments;
    [Tooltip("Z length of one segment (segments are spaced this far apart).")]
    public float segmentLength = 100f;
    [Tooltip("Distance from a segment's pivot to its far (high-Z) content edge.")]
    public float farEdgeOffset = 0f;
    [Tooltip("When a segment's far edge drops below this Z, it leapfrogs forward.")]
    public float recycleZ = -25f;

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;
        if (segments == null || segments.Length == 0) return;

        float speed = GameManager.Instance ? GameManager.Instance.moveSpeed : 12f;
        float dz = speed * Time.deltaTime;
        float total = segmentLength * segments.Length;

        foreach (var s in segments)
        {
            if (!s) continue;
            Vector3 p = s.position;
            p.z -= dz;
            if (p.z + farEdgeOffset < recycleZ) p.z += total;
            s.position = p;
        }
    }
}
