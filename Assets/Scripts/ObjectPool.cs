using System.Collections.Generic;
using UnityEngine;

// Lightweight prefab pool to avoid Instantiate/Destroy churn (and the GC
// hitches it causes on WebGL) for the steady stream of obstacles and coins.
//
// Backward-compatible: if no ObjectPool exists in the scene, Get() falls back
// to Instantiate and Release() falls back to Destroy, so callers are safe to
// use the static helpers unconditionally.
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> pools =
        new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Get a pooled instance of prefab (or Instantiate one if the pool is empty
    // / no ObjectPool is present).
    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!prefab) return null;
        if (Instance) return Instance.Spawn(prefab, position, rotation);
        return Instantiate(prefab, position, rotation);
    }

    // Return an instance to its pool (or Destroy it if it wasn't pooled / no
    // ObjectPool is present).
    public static void Release(GameObject instance)
    {
        if (!instance) return;
        if (Instance) Instance.Despawn(instance);
        else Destroy(instance);
    }

    GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject instance = null;
        if (pools.TryGetValue(prefab, out Queue<GameObject> queue) && queue.Count > 0)
        {
            instance = queue.Dequeue();
            while (!instance && queue.Count > 0) instance = queue.Dequeue();
        }

        if (!instance)
        {
            instance = Instantiate(prefab, position, rotation);
            PooledObject marker = instance.GetComponent<PooledObject>();
            if (!marker) marker = instance.AddComponent<PooledObject>();
            marker.sourcePrefab = prefab;
        }
        else
        {
            instance.transform.SetPositionAndRotation(position, rotation);
        }

        instance.SetActive(true);
        return instance;
    }

    void Despawn(GameObject instance)
    {
        PooledObject marker = instance.GetComponent<PooledObject>();
        if (!marker || !marker.sourcePrefab)
        {
            // Not produced by this pool (e.g. a runtime-created primitive) —
            // just destroy it.
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(transform, false);

        if (!pools.TryGetValue(marker.sourcePrefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools[marker.sourcePrefab] = queue;
        }
        queue.Enqueue(instance);
    }
}
