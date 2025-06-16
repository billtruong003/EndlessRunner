using System;
using UnityEngine;

public abstract class BaseCollectable : MonoBehaviour, ICollectable
{
    [SerializeField] protected LayerMask layerPlayer;
    [SerializeField] protected ECollectable collectableName;
    [SerializeField] protected MeshRenderer meshRenderer;
    [SerializeField] protected Animator anim;
    [SerializeField] protected AudioClip collectSound;
    [SerializeField] protected ParticleSystem collectEffect;
    [SerializeField] protected float collectEffectDuration = 1f;
    [SerializeField] protected int value = 1;

    [SerializeField] protected bool isCollected;

    public abstract void Collect(GameObject collector);
    public virtual int GetValue()
    {
        return value;
    }

    public virtual void OnCollectEffect()
    {
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

    public virtual void DisableObject()
    {
        gameObject.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        int playerLayerIndex = Mathf.RoundToInt(Mathf.Log(layerPlayer.value, 2));
        if (other.gameObject.layer == playerLayerIndex)
        {
            isCollected = true;
            Collect(other.gameObject);
            OnCollectEffect();
            Invoke("DisableObject", collectEffectDuration);
        }
    }
}