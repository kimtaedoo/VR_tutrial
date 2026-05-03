using UnityEngine;

[ExecuteAlways]
public class TargetRingVisualBuilder : MonoBehaviour
{
    [SerializeField] private bool hideRootRenderer = true;
    [SerializeField] private float frontOffset = -0.52f;
    [SerializeField] private int segments = 96;
    [SerializeField] private Color outerColor = Color.black;
    [SerializeField] private Color outerMiddleColor = Color.white;
    [SerializeField] private Color middleColor = Color.blue;
    [SerializeField] private Color innerMiddleColor = Color.white;
    [SerializeField] private Color centerColor = Color.red;

    private const string VisualRootName = "Generated Target Rings";

    private void OnEnable()
    {
        Rebuild();
    }

    [ContextMenu("Rebuild Target Rings")]
    public void Rebuild()
    {
        RemoveExistingVisual();

        if (TryGetComponent(out Renderer rootRenderer))
        {
            rootRenderer.enabled = !hideRootRenderer;
        }

        Transform visualRoot = new GameObject(VisualRootName).transform;
        visualRoot.SetParent(transform, false);

        CreateDisk(visualRoot, "1 Point Ring", 0.5f, outerColor, 0);
        CreateDisk(visualRoot, "2 Point Ring", 0.4f, outerMiddleColor, 1);
        CreateDisk(visualRoot, "3 Point Ring", 0.3f, middleColor, 2);
        CreateDisk(visualRoot, "5 Point Ring", 0.2f, innerMiddleColor, 3);
        CreateDisk(visualRoot, "10 Point Ring", 0.1f, centerColor, 4);
    }

    private void CreateDisk(Transform parent, string objectName, float radius, Color color, int layerIndex)
    {
        GameObject disk = new GameObject(objectName);
        disk.transform.SetParent(parent, false);
        disk.transform.localPosition = new Vector3(0f, 0f, frontOffset - layerIndex * 0.002f);

        MeshFilter meshFilter = disk.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateDiskMesh(radius);

        MeshRenderer meshRenderer = disk.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateMaterial(color);
    }

    private Mesh CreateDiskMesh(float radius)
    {
        int safeSegments = Mathf.Max(segments, 12);
        Vector3[] vertices = new Vector3[safeSegments + 1];
        int[] triangles = new int[safeSegments * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < safeSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / safeSegments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        int triangleIndex = 0;
        for (int i = 0; i < safeSegments; i++)
        {
            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = ((i + 1) % safeSegments) + 1;
            triangles[triangleIndex++] = i + 1;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Generated Target Ring";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

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
