using UnityEngine;

public interface ICollectable
{
    public void Collect(GameObject collector);
    public int GetValue();
    public void OnCollectEffect();
    public void DisableObject();
}
