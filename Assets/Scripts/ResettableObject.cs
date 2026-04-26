using UnityEngine;
using Oculus.Interaction;

public class ResettableObject : MonoBehaviour
{
    [SerializeField] private float resetBelowY = -0.5f;
    [SerializeField] private bool waitInStartPosition = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody objectRigidbody;
    private Grabbable grabbable;
    private bool isWaitingForGrab;

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
        transform.SetPositionAndRotation(startPosition, startRotation);

        if (waitInStartPosition)
        {
            SetWaitingForGrab();
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

        isWaitingForGrab = false;
        EnablePhysics();
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
