using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum GraffitiNozzleShape
{
    SoftRound,
    Needle,
    FatCap,
    Chisel,
    Splatter
}

public class GraffitiPainter : MonoBehaviour
{
    public Camera cam;
    public GameObject decalPrefab;
    public GraffitiRaycastUI raycastUI;
    public FPSController fps;
    public GameObject paintUI;
    public ParticleSystem sprayParticles;
    public GraffitiAnimation sprayCan;
    public TMP_Text paintText;

    [Header("Menu dibujado")]
    public bool menuOpen;

    [Header("Pintando / Free Paint")]
    public Color currentSprayColor = Color.black;
    public float brushMinSize = 0.07f;
    public float brushMaxSize = 0.12f;
    public float freePaintRate = 0.006f;
    public float brushSpacingMultiplier = 0.22f;
    public float brushOpacity = 0.58f;
    public float brushJitter = 0.12f;
    public float brushColorJitter = 0.025f;
    public Texture2D brushTexture;
    public GraffitiNozzleShape currentNozzleShape = GraffitiNozzleShape.SoftRound;
    public string currentNozzleName = "Soft Round";

    [Header("Color dinamico")]
    [Range(0f, 1f)] public float currentColorHue;
    [Range(0f, 1f)] public float currentColorSaturation;
    [Range(0f, 1f)] public float currentColorValue = 1f;

    [Header("Recursos")]
    public SprayResourceSystem sprayResources;
    public SprayResourceHUD resourceHUD;

    [Header("UI")]
    public float uiFadeSpeed = 3f;
    public float uiVisibleTime = 6f;
    public float popSpeed = 6f;
    public Vector3 hiddenScale = new Vector3(0.9f, 0.9f, 0.9f);
    public Vector3 visibleScale = Vector3.one;

    [Header("Pintado Decal")]
    public float paintSpeed = 1f;

    [Header("Modos")]
    public bool useDecalMode = true;
    public bool tabCyclesNozzles = true;
    public bool requireGraffitiMode = true;
    public bool hideSprayCanOutsideGraffiti = true;
    public bool hidePaintUIOutsideGraffiti = true;
    public GameObject[] extraGraffitiOnlyUI;

    GraffitiFade currentGraffiti;
    bool isPainting;
    bool emissionAllowed;
    float targetAlpha;
    float uiTimer;
    int nozzleIndex;
    GameObject[] autoGraffitiOnlyUI = new GameObject[0];

    public bool IsPainting => isPainting;
    public bool CanEmitPaint => isPainting && emissionAllowed && sprayResources != null && sprayResources.HasPaint && !sprayResources.IsShaking;

    void Start()
    {
        SyncHSVFromCurrentColor();
        EnsureRuntimeSystems();

        if (tabCyclesNozzles)
            useDecalMode = false;

        nozzleIndex = GetNozzleIndex(currentNozzleShape);

        if (paintText != null)
        {
            Color color = paintText.color;
            color.a = 0f;
            paintText.color = color;
            paintText.transform.localScale = hiddenScale;
        }

        CacheAutoGraffitiOnlyUI();
        bool active = IsGraffitiModeActive();
        SetPaintUIVisible(active);
        SetGraffitiToolVisible(active);
        if (resourceHUD != null)
            resourceHUD.SetVisible(active);
    }

    void Update()
    {
        UpdateUIFade();
        bool graffitiModeActive = IsGraffitiModeActive();
        SetPaintUIVisible(graffitiModeActive);
        SetGraffitiToolVisible(graffitiModeActive);

        if (resourceHUD != null)
            resourceHUD.SetVisible(graffitiModeActive);

        if (sprayCan != null && sprayResources != null)
            sprayCan.SetShake(sprayResources.GetShakeTravel(), sprayResources.IsShaking);

        if (graffitiModeActive && !menuOpen && sprayResources != null && Input.GetKeyDown(sprayResources.ShakeKey))
        {
            if (isPainting)
                StopPainting();

            sprayResources.TryStartShake();
            if (sprayCan != null)
                sprayCan.SetShake(sprayResources.GetShakeTravel(), true);
            return;
        }

        if (menuOpen || !graffitiModeActive || (sprayResources != null && sprayResources.IsShaking))
        {
            targetAlpha = 0f;
            uiTimer = 0f;
            if (isPainting)
                StopPainting();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (tabCyclesNozzles)
                CycleNozzle();
            else
                useDecalMode = !useDecalMode;
        }

        if (raycastUI == null || !raycastUI.CanPaint())
        {
            targetAlpha = 0f;
            uiTimer = 0f;
            if (isPainting)
                StopPainting();
            return;
        }

        if (!isPainting)
        {
            uiTimer += Time.deltaTime;
            targetAlpha = uiTimer <= uiVisibleTime ? 1f : 0f;
        }

        if (Input.GetMouseButtonDown(0))
            StartPainting();

        if (Input.GetMouseButton(0) && isPainting)
            ContinuePainting();

        if (Input.GetMouseButtonUp(0))
            StopPainting();
    }

    void EnsureRuntimeSystems()
    {
        if (sprayResources == null)
            sprayResources = GetComponent<SprayResourceSystem>();
        if (sprayResources == null)
            sprayResources = gameObject.AddComponent<SprayResourceSystem>();

        if (resourceHUD == null)
            resourceHUD = GetComponent<SprayResourceHUD>();
        if (resourceHUD == null)
            resourceHUD = gameObject.AddComponent<SprayResourceHUD>();

        Canvas canvas = FindNozzleHintCanvas();
        if (canvas == null && paintText != null)
            canvas = paintText.GetComponentInParent<Canvas>(true);
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        resourceHUD.Bind(sprayResources, canvas, paintText != null ? paintText.font : null);
    }

    static Canvas FindNozzleHintCanvas()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
                continue;

            if (text.text.ToLowerInvariant().Contains("boquilla"))
                return text.GetComponentInParent<Canvas>(true);
        }

        return null;
    }

    void StartPainting()
    {
        if (raycastUI == null || !raycastUI.CanPaint())
            return;

        RaycastHit hit = raycastUI.GetHit();
        SetGraffitiToolVisible(true);

        if (fps != null)
        {
            fps.EnterGraffitiMode();
            fps.canMove = false;
            fps.focusMode = true;
            fps.SetPaintingAnimatorState(true);
        }

        raycastUI.isPainting = true;
        targetAlpha = 0f;

        if (useDecalMode && decalPrefab != null)
        {
            Vector3 position = hit.point + hit.normal * 0.01f;
            Quaternion rotation = Quaternion.LookRotation(-hit.normal);
            GameObject decal = Instantiate(decalPrefab, position, rotation);
            currentGraffiti = decal.GetComponent<GraffitiFade>();
        }

        isPainting = true;
        sprayResources.BeginSpray(currentNozzleShape);
        emissionAllowed = sprayResources.CanEmitAt(Time.time);
        SetParticleEmission(emissionAllowed);

        if (sprayCan != null)
            sprayCan.StartSpray();
    }

    void ContinuePainting()
    {
        emissionAllowed = sprayResources == null || sprayResources.TickSpray(Time.deltaTime, currentNozzleShape);
        SetParticleEmission(emissionAllowed);

        if (!useDecalMode || currentGraffiti == null || !emissionAllowed)
            return;

        currentGraffiti.AddPaint(Time.deltaTime * paintSpeed);
        if (currentGraffiti.IsComplete())
            StopPainting();
    }

    void StopPainting()
    {
        isPainting = false;
        emissionAllowed = false;

        if (fps != null)
        {
            fps.canMove = true;
            fps.focusMode = false;
            fps.SetPaintingAnimatorState(false);
        }

        if (raycastUI != null)
            raycastUI.isPainting = false;

        uiTimer = uiVisibleTime;
        targetAlpha = 0f;
        SetParticleEmission(false);

        if (sprayCan != null)
            sprayCan.StopSpray();
        if (sprayResources != null)
            sprayResources.EndSpray();

        SetGraffitiToolVisible(IsGraffitiModeActive());
    }

    void SetParticleEmission(bool active)
    {
        if (sprayParticles == null || !sprayParticles.gameObject.activeInHierarchy)
            return;

        if (active)
        {
            if (!sprayParticles.isPlaying)
                sprayParticles.Play();
        }
        else if (sprayParticles.isPlaying)
        {
            sprayParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void UpdateUIFade()
    {
        if (paintText == null)
            return;

        Color color = paintText.color;
        color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * uiFadeSpeed);
        paintText.color = color;
        Vector3 scale = targetAlpha > 0.1f ? visibleScale : hiddenScale;
        paintText.transform.localScale = Vector3.Lerp(paintText.transform.localScale, scale, Time.deltaTime * popSpeed);
    }

    public void CycleNozzle()
    {
        nozzleIndex = (nozzleIndex + 1) % 5;
        ApplyBuiltInNozzle(nozzleIndex);
    }

    void ApplyBuiltInNozzle(int index)
    {
        switch (index)
        {
            case 0:
                SetNozzleProfile("Soft Round", 0.08f, 0.13f, 0.006f, 0.19f, 0.58f, 0.12f, GraffitiNozzleShape.SoftRound, brushTexture);
                break;
            case 1:
                SetNozzleProfile("Needle", 0.024f, 0.044f, 0.005f, 0.17f, 0.7f, 0.055f, GraffitiNozzleShape.Needle, brushTexture);
                break;
            case 2:
                SetNozzleProfile("Fat Cap", 0.22f, 0.35f, 0.007f, 0.16f, 0.42f, 0.2f, GraffitiNozzleShape.FatCap, brushTexture);
                break;
            case 3:
                SetNozzleProfile("Chisel", 0.13f, 0.21f, 0.006f, 0.15f, 0.56f, 0.055f, GraffitiNozzleShape.Chisel, brushTexture);
                break;
            default:
                SetNozzleProfile("Splatter", 0.12f, 0.23f, 0.009f, 0.23f, 0.66f, 0.32f, GraffitiNozzleShape.Splatter, brushTexture);
                break;
        }
    }

    public void SetFreePaintColor(Color newColor)
    {
        currentSprayColor = newColor;
        SyncHSVFromCurrentColor();
    }

    public void SetFreePaintHSV(float hue, float saturation, float value)
    {
        currentColorHue = Mathf.Repeat(hue, 1f);
        currentColorSaturation = Mathf.Clamp01(saturation);
        currentColorValue = Mathf.Clamp01(value);
        currentSprayColor = Color.HSVToRGB(currentColorHue, currentColorSaturation, currentColorValue);
        currentSprayColor.a = 1f;
    }

    public void SetFreePaintHueSaturation(float hue, float saturation)
    {
        SetFreePaintHSV(hue, saturation, currentColorValue);
    }

    public void SetFreePaintValue(float value)
    {
        SetFreePaintHSV(currentColorHue, currentColorSaturation, value);
    }

    public void SetNozzle(float minSize, float maxSize, float paintRate)
    {
        SetNozzleProfile("Personalizada", minSize, maxSize, paintRate, 0.22f, 0.58f, 0.12f, currentNozzleShape, brushTexture);
    }

    public void SetNozzleProfile(string nozzleName, float minSize, float maxSize, float paintRate, float spacingMultiplier, float opacity, float jitter, GraffitiNozzleShape shape, Texture2D texture)
    {
        currentNozzleName = string.IsNullOrEmpty(nozzleName) ? "Boquilla" : nozzleName;
        brushMinSize = Mathf.Max(0.001f, Mathf.Min(minSize, maxSize));
        brushMaxSize = Mathf.Max(brushMinSize, Mathf.Max(minSize, maxSize));
        freePaintRate = Mathf.Max(0.001f, paintRate);
        brushSpacingMultiplier = Mathf.Clamp(spacingMultiplier, 0.05f, 0.7f);
        brushOpacity = Mathf.Clamp01(opacity);
        brushJitter = Mathf.Clamp01(jitter);
        currentNozzleShape = shape;
        nozzleIndex = GetNozzleIndex(shape);

        if (texture != null)
            brushTexture = texture;
    }

    public float GetBrushSize()
    {
        return Random.Range(brushMinSize, brushMaxSize);
    }

    public float GetBrushSpacing(float size)
    {
        float density = sprayResources != null ? sprayResources.OutputDensity : 1f;
        return Mathf.Max(0.0035f, size * brushSpacingMultiplier / Mathf.Max(0.5f, density));
    }

    public Color GetPaintColorWithVariation()
    {
        return GetPaintColorWithVariation(freePaintRate, 1f);
    }

    public Color GetPaintColorWithVariation(float exposureSeconds, float opacityMultiplier)
    {
        Color.RGBToHSV(currentSprayColor, out float hue, out float saturation, out float value);
        value = Mathf.Clamp01(value + Random.Range(-brushColorJitter, brushColorJitter));

        Color color = Color.HSVToRGB(hue, saturation, value);
        float resourceOpacity = sprayResources != null ? sprayResources.OutputOpacity : 1f;
        float referenceAlpha = Mathf.Clamp(brushOpacity * 0.22f * opacityMultiplier * resourceOpacity, 0f, 0.95f);
        float referenceFrames = Mathf.Max(0.02f, exposureSeconds * 60f);
        color.a = 1f - Mathf.Pow(1f - referenceAlpha, referenceFrames);
        return color;
    }

    public Vector4 GetStampProfile(float distance01, float seed)
    {
        float core;
        float halo;
        float haloOpacity;
        switch (currentNozzleShape)
        {
            case GraffitiNozzleShape.Needle:
                core = 0.67f; halo = 0.9f; haloOpacity = 0.12f; break;
            case GraffitiNozzleShape.FatCap:
                core = 0.61f; halo = 0.99f; haloOpacity = 0.34f; break;
            case GraffitiNozzleShape.Chisel:
                core = 0.7f; halo = 0.94f; haloOpacity = 0.18f; break;
            case GraffitiNozzleShape.Splatter:
                core = 0.47f; halo = 1f; haloOpacity = 0.4f; break;
            default:
                core = 0.58f; halo = 0.97f; haloOpacity = 0.24f; break;
        }

        core = Mathf.Lerp(core * 1.08f, core * 0.78f, distance01);
        haloOpacity = Mathf.Lerp(haloOpacity * 0.72f, haloOpacity * 1.25f, distance01);
        return new Vector4(core, halo, haloOpacity, seed);
    }

    public Vector4 GetSpraySettings()
    {
        float grainDensity;
        float hardness;
        switch (currentNozzleShape)
        {
            case GraffitiNozzleShape.Needle: grainDensity = 48f; hardness = 0.86f; break;
            case GraffitiNozzleShape.FatCap: grainDensity = 31f; hardness = 0.58f; break;
            case GraffitiNozzleShape.Chisel: grainDensity = 43f; hardness = 0.8f; break;
            case GraffitiNozzleShape.Splatter: grainDensity = 24f; hardness = 0.48f; break;
            default: grainDensity = 38f; hardness = 0.67f; break;
        }

        float density = sprayResources != null ? sprayResources.OutputDensity : 1f;
        float instability = sprayResources != null ? sprayResources.Instability : 0f;
        return new Vector4((float)currentNozzleShape, grainDensity * density, hardness, instability);
    }

    public Color GetFullValueColor()
    {
        Color color = Color.HSVToRGB(currentColorHue, currentColorSaturation, 1f);
        color.a = 1f;
        return color;
    }

    void SyncHSVFromCurrentColor()
    {
        Color.RGBToHSV(currentSprayColor, out currentColorHue, out currentColorSaturation, out currentColorValue);
    }

    public void CancelPaintingForMenu()
    {
        if (isPainting)
            StopPainting();

        currentGraffiti = null;
        targetAlpha = 0f;
        if (raycastUI != null)
            raycastUI.isPainting = false;
        if (fps != null)
            fps.SetPaintingAnimatorState(false);
        SetParticleEmission(false);
        if (sprayCan != null)
            sprayCan.StopSpray();
        if (sprayResources != null)
            sprayResources.EndSpray();
        SetGraffitiToolVisible(IsGraffitiModeActive());
    }

    bool IsGraffitiModeActive()
    {
        return !requireGraffitiMode || fps == null || fps.IsGraffitiMode();
    }

    void SetGraffitiToolVisible(bool visible)
    {
        if (!hideSprayCanOutsideGraffiti)
            return;

        if (sprayParticles != null)
        {
            if (!visible)
                sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (sprayParticles.gameObject.activeSelf != visible)
                sprayParticles.gameObject.SetActive(visible);
        }

        if (sprayCan != null && sprayCan.gameObject.activeSelf != visible)
            sprayCan.gameObject.SetActive(visible);
    }

    void SetPaintUIVisible(bool visible)
    {
        if (!hidePaintUIOutsideGraffiti)
            return;

        if (paintUI != null && paintUI.activeSelf != visible)
            paintUI.SetActive(visible);
        SetUIGroupVisible(extraGraffitiOnlyUI, visible);
        SetUIGroupVisible(autoGraffitiOnlyUI, visible);
    }

    static void SetUIGroupVisible(GameObject[] objects, bool visible)
    {
        if (objects == null)
            return;

        foreach (GameObject target in objects)
        {
            if (target != null && target.activeSelf != visible)
                target.SetActive(visible);
        }
    }

    void CacheAutoGraffitiOnlyUI()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> matches = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        foreach (TMP_Text text in texts)
        {
            if (text == null || text == paintText || text.gameObject == paintUI)
                continue;

            string value = text.text;
            if (!string.IsNullOrEmpty(value) && value.ToLowerInvariant().Contains("boquilla"))
            {
                Canvas canvas = text.GetComponentInParent<Canvas>(true);
                GameObject target = canvas != null ? canvas.gameObject : text.gameObject;
                if (target != null && seen.Add(target))
                    matches.Add(target);
            }
        }

        autoGraffitiOnlyUI = matches.ToArray();
    }

    static int GetNozzleIndex(GraffitiNozzleShape shape)
    {
        switch (shape)
        {
            case GraffitiNozzleShape.Needle: return 1;
            case GraffitiNozzleShape.FatCap: return 2;
            case GraffitiNozzleShape.Chisel: return 3;
            case GraffitiNozzleShape.Splatter: return 4;
            default: return 0;
        }
    }
}
