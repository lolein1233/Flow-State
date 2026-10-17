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
        previewObject.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
        previewTransform = previewObject.transform;

        previewMesh = new Mesh { name = "Runtime Nozzle Preview Mesh" };
        previewMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        previewMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        previewMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        previewMesh.RecalculateBounds();
        previewObject.GetComponent<MeshFilter>().sharedMesh = previewMesh;

        previewMaterial = new Material(shader) { name = "Runtime Nozzle Preview Material" };
        MeshRenderer renderer = previewObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = previewMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 18;

        GameObject echoObject = new GameObject("Animated Nozzle Preview Echo", typeof(MeshFilter), typeof(MeshRenderer));
        echoObject.layer = gameObject.layer;
        echoObject.transform.SetParent(artRoot, false);
        echoObject.transform.localPosition = previewBasePosition + new Vector3(0.018f, -0.012f, 0.006f);
        echoObject.transform.localRotation = Quaternion.identity;
        previewEchoTransform = echoObject.transform;
        previewEchoMesh = Instantiate(previewMesh);
        previewEchoMesh.name = "Runtime Nozzle Preview Echo Mesh";
        echoObject.GetComponent<MeshFilter>().sharedMesh = previewEchoMesh;
        previewEchoMaterial = new Material(shader) { name = "Runtime Nozzle Preview Echo Material" };
        previewEchoMaterial.SetFloat("_GlobalOpacity", 0.32f);
        MeshRenderer echoRenderer = echoObject.GetComponent<MeshRenderer>();
        echoRenderer.sharedMaterial = previewEchoMaterial;
        echoRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        echoRenderer.receiveShadows = false;
        echoRenderer.sortingOrder = 17;
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
        previewBaseScale = new Vector3(1.08f * footprint.x, 1.08f * footprint.y, 1f);
        previewTransform.localScale = previewBaseScale;
        if (previewEchoTransform != null)
            previewEchoTransform.localScale = Vector3.Scale(previewBaseScale, new Vector3(1.13f, 1.13f, 1f));

        Vector4 profile = painter.GetStampProfile(0.55f, 37.17f);
        Vector4 spray = painter.GetSpraySettings();
        List<Color> colors = new List<Color>(4) { color, color, color, color };
        List<Vector4> profiles = new List<Vector4>(4) { profile, profile, profile, profile };
        List<Vector4> settings = new List<Vector4>(4) { spray, spray, spray, spray };
        previewMesh.SetColors(colors);
        previewMesh.SetUVs(1, profiles);
        previewMesh.SetUVs(2, settings);

        if (previewEchoMesh != null)
        {
            Color echoColor = new Color(0.04f, 0.82f, 1f, 0.52f);
            List<Color> echoColors = new List<Color>(4) { echoColor, echoColor, echoColor, echoColor };
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
        float breathe = 1f + Mathf.Sin(time * 2.8f) * 0.045f;
        previewTransform.localPosition = previewBasePosition + new Vector3(Mathf.Sin(time * 1.7f), Mathf.Cos(time * 1.35f), 0f) * 0.008f;
        previewTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.9f) * 3.2f);
        previewTransform.localScale = previewBaseScale * breathe;

        if (previewEchoTransform != null)
        {
            previewEchoTransform.localPosition = previewBasePosition + new Vector3(Mathf.Cos(time * 2.1f) * 0.02f, Mathf.Sin(time * 1.8f) * 0.015f, 0.006f);
            previewEchoTransform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(time * 1.55f) * 5f);
            previewEchoTransform.localScale = Vector3.Scale(previewBaseScale, new Vector3(1.13f, 1.13f, 1f)) * (2f - breathe);
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
