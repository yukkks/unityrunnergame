using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float speed = 12f;
    public float destroyZ = -10f;

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        float s = GameManager.Instance ? GameManager.Instance.moveSpeed : speed;
        transform.Translate(Vector3.back * s * Time.deltaTime, Space.World);

        if (transform.position.z < destroyZ)
        {
            Destroy(gameObject);
        }
    }
}
