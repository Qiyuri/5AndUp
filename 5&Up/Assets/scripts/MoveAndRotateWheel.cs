using UnityEngine;

public class MoveAndRotateWheel : MonoBehaviour
{
    [Header("Wielen")]
    public Transform wheel1;
    public Transform wheel2;

    [Header("Beweging")]
    [Tooltip("Bewegingsrichting in lokale ruimte. Pas deze aan in de Inspector.")]
    public Vector3 moveDirection = Vector3.forward;

    [Tooltip("Maximale snelheid van het voertuig.")]
    public float maxSpeed = 5f;

    [Tooltip("Hoe snel het voertuig accelereert.")]
    public float acceleration = 5f;

    [Tooltip("Hoe snel het voertuig remt wanneer je Space ingedrukt houdt.")]
    public float brakeForce = 20f;

    public enum StoppieAxis
    {
        X,
        Y,
        Z
    }

    [Tooltip("Hoe ver het voertuig kantelt tijdens een stoppie.")]
    public float stoppieAngle = 30f;

    [Tooltip("De lokale as waarop de stoppie draait.")]
    public StoppieAxis stoppieAxis = StoppieAxis.X;

    [Tooltip("Hoe snel het voertuig naar de stoppie-positie kantelt.")]
    public float stoppieSpeed = 90f;

    [Header("Rotatie")]
    public float rotationSpeed = 360f;

    [Header("Gronddetectie")]
    [Tooltip("Controleer met wheel1 of het wiel contact maakt met de grond.")]
    public float groundCheckDistance = 0.2f;

    float currentSpeed;
    float currentStoppieAngle;

    void Update()
    {
        float targetSpeed = 0f;
        bool wheel1Grounded = IsWheelGrounded(wheel1);

        if (Input.GetKey(KeyCode.Space))
        {
            targetSpeed = 0f;
        }
        else if (wheel1Grounded)
        {
            if (Input.GetKey(KeyCode.W))
                targetSpeed = maxSpeed;
            else if (Input.GetKey(KeyCode.S))
                targetSpeed = -maxSpeed;
        }

        float accel = Input.GetKey(KeyCode.Space) ? brakeForce : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

        if (Mathf.Abs(currentSpeed) > 0.001f)
        {
            Vector3 movement = moveDirection.normalized * currentSpeed * Time.deltaTime;
            transform.Translate(movement, Space.Self);

            float rotationAmount = rotationSpeed * Mathf.Sign(currentSpeed) * Time.deltaTime;
            if (wheel1 != null)
                wheel1.Rotate(0f, 0f, rotationAmount, Space.Self);
            if (wheel2 != null)
                wheel2.Rotate(0f, 0f, rotationAmount, Space.Self);
        }

        UpdateStoppie();
    }

    void UpdateStoppie()
    {
        float targetStoppie = 0f;
        if (Input.GetKey(KeyCode.Space) && currentSpeed > 0.1f && IsWheelGrounded(wheel1))
        {
            targetStoppie = stoppieAngle;
        }

        currentStoppieAngle = Mathf.MoveTowards(currentStoppieAngle, targetStoppie, stoppieSpeed * Time.deltaTime);
        ApplyStoppieRotation();
    }

    void ApplyStoppieRotation()
    {
        Vector3 euler = transform.localEulerAngles;

        if (stoppieAxis == StoppieAxis.X)
            euler.x = currentStoppieAngle;
        else if (stoppieAxis == StoppieAxis.Y)
            euler.y = currentStoppieAngle;
        else if (stoppieAxis == StoppieAxis.Z)
            euler.z = currentStoppieAngle;

        transform.localEulerAngles = euler;
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    bool IsWheelGrounded(Transform wheel)
    {
        if (wheel == null)
            return false;

        return Physics.Raycast(wheel.position, Vector3.down, groundCheckDistance);
    }
}
