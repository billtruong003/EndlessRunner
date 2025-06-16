using UnityEngine;

public class Coin : BaseCollectable
{
    [SerializeField] private int multiplier;

    public void OnAwake()
    {
        meshRenderer.enabled = true;
        anim.enabled = true;
    }

    public override void Collect(GameObject collector)
    {
        multiplier = GameManager.Instance.Multiplier;
        GameManager.Instance.CoinManager.AddCoinSession(GetValue());
    }

    public override int GetValue()
    {
        return value * multiplier;
    }

    public override void DisableObject()
    {
        gameObject.SetActive(false);
    }

    public override void OnCollectEffect()
    {
        base.OnCollectEffect();
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        if (collectEffect != null)
        {
            collectEffect.Play();
        }
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
            anim.enabled = false;
        }
    }
}