using UnityEngine;

// Small camera controller
public class CameraController : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] private Transform focus;
    [SerializeField] KeyCode up, down, left, right;

    [Header("Parameters")]
    [SerializeField] [Range(20f, 40f )] private float moveSpeed;
    [SerializeField] [Range(20f, 40f)] private float zoomStrength;
    [SerializeField] [Range(10f, 20f)] private float moveDamp;
    [SerializeField] [Range(10f, 20f)] private float zoomDamp;

    // Consts for constraints
    private const float maxViewAngle = 40f;
    private const float minViewAngle = -40f;
    private const float maxDistFromFocus = 5f;
    private const float minDistFromFocus = 2f;

    // Vars for lerping
    private Vector3 prevPos;
    private float   toZoom;

    private void Awake()
    {
        prevPos = transform.position;
    }

    private void Update()
    {
        LookAtFocus();
        Move();
        Zoom();
    }

    private void LookAtFocus()
    {
        transform.LookAt(focus);
    }

    // Moves the camera
    private void Move()
    {
        Vector2 inputVector = ClampMovement(GetMovementInput());

        Vector3 inputAxis = Vector3.zero;
        if (inputVector.x != 0f) { inputAxis += inputVector.x > 0f ? -transform.up : transform.up; }
        if (inputVector.y != 0f) { inputAxis += inputVector.y > 0f ? transform.right : -transform.right; }

        Vector3 direction = transform.position;
        direction = Quaternion.Euler(inputAxis) * direction;
        transform.position = Vector3.Lerp(transform.position, direction, moveDamp * Time.deltaTime * 2f);
    }

    // Gets movement from WASD and mouse
    private Vector2 GetMovementInput()
    {
        Vector2 v = Vector2.zero;
        if (Input.GetKey(up))    { v += Vector2.up; }
        if (Input.GetKey(down))  { v += Vector2.down; }
        if (Input.GetKey(left))  { v += Vector2.left; }
        if (Input.GetKey(right)) { v += Vector2.right; }

        return v;
    }

    // Clamps the movement by maxViewAngle and minViewAngle
    private Vector2 ClampMovement(Vector2 inputVector)
    {
        float rot = transform.rotation.eulerAngles.x;
        float trueRotation = rot > 180f ? rot - 360f : rot;
        float val = inputVector.y + trueRotation;
        if (val > maxViewAngle || val < minViewAngle) { inputVector.y = 0f; }

        return inputVector;
    }

    // Zoom function
    private void Zoom()
    {
        float scrollInput = ClampScroll(Input.GetAxisRaw("Mouse ScrollWheel"));
        Vector3 zoom = (focus.position - transform.position).normalized * scrollInput * zoomStrength * Time.deltaTime * 2f;
        transform.position += zoom;

    }

    // Clamps distance from focus by maxDistFromFocus and minDistFromFocus
    private float ClampScroll(float input)
    {
        float distFromFocus = Vector3.Distance(transform.position, focus.position);
        float threshold = distFromFocus - input;
        if (threshold > maxDistFromFocus || threshold < minDistFromFocus) { input = 0; }

        return input;
    }
}
