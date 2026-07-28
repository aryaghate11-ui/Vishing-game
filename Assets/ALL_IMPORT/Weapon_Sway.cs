using UnityEngine;

public class Weapon_Sway : MonoBehaviour
{
    [Header("Position Sway")]
    public float swayAmount = 0.02f;
    public float maxSway = 0.06f;
    public float smoothSway = 8f;

    [Header("Rotation Sway")]
    public float rotationSwayAmount = 3f;
    public float maxRotationSway = 5f;
    public float smoothRotation = 10f;

    [Header("Smoothing")]
    public float returnSpeed = 4f;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Vector3 swayVelocity;
    private Vector3 inputSmooth;

    void Start()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }

    void Update()
    {
        ApplySway();
    }

    void ApplySway()
    {
        // --- Mouse Input (smoothed) ---
        float mouseX = -Input.GetAxisRaw("Mouse X");
        float mouseY = -Input.GetAxisRaw("Mouse Y");

        // Smooth out input so quick flicks don’t look jerky
        inputSmooth = Vector3.Lerp(inputSmooth, new Vector3(mouseX, mouseY, 0), Time.deltaTime * smoothSway);

        // --- Positional Sway ---
        Vector3 moveOffset = new Vector3(inputSmooth.x * swayAmount, inputSmooth.y * swayAmount, 0);
        moveOffset.x = Mathf.Clamp(moveOffset.x, -maxSway, maxSway);
        moveOffset.y = Mathf.Clamp(moveOffset.y, -maxSway, maxSway);

        transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos + moveOffset, Time.deltaTime * returnSpeed);

        // --- Rotational Sway ---
        float tiltX = inputSmooth.y * rotationSwayAmount;
        float tiltY = inputSmooth.x * rotationSwayAmount;

        Quaternion finalRot = Quaternion.Euler(tiltX, tiltY, tiltY * 0.5f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRot * finalRot, Time.deltaTime * smoothRotation);
    }
}
