// FileName: CollectibleBase.cs

using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class CollectibleBase : MonoBehaviour, IPooledObject
{
    [SerializeField] private string poolTag;
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private AudioSource collectSound;

    private Collider objectCollider;

    protected virtual void Awake()
    {
        objectCollider = GetComponent<Collider>();
        objectCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerStat>(out PlayerStat playerStat))
        {
            OnCollect(playerStat);
            PlayEffects();
            ReturnToPool();
        }
    }

    protected abstract void OnCollect(PlayerStat playerStat);

    private void PlayEffects()
    {
        if (collectParticles != null)
        {
            collectParticles.transform.SetParent(null); // Detach to let particles finish
            collectParticles.Play();
        }

        if (collectSound != null)
        {
            collectSound.Play();
        }
    }

    private void ReturnToPool()
    {
        ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
    }

    // IPooledObject interface implementation
    public virtual void OnObjectSpawn()
    {
        // Có thể thêm logic reset tại đây nếu cần
    }
}