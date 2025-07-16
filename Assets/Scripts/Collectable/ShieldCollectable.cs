using UnityEngine;

public class ShieldCollectible : CollectibleBase
{
    [SerializeField] private float shieldDuration = 10f;

    protected override void OnCollect(PlayerStat playerStat)
    {
        playerStat.ActivateShield(shieldDuration);
    }
}