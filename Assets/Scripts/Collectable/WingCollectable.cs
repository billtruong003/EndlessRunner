using UnityEngine;

public class WingCollectible : CollectibleBase
{
    [Tooltip("Thời gian hiệu ứng bay (tính bằng giây).")]
    [SerializeField] private float flyDuration = 8f;

    protected override void OnCollect(PlayerStat playerStat)
    {
        PlayerController playerController = playerStat.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.ActivateFly(flyDuration);
        }
    }
}