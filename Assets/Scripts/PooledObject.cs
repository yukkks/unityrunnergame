using UnityEngine;

// Marker added to every instance handed out by ObjectPool so it can be
// returned to the correct prefab pool when released.
public class PooledObject : MonoBehaviour
{
    [HideInInspector] public GameObject sourcePrefab;
}
