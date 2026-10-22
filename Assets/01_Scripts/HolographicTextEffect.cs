using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class HolographicTextEffect : MonoBehaviour
{
    [Header("Foil holografico")]
    [SerializeField] Color readableBase = new Color(0.09f, 0.045f, 0.12f, 1f);
    [SerializeField] Color coolTint = new Color(0.055f, 0.62f, 0.72f, 1f);
    [SerializeField] Color warmTint = new Color(0.72f, 0.07f, 0.38f, 1f);
    [SerializeField] Color acidTint = new Color(0.64f, 0.52f, 0.065f, 1f);
    [SerializeField] Color violetTint = new Color(0.42f, 0.1f, 0.66f, 1f);
    [SerializeField] Color pearlTint = new Color(0.9f, 0.88f, 0.96f, 1f);
    [SerializeField, Range(0f, 1f)] float foilStrength = 0.9f;
    [SerializeField, Range(0f, 1f)] float reflectionStrength = 0.42f;
    [SerializeField, Range(0.02f, 0.35f)] float reflectionWidth = 0.1f;
    [SerializeField, Range(0.01f, 0.5f)] float sweepSpeed = 0.165f;
    [SerializeField, Range(0.5f, 4f)] float bandCount = 1.2f;
    [SerializeField, Range(-1f, 1f)] float diagonalBias = 0.34f;

    [Header("Legibilidad")]
    [SerializeField, Range(0f, 0.5f)] float outlineWidth = 0.1f;
    [SerializeField, Range(0f, 0.5f)] float outlineSoftness = 0.055f;
    [SerializeField] Color outlineColor = new Color(0.018f, 0.012f, 0.028f, 0.98f);
    [SerializeField, Range(0f, 1f)] float blurredFoilAlpha = 0.24f;

    TMP_Text label;
    Material runtimeMaterial;
    Material originalMaterial;
    Color originalTextColor;
    bool hasOriginalTextColor;
    string lastText;

    static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    static readonly int OutlineSoftnessId = Shader.PropertyToID("_OutlineSoftness");
    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");
    static readonly int UnderlayColorId = Shader.PropertyToID("_UnderlayColor");
    static readonly int UnderlayOffsetXId = Shader.PropertyToID("_UnderlayOffsetX");
    static readonly int UnderlayOffsetYId = Shader.PropertyToID("_UnderlayOffsetY");
    static readonly int UnderlayDilateId = Shader.PropertyToID("_UnderlayDilate");
    static readonly int UnderlaySoftnessId = Shader.PropertyToID("_UnderlaySoftness");

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();

        originalTextColor = label.color;
        hasOriginalTextColor = true;
        // These prompts are shown/hidden through their GameObjects or parent
        // CanvasGroups. A stale zero alpha from their previous disabled frame
        // must not make the foil invisible when the prompt is reactivated.
        label.color = Color.white;
        BuildReadableFoilMaterial();
        RefreshGeometry();
        ApplyFoil(Time.unscaledTime);
        Canvas.willRenderCanvases += ApplyFoilBeforeCanvasRender;
    }

    void LateUpdate()
    {
        if (label == null)
            return;

        if (lastText != label.text)
            RefreshGeometry();

        ApplyFoil(Time.unscaledTime);
    }

    void ApplyFoilBeforeCanvasRender()
    {
        if (!isActiveAndEnabled || label == null || !(label is TextMeshProUGUI))
            return;

        if (lastText != label.text)
            RefreshGeometry();
        ApplyFoil(Time.unscaledTime);
    }

    void BuildReadableFoilMaterial()
    {
        if (label.fontSharedMaterial == null)
            return;

        originalMaterial = label.fontSharedMaterial;
        runtimeMaterial = new Material(originalMaterial)
        {
            name = originalMaterial.name + " · Holographic Text (Runtime)"
        };

        // Vertex colors carry the foil. Keeping the face white prevents the
        // label's old tint from flattening the darker physical spectrum.
        if (runtimeMaterial.HasProperty(FaceColorId))
            runtimeMaterial.SetColor(FaceColorId, Color.white);

        if (runtimeMaterial.HasProperty(OutlineWidthId))
        {
            runtimeMaterial.EnableKeyword("OUTLINE_ON");
            runtimeMaterial.SetFloat(OutlineWidthId, outlineWidth);
            runtimeMaterial.SetFloat(OutlineSoftnessId, outlineSoftness);
            runtimeMaterial.SetColor(OutlineColorId, outlineColor);
        }

        if (runtimeMaterial.HasProperty(UnderlayColorId))
        {
            runtimeMaterial.EnableKeyword("UNDERLAY_ON");
            runtimeMaterial.SetFloat(UnderlayOffsetXId, 0.32f);
            runtimeMaterial.SetFloat(UnderlayOffsetYId, -0.32f);
            runtimeMaterial.SetFloat(UnderlayDilateId, 0.12f);
            runtimeMaterial.SetFloat(UnderlaySoftnessId, 0.36f);
        }

        // Ya es una instancia exclusiva; asignarla como shared evita que TMP cree
        // una segunda copia que deje el underlay animado apuntando al material viejo.
        label.fontSharedMaterial = runtimeMaterial;
    }

    void RefreshGeometry()
    {
        label.ForceMeshUpdate();
        lastText = label.text;
    }

    void ApplyFoil(float time)
    {
        TMP_TextInfo textInfo = label.textInfo;
        if (textInfo == null || textInfo.characterCount == 0)
            return;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
                continue;
            minX = Mathf.Min(minX, character.bottomLeft.x);
            maxX = Mathf.Max(maxX, character.topRight.x);
            minY = Mathf.Min(minY, character.bottomLeft.y);
            maxY = Mathf.Max(maxY, character.topRight.y);
        }

        float width = Mathf.Max(0.001f, maxX - minX);
        float height = Mathf.Max(0.001f, maxY - minY);
        float timePhase = time * sweepSpeed;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
                continue;

            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            for (int corner = 0; corner < 4; corner++)
            {
                float normalizedX = (vertices[vertexIndex + corner].x - minX) / width;
                float normalizedY = (vertices[vertexIndex + corner].y - minY) / height;
                float phase = Mathf.Repeat(
                    normalizedX * bandCount + normalizedY * diagonalBias - timePhase,
                    1f);
                Color foil = FoilPalette(phase);
                Color readableFoil = Color.Lerp(readableBase, foil, foilStrength);

                // A narrow pearl reflection sells the foil without turning the
                // complete word into a bright neon gradient.
                float reflectionDistance = WrappedDistance(phase, 0.48f);
                float reflection = 1f - Mathf.SmoothStep(0f, reflectionWidth, reflectionDistance);
                readableFoil = Color.Lerp(readableFoil, pearlTint, reflection * reflectionStrength);
                readableFoil *= 1.08f + reflection * 0.12f;
                readableFoil.a = label.color.a;
                colors[vertexIndex + corner] = readableFoil;
            }
        }

        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        if (runtimeMaterial != null && runtimeMaterial.HasProperty(UnderlayColorId))
        {
            Color blurredFoil = FoilPalette(Mathf.Repeat(0.82f - timePhase, 1f));
            blurredFoil.a = blurredFoilAlpha;
            runtimeMaterial.SetColor(UnderlayColorId, blurredFoil);
        }
    }

    Color FoilPalette(float phase)
    {
        float scaled = phase * 4f;
        if (scaled < 1f)
            return Color.Lerp(warmTint, acidTint, Mathf.SmoothStep(0f, 1f, scaled));
        if (scaled < 2f)
            return Color.Lerp(acidTint, coolTint, Mathf.SmoothStep(0f, 1f, scaled - 1f));
        if (scaled < 3f)
            return Color.Lerp(coolTint, violetTint, Mathf.SmoothStep(0f, 1f, scaled - 2f));
        return Color.Lerp(violetTint, warmTint, Mathf.SmoothStep(0f, 1f, scaled - 3f));
    }

    static float WrappedDistance(float a, float b)
    {
        float direct = Mathf.Abs(a - b);
        return Mathf.Min(direct, 1f - direct);
    }

    void OnDisable()
    {
        Canvas.willRenderCanvases -= ApplyFoilBeforeCanvasRender;

        if (label != null && originalMaterial != null)
            label.fontSharedMaterial = originalMaterial;
        if (label != null && hasOriginalTextColor)
            label.color = originalTextColor;
        hasOriginalTextColor = false;

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }
}
