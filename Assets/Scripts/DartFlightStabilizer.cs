using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(Rigidbody))]
public class DartFlightStabilizer : MonoBehaviour
{
    [SerializeField] private float minimumSpeed = 0.25f;
    [SerializeField] private float angularDampingStrength = 10f;
    [SerializeField] private float alignDegreesPerSecond = 1440f;
    [SerializeField] private bool forceRotationWhileHeld = false;
    [SerializeField] private float heldAlignDegreesPerSecond = 360f;
    [SerializeField] private bool alignOnceOnGrab = true;
    [SerializeField] private bool snapRotation = true;
    [SerializeField] private float throwVelocityMultiplier = 1.35f;
    [SerializeField] private float maxThrowSpeed = 8f;

    private Rigidbody objectRigidbody;
    private Grabbable grabbable;
    private int pendingThrowBoostFrames;

    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
    }

    private void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += HandlePointerEventRaised;
        }
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEventRaised;
        }
    }

    private void LateUpdate()
    {
        if (objectRigidbody == null || !forceRotationWhileHeld || !IsGrabbed())
        {
            return;
        }

        AlignHeldRotation(heldAlignDegreesPerSecond * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (objectRigidbody == null || objectRigidbody.isKinematic)
        {
            return;
        }

        ApplyPendingThrowBoost();

        Vector3 velocity = objectRigidbody.linearVelocity;
        if (velocity.sqrMagnitude < minimumSpeed * minimumSpeed)
        {
            return;
        }

        if (snapRotation)
        {
            objectRigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            objectRigidbody.angularVelocity = Vector3.MoveTowards(
                objectRigidbody.angularVelocity,
                Vector3.zero,
                angularDampingStrength * Time.fixedDeltaTime);
        }

        Quaternion targetRotation = GetTargetRotation(velocity.normalized);
        Quaternion nextRotation = GetAlignedRotation(
            objectRigidbody.rotation,
            targetRotation,
            Time.fixedDeltaTime);

        objectRigidbody.MoveRotation(nextRotation);
    }

    private void HandlePointerEventRaised(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type == PointerEventType.Select && alignOnceOnGrab)
        {
            AlignHeldRotation(float.MaxValue);
            return;
        }

        if (pointerEvent.Type == PointerEventType.Unselect)
        {
            pendingThrowBoostFrames = 2;
        }
    }

    private void AlignHeldRotation(float maxDegreesDelta)
    {
        Quaternion targetRotation = GetTargetRotation(GetPlayerForward());
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            maxDegreesDelta);

        objectRigidbody.angularVelocity = Vector3.zero;
    }

    private Vector3 GetPlayerForward()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.0001f)
            {
                return forward.normalized;
            }
        }

        return transform.forward;
    }

    private void ApplyPendingThrowBoost()
    {
        if (pendingThrowBoostFrames <= 0)
        {
            return;
        }

        pendingThrowBoostFrames--;

        Vector3 velocity = objectRigidbody.linearVelocity;
        if (velocity.sqrMagnitude < minimumSpeed * minimumSpeed)
        {
            return;
        }

        Vector3 boostedVelocity = velocity * throwVelocityMultiplier;
        if (boostedVelocity.sqrMagnitude > maxThrowSpeed * maxThrowSpeed)
        {
            boostedVelocity = boostedVelocity.normalized * maxThrowSpeed;
        }

        objectRigidbody.linearVelocity = boostedVelocity;
    }

    private bool IsGrabbed()
    {
        return grabbable != null && grabbable.SelectingPointsCount > 0;
    }

    private Quaternion GetTargetRotation(Vector3 forward)
    {
        Vector3 normalizedForward = forward.normalized;
        Vector3 up = Vector3.up;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            up = mainCamera.transform.up;
        }

        Vector3 stableUp = Vector3.ProjectOnPlane(up, normalizedForward);
        if (stableUp.sqrMagnitude < 0.0001f)
        {
            stableUp = Vector3.ProjectOnPlane(Vector3.up, normalizedForward);
        }

        if (stableUp.sqrMagnitude < 0.0001f)
        {
            stableUp = Vector3.ProjectOnPlane(Vector3.right, normalizedForward);
        }

        return Quaternion.LookRotation(normalizedForward, stableUp.normalized);
    }

    private Quaternion GetAlignedRotation(Quaternion currentRotation, Quaternion targetRotation, float deltaTime)
    {
        if (snapRotation)
        {
            return targetRotation;
        }

        return Quaternion.RotateTowards(
            currentRotation,
            targetRotation,
            alignDegreesPerSecond * deltaTime);
    }
}
