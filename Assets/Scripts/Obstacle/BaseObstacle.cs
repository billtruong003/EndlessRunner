using UnityEngine;

public class BaseObstacle : MonoBehaviour
{
    [SerializeField] protected int damage = 10;
    [SerializeField] protected string obstacleName = "Obstacle";

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStat playerStat = other.GetComponent<PlayerStat>();
            if (playerStat != null)
            {
                playerStat.TakeDamage(damage);
            }
        }
    }
}