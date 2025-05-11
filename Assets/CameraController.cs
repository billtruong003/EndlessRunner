using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.1f; // Thời gian làm mượt cho SmoothDamp
    [SerializeField] private float lerpSmoothFactor = 0.2f; // Hệ số làm mượt cho Lerp
    [SerializeField] private Vector3 offset;

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target is not assigned in the CameraController script.");
            return;
        }

        offset = transform.position - target.position;
    }

    private void Update()
    {
        FollowPosition(target.position, 5);
    }

    // Phương pháp hiện tại dùng SmoothDamp
    public void MoveToTarget(Vector3 newPosition, float followSpeed)
    {
        if (target == null) return;

        Vector3 targetPosition = newPosition + offset;

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime / followSpeed,
            Mathf.Infinity,
            Time.deltaTime
        );

        transform.position = smoothedPosition;
    }

    // Phương pháp mới dùng Lerp động
    public void FollowPosition(Vector3 newPosition, float followSpeed)
    {
        if (target == null) return;

        Vector3 targetPosition = newPosition + offset;

        // Tính khoảng cách để điều chỉnh độ mượt động
        float distance = Vector3.Distance(transform.position, targetPosition);
        float dynamicSmoothFactor = Mathf.Lerp(lerpSmoothFactor * 0.5f, lerpSmoothFactor * 1.5f, distance / 2f);
        float smoothAmount = dynamicSmoothFactor * followSpeed * Time.deltaTime;

        // Làm mượt vị trí bằng Lerp
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothAmount
        );

        transform.position = smoothedPosition;
    }
}