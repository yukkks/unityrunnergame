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

        // Tint the splat with this obstacle's color (onion = pale, eggplant =
        // purple) so the effect reads as that veggie bursting.
        Color splat = new Color(0.55f, 0.75f, 0.35f);
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend && rend.sharedMaterial)
        {
            if (rend.sharedMaterial.HasProperty("_BaseColor")) splat = rend.sharedMaterial.GetColor("_BaseColor");
            else if (rend.sharedMaterial.HasProperty("_Color")) splat = rend.sharedMaterial.color;
        }
        player.HitByObstacle(splat);

        ObjectPool.Release(gameObject);
    }
}
