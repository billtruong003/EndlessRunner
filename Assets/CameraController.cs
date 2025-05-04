using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 5f;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothTime = 0.3f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target is not assigned in the CameraController script.");
            return;
        }

        offset = transform.position - target.position;
    }

    // Another Way to Move the Camera
    // void Update()
    // {
    //     if (target == null) return;

    //     Vector3 targetPosition = target.position + offset;
    //     Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, speed * Time.deltaTime);
    //     transform.position = smoothedPosition;
    // }

    public void MoveToTarget(Vector3 newPosition, float duration)
    {
        Vector3 targetPosition = newPosition + offset;
        transform.DOMoveX(targetPosition.x, duration).SetEase(Ease.OutQuad);
    }
}
