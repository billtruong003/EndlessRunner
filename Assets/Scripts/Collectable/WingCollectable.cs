using UnityEngine;

public class WingsCollectable : BaseCollectable
{

    [SerializeField] private float speedBoost = 2f;
    [SerializeField] private float boostDuration = 5f;

    public override void Collect(GameObject collector)
    {
        PlayerStat playerStat = collector.GetComponent<PlayerStat>();
        if (playerStat != null)
        {
            playerStat.BoostSpeed(speedBoost, boostDuration);
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