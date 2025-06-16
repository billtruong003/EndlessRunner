using UnityEngine;

public class HealthPotionCollectable : BaseCollectable
{
    [SerializeField] private int healthAmount = 20;

    public override void Collect(GameObject collector)
    {
        PlayerStat playerStat = collector.GetComponent<PlayerStat>();
        if (playerStat != null)
        {
            playerStat.RestoreHealth(healthAmount);
        }
    }

    public override void DisableObject()
    {
        gameObject.SetActive(false);
    }
}