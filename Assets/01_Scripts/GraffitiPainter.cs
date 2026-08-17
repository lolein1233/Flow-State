using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;

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
    public bool menuOpen = false;

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

    [Header("UI")]
    public float uiFadeSpeed = 3f;
    float targetAlpha = 0f;

    public float uiVisibleTime = 6f;
    float uiTimer = 0f;

    public float popSpeed = 6f;
    public Vector3 hiddenScale = new Vector3(0.9f, 0.9f, 0.9f);
    public Vector3 visibleScale = Vector3.one;

    [Header("Pintado Decal")]
    public float paintSpeed = 1f;

    GraffitiFade currentGraffiti;
    bool isPainting = false;

    [Header("Modos")]
    public bool useDecalMode = true;
    public bool requireGraffitiMode = true;
    public bool hideSprayCanOutsideGraffiti = true;
    public bool hidePaintUIOutsideGraffiti = true;
    public GameObject[] extraGraffitiOnlyUI;

    GameObject[] autoGraffitiOnlyUI = new GameObject[0];


    void Start()
    {
        SyncHSVFromCurrentColor();

        if (paintText != null)
        {
            Color c = paintText.color;
            c.a = 0f;
            paintText.color = c;
            paintText.transform.localScale = hiddenScale;
        }

        CacheAutoGraffitiOnlyUI();

        bool graffitiModeActive = IsGraffitiModeActive();
        SetPaintUIVisible(graffitiModeActive);
        SetGraffitiToolVisible(graffitiModeActive);
    }

    void Update()
    {
        UpdateUIFade();
        bool graffitiModeActive = IsGraffitiModeActive();
        SetPaintUIVisible(graffitiModeActive);
        SetGraffitiToolVisible(graffitiModeActive);

        if (menuOpen)
        {
            targetAlpha = 0f;

            if (isPainting)
                StopPainting();

            return;
        }
        

        if (Input.GetKeyDown(KeyCode.Tab))
            useDecalMode = !useDecalMode;

        if (!graffitiModeActive)
        {
            targetAlpha = 0f;
            uiTimer = 0f;

            if (isPainting)
                StopPainting();

            return;
        }

        if (!useDecalMode)
        {
            targetAlpha = 0f;

            if (isPainting)
                StopPainting();

            return;
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

            if (uiTimer <= uiVisibleTime)
                targetAlpha = 1f;
            else
                targetAlpha = 0f;
        }

        if (Input.GetMouseButtonDown(0))
            StartPainting();

        if (Input.GetMouseButton(0) && isPainting)
            ContinuePainting();

        if (Input.GetMouseButtonUp(0))
            StopPainting();
    }

   

    void StartPainting()
    {
        RaycastHit hit = raycastUI.GetHit();
        SetGraffitiToolVisible(true);

        if (fps != null)
        {
            fps.EnterGraffitiMode();
            fps.canMove = false;
            fps.focusMode = true;
            fps.SetPaintingAnimatorState(true);
        }

        if (raycastUI != null)
            raycastUI.isPainting = true;

        targetAlpha = 0f;

        Vector3 pos = hit.point + hit.normal * 0.01f;
        Quaternion rot = Quaternion.LookRotation(-hit.normal);

        GameObject decal = Instantiate(decalPrefab, pos, rot);

        // IMPORTANTE:
        // Aquí NO cambiamos el color del decal.
        // El Decal usa su material/textura original.

        currentGraffiti = decal.GetComponent<GraffitiFade>();

        isPainting = true;

        if (sprayParticles != null)
            sprayParticles.Play();

        if (sprayCan != null)
            sprayCan.StartSpray();
    }

    void ContinuePainting()
    {
        if (currentGraffiti == null) return;

        currentGraffiti.AddPaint(Time.deltaTime * paintSpeed);

        if (currentGraffiti.IsComplete())
            StopPainting();
    }

    void StopPainting()
    {
        isPainting = false;

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

        if (sprayParticles != null)
            sprayParticles.Stop();

        if (sprayCan != null)
            sprayCan.StopSpray();

        SetGraffitiToolVisible(IsGraffitiModeActive());
    }

    void UpdateUIFade()
    {
        if (paintText == null) return;

        Color c = paintText.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * uiFadeSpeed);
        paintText.color = c;

        Vector3 targetScale = targetAlpha > 0.1f ? visibleScale : hiddenScale;

        paintText.transform.localScale = Vector3.Lerp(
            paintText.transform.localScale,
            targetScale,
            Time.deltaTime * popSpeed
        );
    }

    public void SetFreePaintColor(Color newColor)
    {
        currentSprayColor = newColor;
        SyncHSVFromCurrentColor();
        Debug.Log("Color de pintado libre cambiado a: " + newColor);
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
        SetNozzleProfile(
            "Personalizada",
            minSize,
            maxSize,
            paintRate,
            0.22f,
            0.58f,
            0.12f,
            currentNozzleShape,
            brushTexture
        );
    }

    public void SetNozzleProfile(
        string nozzleName,
        float minSize,
        float maxSize,
        float paintRate,
        float spacingMultiplier,
        float opacity,
        float jitter,
        GraffitiNozzleShape shape,
        Texture2D texture
    )
    {
        currentNozzleName = string.IsNullOrEmpty(nozzleName) ? "Boquilla" : nozzleName;
        brushMinSize = Mathf.Max(0.001f, Mathf.Min(minSize, maxSize));
        brushMaxSize = Mathf.Max(brushMinSize, Mathf.Max(minSize, maxSize));
        freePaintRate = Mathf.Max(0.001f, paintRate);
        brushSpacingMultiplier = Mathf.Clamp(spacingMultiplier, 0.05f, 0.7f);
        brushOpacity = Mathf.Clamp01(opacity);
        brushJitter = Mathf.Clamp01(jitter);
        currentNozzleShape = shape;

        if (texture != null)
            brushTexture = texture;

        Debug.Log("Boquilla cambiada: " + currentNozzleName + " " + brushMinSize + " - " + brushMaxSize);
    }

    public float GetBrushSize()
    {
        return Random.Range(brushMinSize, brushMaxSize);
    }

    public float GetBrushSpacing(float size)
    {
        return Mathf.Max(0.0035f, size * brushSpacingMultiplier);
    }

    public Color GetPaintColorWithVariation()
    {
        Color.RGBToHSV(currentSprayColor, out float hue, out float saturation, out float value);
        value = Mathf.Clamp01(value + Random.Range(-brushColorJitter, brushColorJitter));

        Color color = Color.HSVToRGB(hue, saturation, value);
        color.a = Mathf.Clamp01(brushOpacity * Random.Range(0.82f, 1f));
        return color;
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

        if (sprayParticles != null)
            sprayParticles.Stop();

        if (sprayCan != null)
            sprayCan.StopSpray();

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

    void SetUIGroupVisible(GameObject[] objects, bool visible)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.activeSelf != visible)
                obj.SetActive(visible);
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
}
