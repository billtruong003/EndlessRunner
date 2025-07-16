using UnityEngine;

public class CoinCollectible : CollectibleBase
{
    [SerializeField] private int coinValue = 1;

    protected override void OnCollect(PlayerStat playerStat)
    {
        // Giả sử PlayerStat có một phương thức để thêm tiền
        // playerStat.AddCoins(coinValue); 
        Debug.Log($"Collected {coinValue} coin(s).");
    }
}