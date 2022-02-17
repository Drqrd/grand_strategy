using UnityEngine;

public class Star : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private Transform focus;
    [SerializeField] [Range(1f, 100f)] private float movementSpeed;

    private Vector3 rotationAxis;

    private void Start()
    {
        Vector3 fow = transform.forward;
        Vector3 right = transform.right;
        rotationAxis = Vector3.Cross(fow, right);
    }

    private void FixedUpdate()
    {
        transform.LookAt(focus);
        transform.RotateAround(focus.transform.position, rotationAxis, movementSpeed / 360f);
    }
}
