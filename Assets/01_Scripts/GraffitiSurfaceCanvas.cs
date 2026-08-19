using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class GraffitiSurfaceCanvas : MonoBehaviour
{
    public int maxStampCount = 12000;

    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Vector2> uvs = new List<Vector2>();
    readonly List<Color32> colors = new List<Color32>();
    readonly List<Vector4> profiles = new List<Vector4>();
    readonly List<Vector4> spraySettings = new List<Vector4>();
    readonly List<int> triangles = new List<int>();

    Mesh mesh;
    MeshRenderer meshRenderer;
    bool meshDirty;
    int stampCount;

    public static GraffitiSurfaceCanvas GetOrCreate(Transform surface, Material material)
    {
        if (surface == null)
            return null;

        GraffitiSurfaceCanvas existing = surface.GetComponentInChildren<GraffitiSurfaceCanvas>(true);
        if (existing != null)
        {
            existing.SetMaterial(material);
            return existing;
        }

        GameObject canvasObject = new GameObject("__Graffiti Surface Canvas");
        canvasObject.layer = surface.gameObject.layer;
        canvasObject.transform.SetParent(surface, false);
        canvasObject.transform.localPosition = Vector3.zero;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one;

        GraffitiSurfaceCanvas canvas = canvasObject.AddComponent<GraffitiSurfaceCanvas>();
        canvas.Initialize(material);
        return canvas;
    }

    void Awake()
    {
        Initialize(null);
    }

    void Initialize(Material material)
    {
        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "Runtime Graffiti Surface Mesh",
                indexFormat = IndexFormat.UInt32
            };
            mesh.MarkDynamic();

            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter == null)
                filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.allowOcclusionWhenDynamic = false;
            meshRenderer.sortingOrder = 24;
        }

        SetMaterial(material);
    }

    public void SetMaterial(Material material)
    {
        if (material != null && meshRenderer != null && meshRenderer.sharedMaterial != material)
            meshRenderer.sharedMaterial = material;
    }

    public bool AddStamp(
        Vector3 worldPoint,
        Vector3 worldNormal,
        Vector3 worldTangent,
        float width,
        float height,
        Color color,
        Vector4 profile,
        Vector4 spray)
    {
        if (stampCount >= maxStampCount || width <= 0f || height <= 0f)
            return false;

        Vector3 normal = worldNormal.normalized;
        Vector3 tangent = Vector3.ProjectOnPlane(worldTangent, normal).normalized;
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(normal, Vector3.up).normalized;
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(normal, Vector3.forward).normalized;

        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
        Vector3 halfTangent = tangent * (width * 0.5f);
        Vector3 halfBitangent = bitangent * (height * 0.5f);

        int startVertex = vertices.Count;
        vertices.Add(transform.InverseTransformPoint(worldPoint - halfTangent - halfBitangent));
        vertices.Add(transform.InverseTransformPoint(worldPoint + halfTangent - halfBitangent));
        vertices.Add(transform.InverseTransformPoint(worldPoint + halfTangent + halfBitangent));
        vertices.Add(transform.InverseTransformPoint(worldPoint - halfTangent + halfBitangent));

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(0f, 1f));

        Color32 color32 = color;
        for (int i = 0; i < 4; i++)
        {
            colors.Add(color32);
            profiles.Add(profile);
            spraySettings.Add(spray);
        }

        triangles.Add(startVertex);
        triangles.Add(startVertex + 1);
        triangles.Add(startVertex + 2);
        triangles.Add(startVertex);
        triangles.Add(startVertex + 2);
        triangles.Add(startVertex + 3);

        stampCount++;
        meshDirty = true;
        return true;
    }

    void LateUpdate()
    {
        if (!meshDirty || mesh == null)
            return;

        mesh.Clear(false);
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetUVs(1, profiles);
        mesh.SetUVs(2, spraySettings);
        mesh.SetTriangles(triangles, 0, true);
        mesh.RecalculateBounds();
        meshDirty = false;
    }

    public int GetStampCount()
    {
        return stampCount;
    }

    void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }
}
