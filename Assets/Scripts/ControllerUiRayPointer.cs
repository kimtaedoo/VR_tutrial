using UnityEngine;
using UnityEngine.UI;

public class ControllerUiRayPointer : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Button targetButton;
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [SerializeField] private OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger;
    [SerializeField] private Color normalColor = new Color(0.1f, 0.7f, 1f, 0.85f);
    [SerializeField] private Color hoverColor = new Color(0.1f, 1f, 0.35f, 0.95f);

    private LineRenderer lineRenderer;
    private RectTransform buttonRect;

    public void Configure(Canvas canvas, Button button)
    {
        targetCanvas = canvas;
        targetButton = button;
        CacheReferences();
    }

    private void Awake()
    {
        EnsureLineRenderer();
        CacheReferences();
    }

    private void OnEnable()
    {
        EnsureLineRenderer();
        CacheReferences();
        lineRenderer.enabled = true;
    }

    private void OnDisable()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        EnsureLineRenderer();
        CacheReferences();

        if (targetCanvas == null || targetButton == null || rayOrigin == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        Vector3 origin = rayOrigin.position;
        Vector3 direction = rayOrigin.forward;
        Ray ray = new Ray(origin, direction);
        Plane canvasPlane = new Plane(targetCanvas.transform.forward, targetCanvas.transform.position);

        bool isHoveringButton = false;
        Vector3 endPoint = origin + direction * maxDistance;

        if (canvasPlane.Raycast(ray, out float enter) && enter >= 0f && enter <= maxDistance)
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            endPoint = hitPoint;
            isHoveringButton = IsPointInsideButton(hitPoint);
        }

        Color lineColor = isHoveringButton ? hoverColor : normalColor;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        if (isHoveringButton && OVRInput.GetDown(clickButton, controller))
        {
            targetButton.onClick.Invoke();
        }
    }

    private bool IsPointInsideButton(Vector3 worldPoint)
    {
        if (targetCanvas == null || buttonRect == null)
        {
            return false;
        }

        Camera eventCamera = targetCanvas.worldCamera != null
            ? targetCanvas.worldCamera
            : Camera.main;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);
        return RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPoint, eventCamera);
    }

    private void CacheReferences()
    {
        if (targetButton != null)
        {
            buttonRect = targetButton.GetComponent<RectTransform>();
        }

        if (rayOrigin == null)
        {
            rayOrigin = FindControllerAnchor();
        }
    }

    private Transform FindControllerAnchor()
    {
        string[] candidateNames =
        {
            "RightControllerAnchor",
            "RightHandAnchor",
            "RightControllerInHandAnchor"
        };

        foreach (string candidateName in candidateNames)
        {
            GameObject candidate = GameObject.Find(candidateName);
            if (candidate != null)
            {
                return candidate.transform;
            }
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer != null)
        {
            return;
        }

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.004f;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null)
        {
            lineRenderer.material = new Material(shader);
        }
    }
}
