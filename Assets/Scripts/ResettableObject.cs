using UnityEngine;
using Oculus.Interaction;
using System.Collections.Generic;

public class ResettableObject : MonoBehaviour
{
    [SerializeField] private float resetBelowY = -0.5f;
    [SerializeField] private bool waitInStartPosition = true;
    [SerializeField] private bool useCameraRelativeStartPosition = false;
    [SerializeField] private float cameraForwardOffset = 0.3f;
    [SerializeField] private float cameraDownOffset = 0.2f;
    [SerializeField] private float cameraSideOffset = 0.2f;
    [SerializeField] private bool alignRotationToCameraForward = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody objectRigidbody;
    private Grabbable grabbable;
    private bool isWaitingForGrab;
    private int lastGrabSide = 1;
    private readonly Dictionary<Renderer, bool> rendererVisibility = new Dictionary<Renderer, bool>();
    private readonly Dictionary<Collider, bool> colliderVisibility = new Dictionary<Collider, bool>();

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        objectRigidbody = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();

        if (grabbable != null)
        {
            grabbable.ForceKinematicDisabled = true;
        }

        if (waitInStartPosition)
        {
            SetWaitingForGrab();
        }
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

    private void Start()
    {
        if (waitInStartPosition)
        {
            ResetToStart();
        }
    }

    private void Update()
    {
        if (!IsVisible())
        {
            return;
        }

        if (transform.position.y < resetBelowY)
        {
            ResetToStart();
            return;
        }

        if (waitInStartPosition && isWaitingForGrab)
        {
            KeepWaitingForGrab();
        }

        if (waitInStartPosition && isWaitingForGrab)
        {
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }

    public void ResetToStart()
    {
        RestoreVisibility();
        UpdateStartPoseFromCamera();
        transform.SetPositionAndRotation(startPosition, startRotation);

        if (waitInStartPosition)
        {
            SetWaitingForGrab();
        }
    }

    private void UpdateStartPoseFromCamera()
    {
        if (!useCameraRelativeStartPosition)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Transform cameraTransform = mainCamera.transform;
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = cameraTransform.forward;
        }

        forward.Normalize();
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.Cross(Vector3.up, forward);
        }

        right.Normalize();
        startPosition = cameraTransform.position
            + forward * cameraForwardOffset
            + right * cameraSideOffset * lastGrabSide
            - Vector3.up * cameraDownOffset;

        if (alignRotationToCameraForward)
        {
            startRotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    private void SetWaitingForGrab()
    {
        isWaitingForGrab = true;

        if (objectRigidbody != null)
        {
            objectRigidbody.linearVelocity = Vector3.zero;
            objectRigidbody.angularVelocity = Vector3.zero;
            objectRigidbody.useGravity = false;
            objectRigidbody.isKinematic = true;
        }
    }

    private void HandlePointerEventRaised(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type != PointerEventType.Select || !isWaitingForGrab)
        {
            return;
        }

        RememberGrabSide(pointerEvent.Pose.position);
        isWaitingForGrab = false;
        EnablePhysics();
    }

    public void HideUntilReset()
    {
        isWaitingForGrab = false;
        CaptureVisibility();
        SetHidden();

        if (objectRigidbody != null)
        {
            objectRigidbody.linearVelocity = Vector3.zero;
            objectRigidbody.angularVelocity = Vector3.zero;
            objectRigidbody.useGravity = false;
            objectRigidbody.isKinematic = true;
        }
    }

    private void RememberGrabSide(Vector3 grabPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 toGrab = grabPosition - mainCamera.transform.position;
        float side = Vector3.Dot(toGrab, mainCamera.transform.right);
        lastGrabSide = side >= 0f ? 1 : -1;
    }

    private bool IsVisible()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return true;
        }

        foreach (Renderer objectRenderer in renderers)
        {
            if (objectRenderer.enabled)
            {
                return true;
            }
        }

        return false;
    }

    private void CaptureVisibility()
    {
        rendererVisibility.Clear();
        foreach (Renderer objectRenderer in GetComponentsInChildren<Renderer>(true))
        {
            rendererVisibility[objectRenderer] = objectRenderer.enabled;
        }

        colliderVisibility.Clear();
        foreach (Collider objectCollider in GetComponents<Collider>())
        {
            colliderVisibility[objectCollider] = objectCollider.enabled;
        }
    }

    private void RestoreVisibility()
    {
        foreach (KeyValuePair<Renderer, bool> entry in rendererVisibility)
        {
            if (entry.Key != null)
            {
                entry.Key.enabled = entry.Value;
            }
        }

        foreach (KeyValuePair<Collider, bool> entry in colliderVisibility)
        {
            if (entry.Key != null)
            {
                entry.Key.enabled = entry.Value;
            }
        }
    }

    private void SetHidden()
    {
        foreach (Renderer objectRenderer in GetComponentsInChildren<Renderer>(true))
        {
            objectRenderer.enabled = false;
        }

        foreach (Collider objectCollider in GetComponents<Collider>())
        {
            objectCollider.enabled = false;
        }
    }

    private void KeepWaitingForGrab()
    {
        if (objectRigidbody != null)
        {
            objectRigidbody.linearVelocity = Vector3.zero;
            objectRigidbody.angularVelocity = Vector3.zero;
            objectRigidbody.useGravity = false;
            objectRigidbody.isKinematic = true;
        }
    }

    private void EnablePhysics()
    {
        if (objectRigidbody != null)
        {
            objectRigidbody.isKinematic = false;
            objectRigidbody.useGravity = true;
        }
    }
}
