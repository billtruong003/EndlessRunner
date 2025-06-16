using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public int TotalCoin { get; set; }
    public int SessionCoin { get; set; }

    public int GetTotalCoin() => TotalCoin;
    public void AddTotalCoin() => TotalCoin += SessionCoin;
    public void AddCoinSession(int coin) => SessionCoin += coin;
}