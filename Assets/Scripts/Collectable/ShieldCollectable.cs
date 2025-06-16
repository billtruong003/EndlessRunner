using UnityEngine;

public class ShieldCollectable : BaseCollectable
{
    [SerializeField] private float shieldDuration = 10f;

    public override void Collect(GameObject collector)
    {
        PlayerStat playerStat = collector.GetComponent<PlayerStat>();
        if (playerStat != null)
        {
            playerStat.ActivateShield(shieldDuration);
        }
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