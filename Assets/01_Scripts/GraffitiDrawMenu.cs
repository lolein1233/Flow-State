using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
public class GraffitiDrawMenu : MonoBehaviour
{
    public Transform visualRoot;
    public float drawTime = 0.5f;
    public ParticleSystem drawParticles;

    Vector3 originalScale;
    Collider[] colliders;
    GraffitiPainter painter;
    Mesh previewMesh;
    Mesh previewEchoMesh;
    Material previewMaterial;
    Material previewEchoMaterial;
    Transform previewTransform;
    Transform previewEchoTransform;
    Vector3 previewBasePosition;
    Vector3 previewBaseScale;
    GraffitiNozzleShape previewShape = (GraffitiNozzleShape)(-1);
    Color previewColor = new Color(-1f, -1f, -1f, -1f);
    const int PreviewStrokeStampCount = 9;
    const int PreviewDetailStampCount = 2;
    const int PreviewStampCount = PreviewStrokeStampCount + PreviewDetailStampCount;

    public bool IsReady { get; private set; }

    void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        originalScale = visualRoot.localScale;
        colliders = GetComponentsInChildren<Collider>(true);

        SetColliders(false);
    }

    void Start()
    {
        Open();
    }

    void Update()
    {
        RefreshNozzlePreview();
        AnimateNozzlePreview();
    }

    public void BindPainter(GraffitiPainter value)
    {
        painter = value;
        BuildNozzlePreview();
        RefreshNozzlePreview(true);
    }

    public void SetPresentationScale(float multiplier)
    {
        if (visualRoot == null)
            visualRoot = transform;
        originalScale = visualRoot.localScale * Mathf.Max(1f, multiplier);
        visualRoot.localScale = originalScale;
    }

    void BuildNozzlePreview()
    {
        if (previewMesh != null)
            return;

        Transform artRoot = visualRoot != null ? visualRoot : transform;
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            if (label == null)
                continue;

            string value = label.text ?? string.Empty;
            if (label.gameObject.name == "Index" || value.Contains("TU MURO") || value.Contains("ELEGIR"))
                label.gameObject.SetActive(false);
            else if (label.gameObject.name == "Rack label" || value.Contains("ELIGE TU"))
            {
                artRoot = label.transform.parent;
                label.gameObject.SetActive(false);
            }
        }

        Shader shader = Shader.Find("FLOWSTATE/Graffiti/LayeredSprayStamp");
        if (shader == null)
            return;

        GameObject previewObject = new GameObject("Live Nozzle Spray Preview", typeof(MeshFilter), typeof(MeshRenderer));
        previewObject.layer = gameObject.layer;
        previewObject.transform.SetParent(artRoot, false);
        previewBasePosition = new Vector3(0.875f, 0.3f, -0.098f);
        previewObject.transform.localPosition = previewBasePosition;
        previewObject.transform.localRotation = Quaternion.identity;
        previewObject.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
        previewTransform = previewObject.transform;

        previewMesh = new Mesh { name = "Runtime Nozzle Preview Mesh" };
        BuildPreviewSwatchGeometry(previewMesh, Vector2.one);
        previewObject.GetComponent<MeshFilter>().sharedMesh = previewMesh;

        previewMaterial = new Material(shader) { name = "Runtime Nozzle Preview Material" };
        previewMaterial.SetFloat("_GlobalOpacity", 0.92f);
        MeshRenderer renderer = previewObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = previewMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 18;

        GameObject echoObject = new GameObject("Animated Nozzle Preview Echo", typeof(MeshFilter), typeof(MeshRenderer));
        echoObject.layer = gameObject.layer;
        echoObject.transform.SetParent(artRoot, false);
        echoObject.transform.localPosition = previewBasePosition + new Vector3(0.012f, -0.009f, 0.006f);
        echoObject.transform.localRotation = Quaternion.identity;
        previewEchoTransform = echoObject.transform;
        previewEchoMesh = Instantiate(previewMesh);
        previewEchoMesh.name = "Runtime Nozzle Preview Echo Mesh";
        echoObject.GetComponent<MeshFilter>().sharedMesh = previewEchoMesh;
        previewEchoMaterial = new Material(shader) { name = "Runtime Nozzle Preview Echo Material" };
        previewEchoMaterial.SetFloat("_GlobalOpacity", 0.18f);
        MeshRenderer echoRenderer = echoObject.GetComponent<MeshRenderer>();
        echoRenderer.sharedMaterial = previewEchoMaterial;
        echoRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        echoRenderer.receiveShadows = false;
        echoRenderer.sortingOrder = 17;
    }

    static void BuildPreviewSwatchGeometry(Mesh target, Vector2 footprint)
    {
        footprint.x = Mathf.Max(0.18f, footprint.x);
        footprint.y = Mathf.Max(0.18f, footprint.y);
        var vertices = new List<Vector3>(PreviewStampCount * 4);
        var uvs = new List<Vector2>(PreviewStampCount * 4);
        var triangles = new List<int>(PreviewStampCount * 6);

        for (int stamp = 0; stamp < PreviewStampCount; stamp++)
        {
            bool detail = stamp >= PreviewStrokeStampCount;
            float t = detail ? 1f : stamp / (float)(PreviewStrokeStampCount - 1);
            Vector2 centre;
            float diameterScale;
            float angle;
            if (detail)
            {
                int detailIndex = stamp - PreviewStrokeStampCount;
                centre = detailIndex == 0 ? new Vector2(0.27f, 0.18f) : new Vector2(0.4f, 0.11f);
                diameterScale = detailIndex == 0 ? 0.28f : 0.18f;
                angle = -16f;
            }
            else
            {
                centre = new Vector2(Mathf.Lerp(-0.39f, 0.34f, t), Mathf.Sin(t * Mathf.PI * 1.15f) * 0.09f - 0.035f);
                diameterScale = Mathf.Lerp(0.82f, 1.08f, Mathf.SmoothStep(0f, 1f, t));
                angle = Mathf.Cos(t * Mathf.PI * 1.15f) * 15f;
            }

            Vector2 size = new Vector2(0.31f * footprint.x, 0.31f * footprint.y) * diameterScale;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (size.x * 0.5f);
            Vector2 up = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians)) * (size.y * 0.5f);
            int vertex = vertices.Count;
            vertices.Add(centre - right - up);
            vertices.Add(centre + right - up);
            vertices.Add(centre + right + up);
            vertices.Add(centre - right + up);
            uvs.Add(Vector2.zero);
            uvs.Add(Vector2.right);
            uvs.Add(Vector2.one);
            uvs.Add(Vector2.up);
            triangles.Add(vertex);
            triangles.Add(vertex + 1);
            triangles.Add(vertex + 2);
            triangles.Add(vertex);
            triangles.Add(vertex + 2);
            triangles.Add(vertex + 3);
        }

        target.Clear(false);
        target.SetVertices(vertices);
        target.SetUVs(0, uvs);
        target.SetTriangles(triangles, 0, true);
        target.RecalculateBounds();
    }

    void RefreshNozzlePreview(bool force = false)
    {
        if (painter == null || previewMesh == null)
            return;

        Color color = painter.GetPreviewColor();
        color.a = Mathf.Round(color.a * 20f) / 20f;
        if (!force && previewShape == painter.currentNozzleShape && previewColor == color)
            return;

        previewShape = painter.currentNozzleShape;
        previewColor = color;
        Vector2 footprint = painter.GetPreviewSize();
        BuildPreviewSwatchGeometry(previewMesh, footprint);
        if (previewEchoMesh != null)
            BuildPreviewSwatchGeometry(previewEchoMesh, footprint);
        previewBaseScale = new Vector3(0.78f, 0.78f, 1f);
        previewTransform.localScale = previewBaseScale;
        if (previewEchoTransform != null)
            previewEchoTransform.localScale = Vector3.Scale(previewBaseScale, new Vector3(1.025f, 1.025f, 1f));

        Vector4 spray = painter.GetSpraySettings();
        var colors = new List<Color>(PreviewStampCount * 4);
        var profiles = new List<Vector4>(PreviewStampCount * 4);
        var settings = new List<Vector4>(PreviewStampCount * 4);
        var echoColors = new List<Color>(PreviewStampCount * 4);
        Color echoColor = Color.Lerp(new Color(0.04f, 0.82f, 1f, 0.46f), color, 0.22f);
        echoColor.a = 0.46f;
        for (int stamp = 0; stamp < PreviewStampCount; stamp++)
        {
            bool detail = stamp >= PreviewStrokeStampCount;
            Color stampColor = color;
            stampColor.a *= detail ? 0.7f : 1f;
            Vector4 profile = painter.GetStampProfile(Mathf.Lerp(0.2f, 0.62f, stamp / (float)(PreviewStampCount - 1)), 37.17f + stamp * 0.73f);
            for (int vertex = 0; vertex < 4; vertex++)
            {
                colors.Add(stampColor);
                profiles.Add(profile);
                settings.Add(spray);
                echoColors.Add(echoColor);
            }
        }
        previewMesh.SetColors(colors);
        previewMesh.SetUVs(1, profiles);
        previewMesh.SetUVs(2, settings);

        if (previewEchoMesh != null)
        {
            previewEchoMesh.SetColors(echoColors);
            previewEchoMesh.SetUVs(1, profiles);
            previewEchoMesh.SetUVs(2, settings);
        }
    }

    void AnimateNozzlePreview()
    {
        if (previewTransform == null || previewBaseScale == Vector3.zero)
            return;

        float time = Time.unscaledTime;
        float breathe = 1f + Mathf.Sin(time * 2.4f) * 0.018f;
        previewTransform.localPosition = previewBasePosition + new Vector3(Mathf.Sin(time * 1.4f), Mathf.Cos(time * 1.15f), 0f) * 0.004f;
        previewTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.5f) * 1.4f);
        previewTransform.localScale = previewBaseScale * breathe;

        if (previewEchoTransform != null)
        {
            previewEchoTransform.localPosition = previewBasePosition + new Vector3(0.012f + Mathf.Cos(time * 1.8f) * 0.004f, -0.009f + Mathf.Sin(time * 1.5f) * 0.003f, 0.006f);
            previewEchoTransform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(time * 1.3f) * 1.8f);
            previewEchoTransform.localScale = Vector3.Scale(previewBaseScale, new Vector3(1.025f, 1.025f, 1f)) * (2f - breathe);
        }
    }

    public void Open()
    {
        StopAllCoroutines();
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        IsReady = false;
        SetColliders(false);

        if (drawParticles != null)
            drawParticles.Play();

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / drawTime;

            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualRoot.localScale = originalScale * Mathf.Lerp(0.05f, 1f, eased);

            yield return null;
        }

        visualRoot.localScale = originalScale;

        if (drawParticles != null)
            drawParticles.Stop();

        IsReady = true;
        SetColliders(true);
    }

    public void CloseAndDestroy()
    {
        StopAllCoroutines();
        StartCoroutine(CloseRoutine());
    }

    IEnumerator CloseRoutine()
    {
        IsReady = false;
        SetColliders(false);

        float t = 0f;
        Vector3 startScale = visualRoot.localScale;

        while (t < 1f)
        {
            t += Time.deltaTime / drawTime;

            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualRoot.localScale = Vector3.Lerp(startScale, originalScale * 0.05f, eased);

            yield return null;
        }

        Destroy(gameObject);
    }

    void SetColliders(bool value)
    {
        foreach (Collider c in colliders)
        {
            if (c != null)
                c.enabled = value;
        }
    }

    void OnDestroy()
    {
        if (previewMesh != null)
            Destroy(previewMesh);
        if (previewEchoMesh != null)
            Destroy(previewEchoMesh);
        if (previewMaterial != null)
            Destroy(previewMaterial);
        if (previewEchoMaterial != null)
            Destroy(previewEchoMaterial);
    }
}
