using UnityEngine;

public class CurveController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [Header("Curve Settings")]
    [SerializeField] private Material roadMaterial; // Material using RoundWorldToon shader
    [SerializeField] private float baseLateralCurve = 0.0f; // Base lateral curve value
    [SerializeField] private float maxLateralCurve = 0.2f; // Maximum lateral curve deviation
    [SerializeField] private float curveChangeSpeed = 1.0f; // Speed of curve oscillation
    [SerializeField] private float curveChangeAmplitude = 0.1f; // Amplitude of curve variation
    [SerializeField] private float distanceIncreaseRate = 0.01f; // Rate at which curve increases with distance

    private float timeElapsed = 0f;
    private float initialZPosition = 0f;


    void Start()
    {
        if (roadMaterial == null)
        {
            Debug.LogError("CurveController: Missing road material reference!");
            enabled = false;
            return;
        }

        if (playerTransform != null)
        {
            initialZPosition = playerTransform.position.z;
        }

        // Set initial curve value
        roadMaterial.SetFloat("_LateralCurve", baseLateralCurve);
        // Initialize a random phase for the oscillation
        timeElapsed = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (playerTransform == null || roadMaterial == null) return;

        // Calculate distance traveled by player (assuming movement in negative Z direction)
        float distanceTraveled = initialZPosition - playerTransform.position.z;

        // Increase curve based on distance traveled
        float distanceCurve = baseLateralCurve + (distanceTraveled * distanceIncreaseRate);
        distanceCurve = Mathf.Clamp(distanceCurve, baseLateralCurve, maxLateralCurve);

        // Add oscillating curve for a winding effect with random phase
        timeElapsed += Time.deltaTime * curveChangeSpeed;
        float oscillatingCurve = Mathf.Sin(timeElapsed) * curveChangeAmplitude;

        // Combine base, distance-based, and oscillating curve
        float finalCurve = distanceCurve + oscillatingCurve;
        finalCurve = Mathf.Clamp(finalCurve, -maxLateralCurve, maxLateralCurve);

        // Apply the curve to the material
        roadMaterial.SetFloat("_LateralCurve", finalCurve);
    }

    // Public method to set player transform if needed
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
        initialZPosition = playerTransform.position.z;
    }

    // Public method to reset curve to base value
    public void ResetCurve()
    {
        if (roadMaterial != null)
        {
            roadMaterial.SetFloat("_LateralCurve", baseLateralCurve);
        }
        timeElapsed = 0f;
    }
}