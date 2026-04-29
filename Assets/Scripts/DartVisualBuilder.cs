using UnityEngine;

public class DartVisualBuilder : MonoBehaviour
{
    [SerializeField] private bool hideRootRenderer = false;
    [SerializeField] private bool configureBoxCollider = false;
    [SerializeField] private float bodyLength = 0.75f;
    [SerializeField] private float bodyRadius = 0.06f;
    [SerializeField] private float tipLength = 0.22f;
    [SerializeField] private float finLength = 0.26f;
    [SerializeField] private float finHeight = 0.14f;
    [SerializeField] private Vector3 grabColliderSize = new Vector3(0.45f, 0.45f, 1.35f);
    [SerializeField] private Color bodyColor = new Color(0.18f, 0.18f, 0.2f);
    [SerializeField] private Color tipColor = new Color(0.8f, 0.78f, 0.68f);
    [SerializeField] private Color finColor = new Color(0.9f, 0.12f, 0.08f);

    private const string VisualRootName = "Generated Dart Visual";

    private void Awake()
    {
        Rebuild();
    }

    [ContextMenu("Rebuild Dart Visual")]
    public void Rebuild()
    {
        RemoveExistingVisual();

        if (TryGetComponent(out Renderer rootRenderer))
        {
            rootRenderer.enabled = !hideRootRenderer;
        }

        if (configureBoxCollider && TryGetComponent(out BoxCollider boxCollider))
        {
            boxCollider.center = Vector3.zero;
            boxCollider.size = grabColliderSize;
        }

        Transform visualRoot = new GameObject(VisualRootName).transform;
        visualRoot.SetParent(transform, false);

        CreateBody(visualRoot);
        CreateTip(visualRoot);
        CreateFins(visualRoot);
    }

    private void CreateBody(Transform parent)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(parent, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(bodyRadius * 2f, bodyLength * 0.5f, bodyRadius * 2f);
        body.GetComponent<Collider>().enabled = false;
        body.GetComponent<Renderer>().sharedMaterial = CreateMaterial(bodyColor);
    }

    private void CreateTip(Transform parent)
    {
        GameObject tip = new GameObject("Tip");
        tip.transform.SetParent(parent, false);
        tip.transform.localPosition = new Vector3(0f, 0f, bodyLength * 0.5f + tipLength * 0.5f);
        tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        MeshFilter meshFilter = tip.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateConeMesh(bodyRadius * 1.2f, tipLength, 24);
        tip.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(tipColor);
    }

    private void CreateFins(Transform parent)
    {
        float rearZ = -bodyLength * 0.5f - finLength * 0.3f;

        for (int i = 0; i < 4; i++)
        {
            GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = $"Fin {i + 1}";
            fin.transform.SetParent(parent, false);
            fin.transform.localPosition = new Vector3(0f, 0f, rearZ);
            fin.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            fin.transform.localScale = new Vector3(0.012f, finHeight, finLength);
            fin.GetComponent<Collider>().enabled = false;
            fin.GetComponent<Renderer>().sharedMaterial = CreateMaterial(finColor);
        }
    }

    private Mesh CreateConeMesh(float radius, float height, int segments)
    {
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = new Vector3(0f, height * 0.5f, 0f);
        vertices[1] = new Vector3(0f, -height * 0.5f, 0f);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, -height * 0.5f, Mathf.Sin(angle) * radius);
        }

        int triangleIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int current = i + 2;
            int next = ((i + 1) % segments) + 2;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;

            triangles[triangleIndex++] = 1;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Generated Dart Tip";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void RemoveExistingVisual()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(VisualRootName))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
