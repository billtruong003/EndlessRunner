using UnityEngine;
using System.Collections.Generic;

public class ObjectPooler : Singleton<ObjectPooler>
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Transform> poolContainers;

    protected override void Awake()
    {
        base.Awake();
        InitializePooler();
    }

    private void InitializePooler()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolContainers = new Dictionary<string, Transform>();

        foreach (Pool pool in pools)
        {
            GameObject poolContainer = new GameObject($"{pool.tag} Pool");
            poolContainer.transform.SetParent(this.transform);
            poolContainers.Add(pool.tag, poolContainer.transform);

            Queue<GameObject> objectQueue = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, poolContainer.transform);
                obj.SetActive(false);
                objectQueue.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectQueue);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist.");
            return null;
        }

        Queue<GameObject> poolQueue = poolDictionary[tag];

        if (poolQueue.Count == 0)
        {
            Debug.LogWarning($"Pool '{tag}' is empty. Expanding pool size.");
            ExpandPool(tag);
        }

        GameObject objectToSpawn = poolQueue.Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.SetParent(parent);
        objectToSpawn.transform.SetPositionAndRotation(position, rotation);

        IPooledObject pooledObject = objectToSpawn.GetComponent<IPooledObject>();
        pooledObject?.OnObjectSpawn();

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist.");
            Destroy(objectToReturn);
            return;
        }

        objectToReturn.SetActive(false);
        objectToReturn.transform.SetParent(poolContainers[tag]);
        poolDictionary[tag].Enqueue(objectToReturn);
    }

    private void ExpandPool(string tag)
    {
        Pool poolToExpand = pools.Find(p => p.tag == tag);
        if (poolToExpand != null)
        {
            GameObject obj = Instantiate(poolToExpand.prefab, poolContainers[tag]);
            obj.SetActive(false);
            poolDictionary[tag].Enqueue(obj);
        }
    }
}

public interface IPooledObject
{
    void OnObjectSpawn();
}