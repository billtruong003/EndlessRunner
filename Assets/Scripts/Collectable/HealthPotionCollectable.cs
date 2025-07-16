using UnityEngine;

public class HealthCollectible : CollectibleBase
{
    [SerializeField] private int healthToRestore = 25;

    protected override void OnCollect(PlayerStat playerStat)
    {
        playerStat.RestoreHealth(healthToRestore);
    }
}
