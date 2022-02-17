using UnityEngine;

public class PlanetMovement : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] [Range(0.01f, 1f)] float rotationSpeed = 0.5f;

    private void FixedUpdate()
    {
        // Rotate planet
        transform.Rotate(transform.up, rotationSpeed);
    }
}
