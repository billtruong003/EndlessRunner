using NaughtyAttributes;
using UnityEngine;

public class CoinLine : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private float spaceSize;
    [SerializeField] private int coinQuantity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    [Button]
    private void InitCoin()
    {
        float halfTotal = (spaceSize * coinQuantity) * 0.5f;
        for (int i = 0; i < coinQuantity; i++)
        {
            GameObject coinSpawn = Instantiate(prefab, this.transform);
            Transform coinTransform = coinSpawn.transform;
            float zSpawnPose = -halfTotal + (i * spaceSize);
            coinTransform.localPosition = new Vector3(coinTransform.position.x, coinTransform.position.y, zSpawnPose);
        }

    }
}
