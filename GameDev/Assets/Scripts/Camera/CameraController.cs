using UnityEngine;

// Small camera controller
public class CameraController : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] private Transform focus;
    [SerializeField] KeyCode up, down, left, right;

    [Header("Parameters")]
    [SerializeField] [Range(1f, 5f)]      private float moveSpeed;
    [SerializeField] [Range(.1f, 10f)]    private float zoomSensitivity;
    [SerializeField] [Range(1f, 5f)]      private float zoomDamp;
    [SerializeField] [Range(0.01f, 0.1f)] private float mouseDamp;

    // Consts for constraints
    private const float maxViewAngle     = 40f;
    private const float minViewAngle     = -40f;
    private const float maxDistFromFocus = 5f;
    private const float minDistFromFocus = 1.5f;

    private const float maxKeyboardDrift = 0.5f;
    private const float verticalMouseSensitivity = 0.9f;
    private const float horizontalMouseSensitivity = 3f;
    private const float mouseInputThreshold = 0.2f;

    private float falloff;

    // Keyboard lerping
    Vector2 kInputVector = Vector2.zero;

    // Scroll lerping
    private float prevZoomDist;
    private float nextZoomDist;
    private float zoomCounter = 0f;

    private void Awake()
    {
        kInputVector = Vector3.zero;  

        prevZoomDist = transform.position.magnitude;
        nextZoomDist = transform.position.magnitude;
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
        // If receiving input, assign
        kInputVector += GetInput();

        if (kInputVector.magnitude > 0f) 
        {
            float x = kInputVector.x != 0 ? Time.deltaTime * Mathf.Sign(kInputVector.x) : 0f;
            float y = kInputVector.y != 0 ? Time.deltaTime * Mathf.Sign(kInputVector.y) : 0f;

            Vector2 subtractVector = new Vector2(x, y);

            // Make sure subtractVector never over subrtracts from the inputVector
            if (GetInput().magnitude == 0f) 
            {
                subtractVector.x = Mathf.Sign(kInputVector.x - subtractVector.x) != Mathf.Sign(kInputVector.x) ? kInputVector.x : subtractVector.x;
                subtractVector.y = Mathf.Sign(kInputVector.y - subtractVector.y) != Mathf.Sign(kInputVector.y) ? kInputVector.y : subtractVector.y;
                kInputVector -= subtractVector; 
            }

            // Clamp movement
            float xx = Mathf.Clamp(kInputVector.x, -maxKeyboardDrift, maxKeyboardDrift);
            float yy = Mathf.Clamp(kInputVector.y, -maxKeyboardDrift, maxKeyboardDrift);

            // Clamps to 0 if would go past the viewAngle constraints
            kInputVector = ClampMovement(new Vector2(xx,yy));

            Vector3 inputAxis = Vector3.zero;
            if (kInputVector.x != 0f) { inputAxis += kInputVector.x > 0f ? -transform.up : transform.up; }
            if (kInputVector.y != 0f) { inputAxis += kInputVector.y > 0f ? transform.right : -transform.right; }

            Vector3 nextPosition = Quaternion.Euler(inputAxis) * transform.position;

            // Gets angle between and lerps
            float angleBetween = Vector3.Angle(transform.position, nextPosition);

            angleBetween *= moveSpeed / 10f;

            // Decrease falloff as kInputVector decreases
            falloff = kInputVector.magnitude > 1f ? 1f : kInputVector.magnitude;

            if (GetInput().magnitude == 0f) { angleBetween *= falloff; }

            float lerpedAngle = Mathf.LerpAngle(angleBetween, 0f, Time.deltaTime);

            transform.RotateAround(focus.position, inputAxis, lerpedAngle);
        }
    }

    // Gets movement from WASD and mouse
    private Vector2 GetInput()
    {
        Vector2 v = Vector2.zero;
        if (Input.GetKey(up)) { v += Vector2.up; }
        if (Input.GetKey(down)) { v += Vector2.down; }
        if (Input.GetKey(left)) { v += Vector2.left; }
        if (Input.GetKey(right)) { v += Vector2.right; }

        v += GetMouseInput();   

        return v;
    }

    private Vector2 GetMouseInput()
    {
        Vector2 v = Vector2.zero;

        if (Input.GetMouseButton(0)) 
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            v += MouseInput;

            // Makes sure the x and y movement are over a threshold
            v.x = Mathf.Abs(v.x) > mouseInputThreshold ? v.x : 0f;
            v.y = Mathf.Abs(v.y) > mouseInputThreshold ? v.y : 0f;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

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
    private Vector2 MouseInput { get { return new Vector2(-Input.GetAxis("Mouse X") * horizontalMouseSensitivity, -Input.GetAxis("Mouse Y") * verticalMouseSensitivity); } }
}
