using UnityEngine;

// Detects the dog hitting this obstacle. Mirrors CoinPickup: the trigger logic
// lives on the obstacle and identifies the player via GetComponent<PlayerController>()
// rather than relying on a tag match on the player side.
[RequireComponent(typeof(Collider))]
public class ObstacleHit : MonoBehaviour
{
    private bool hit;

    void OnEnable()
    {
        // Reset per-life state so a pooled obstacle behaves like a fresh one.
        hit = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hit) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (!player) return;

        hit = true;
        player.HitByObstacle();

        ObjectPool.Release(gameObject);
    }
}
