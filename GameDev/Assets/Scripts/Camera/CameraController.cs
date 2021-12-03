using UnityEngine;

// Small camera controller
public class CameraController : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] private Transform focus;
    [SerializeField] KeyCode up, down, left, right;

    [Header("Parameters")]
    [SerializeField] [Range(1f, 10f)] private float moveSpeed;
    [SerializeField] [Range(.1f, 10f)] private float zoomSensitivity;
    [SerializeField] [Range(1f, 5f)] private float zoomDamp;

    // Consts for constraints
    private const float maxViewAngle = 40f;
    private const float minViewAngle = -40f;
    private const float maxDistFromFocus = 5f;
    private const float minDistFromFocus = 2f;

    private const float maxKeyboardSpeed = 10f;

    // Scroll lerping
    private float prevZoomDist;
    private float nextZoomDist;
    private float zoomCounter = 0f;

    private void Awake()
    {
        prevZoomDist = transform.position.magnitude;
        nextZoomDist = transform.position.magnitude;
    }

    private void Update()
    {
        LookAtFocus();
        KeyboardMove();
        MouseMove();
        Zoom();
    }

    private void LookAtFocus()
    {
        transform.LookAt(focus);
    }

    // Moves the camera
    private void KeyboardMove()
    {
        // If receiving input, assign
        Vector2 inputVector = GetKeyboardInput();

        if (inputVector.magnitude != 0f) 
        {
            float x = inputVector.x != 0 ? 1f * Mathf.Sign(inputVector.x) : 0f;
            float y = inputVector.y != 0 ? 1f * Mathf.Sign(inputVector.y) : 0f;

            Vector2 subtractVector = new Vector2(x, y);

            if (GetKeyboardInput().magnitude == 0f) { inputVector -= subtractVector; }

            // Clamp movement
            float xx = Mathf.Clamp(inputVector.x, -maxKeyboardSpeed, maxKeyboardSpeed);
            float yy = Mathf.Clamp(inputVector.y, -maxKeyboardSpeed, maxKeyboardSpeed);

            // Clamps to 0 if would go past the viewAngle constraints
            inputVector = ClampKeyboardMovement(new Vector2(xx,yy));

            Vector3 inputAxis = Vector3.zero;
            if (inputVector.x != 0f) { inputAxis += inputVector.x > 0f ? -transform.up : transform.up; }
            if (inputVector.y != 0f) { inputAxis += inputVector.y > 0f ? transform.right : -transform.right; }

            Vector3 nextPosition = Quaternion.Euler(inputAxis) * transform.position;
            transform.position = Vector3.Lerp(transform.position, nextPosition, moveSpeed * Time.deltaTime * 20f);
        }
    }

    private void MouseMove()
    {

    }

    // Gets movement from WASD and mouse
    private Vector2 GetKeyboardInput()
    {
        Vector2 v = Vector2.zero;
        if (Input.GetKey(up)) { v += Vector2.up; }
        if (Input.GetKey(down)) { v += Vector2.down; }
        if (Input.GetKey(left)) { v += Vector2.left; }
        if (Input.GetKey(right)) { v += Vector2.right; }

        return v;
    }

    // Clamps the movement by maxViewAngle and minViewAngle
    private Vector2 ClampKeyboardMovement(Vector2 inputVector)
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
        // whenever mouse is scrolled up or down, add +1 or -1 respectively.
        if (ScrollWheelInput != 0f) { zoomCounter -= Mathf.Sign(ScrollWheelInput) * zoomSensitivity; }

        if (zoomCounter == 0f) { prevZoomDist = transform.position.magnitude; }

        // If there is zoom on zoomCounter, shift position
        if (zoomCounter != 0f) 
        {
            // Find out the zoom
            nextZoomDist = prevZoomDist + zoomCounter;

            // Clamp
            nextZoomDist = Mathf.Clamp(nextZoomDist, minDistFromFocus, maxDistFromFocus);

            // Decrease zoom counter
            zoomCounter -= Mathf.Sign(zoomCounter) * zoomSensitivity;
        }

        // Lerp
        float lerpedZoomDist = Mathf.Lerp(prevZoomDist, nextZoomDist, zoomDamp * Time.deltaTime);

        // Translate
        transform.position = transform.position.normalized * lerpedZoomDist;
    }

    // Reference to Mouse ScrollWheel Axis
    private float ScrollWheelInput { get { return Input.GetAxisRaw("Mouse ScrollWheel"); } }

    // Reference to Mouse Movement
    private Vector2 MouseInput { get { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")); } }
}
