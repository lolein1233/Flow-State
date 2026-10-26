using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-75)]
[DisallowMultipleComponent]
public sealed class GraffitiQuickPainter : MonoBehaviour
{
    public FPSController player;
    public GraffitiPainter painter;
    public QuickGraffitiBook book;
    public Material artworkMaterial;
    public QuickGraffitiEntry[] library;
    public GameObject modeHud;
    public TMP_Text modeLabel;
    public GameObject freeModeHint;
    [Header("Tamaño con rueda - Antes de pintar")]
    public Vector2 sizeMultiplierRange = new Vector2(0.4f, 2.5f);
    [Range(0.01f, 0.5f)] public float sizeChangePerStep = 0.12f;
    [SerializeField] float selectedSizeMultiplier = 1f;
    public KeyCode toggleModeKey = KeyCode.E;
    [Min(1)] public int maximumPieces = 128;
    public int selectedIndex;
    GraffitiMenuDrawer drawer;
    QuickGraffitiPiece preview;
    QuickGraffitiEntry previewEntry;
    QuickGraffitiPiece currentPiece;
    int pieceCount;
    int menuClosedFrame = -1;
    int selectedFrame = -1;

    public bool IsQuickMode { get; private set; }
    public bool StrokeActive { get; private set; }
    public bool MenuClosedThisFrame => menuClosedFrame == Time.frameCount;
    public bool BookOpen => book != null && book.IsOpen;
    public QuickGraffitiPiece CurrentPiece => currentPiece;
    public float SelectedSizeMultiplier => selectedSizeMultiplier;
    public QuickGraffitiEntry Selected => library != null && library.Length > 0
        ? library[Mathf.Clamp(selectedIndex, 0, library.Length - 1)] : null;

    void Awake()
    {
        if (player == null) player = GetComponent<FPSController>();
        if (painter == null) painter = GetComponent<GraffitiPainter>();
        drawer = GetComponentInChildren<GraffitiMenuDrawer>(true);
        if (modeHud != null) modeHud.SetActive(false);
        if (freeModeHint != null) freeModeHint.SetActive(false);
    }

    void Update()
    {
        if (PauseMenuController.GameplayInputBlocked) return;
        if (player == null || !player.IsGraffitiMode())
        {
            if (IsQuickMode) SetQuickMode(false);
            return;
        }

        if (Input.GetKeyDown(toggleModeKey) && (painter == null || !painter.menuOpen || BookOpen))
            SetQuickMode(!IsQuickMode);
        else if (IsQuickMode && Input.GetKeyDown(KeyCode.Q))
        {
            if (BookOpen) book.Close(); else OpenBook();
        }
        else if (BookOpen && Input.GetKeyDown(KeyCode.Escape)) book.Close();

        if (BookOpen)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) book.ChangePage(1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) book.ChangePage(-1);
        }
        if (Input.mouseScrollDelta.y != 0f) AdjustSize(Input.mouseScrollDelta.y);
    }

    public bool AdjustSize(float wheelSteps)
    {
        if (wheelSteps == 0f || !IsQuickMode || BookOpen || StrokeActive || player == null || !player.IsGraffitiMode() ||
            (painter != null && (painter.menuOpen || painter.IsPainting))) return false;
        float minimum = Mathf.Max(0.05f, Mathf.Min(sizeMultiplierRange.x, sizeMultiplierRange.y));
        float maximum = Mathf.Max(minimum, Mathf.Max(sizeMultiplierRange.x, sizeMultiplierRange.y));
        selectedSizeMultiplier = Mathf.Clamp(selectedSizeMultiplier * Mathf.Pow(1f + sizeChangePerStep, wheelSteps), minimum, maximum);
        if (preview != null) preview.SetPreviewSize(selectedSizeMultiplier);
        return true;
    }

    public void SetQuickMode(bool quick)
    {
        if (quick && (player == null || !player.IsGraffitiMode() || Selected == null || book == null)) return;
        if (painter != null) painter.CancelPaintingForMenu();
        if (drawer != null && drawer.IsMenuOpen) drawer.CloseMenu();
        if (BookOpen) book.Close();
        IsQuickMode = quick;
        StrokeActive = false;
        if (quick) OpenBook();
        else
        {
            HidePreview();
            if (modeHud != null) modeHud.SetActive(false);
        }
    }

    public void OpenBook()
    {
        if (!IsQuickMode || book == null) return;
        if (painter != null) painter.CancelPaintingForMenu();
        book.Open();
        HidePreview();
    }

    public void NotifyBookClosed()
    {
        menuClosedFrame = Time.frameCount;
        selectedFrame = Time.frameCount;
    }

    public void SelectArtwork(int index)
    {
        if (library == null || index < 0 || index >= library.Length) return;
        selectedIndex = index;
        selectedFrame = Time.frameCount;
        if (BookOpen) book.Close();
    }

    public bool CanStartThisFrame => selectedFrame != Time.frameCount && !BookOpen;

    public static bool CanPlace(RaycastHit hit, QuickGraffitiEntry entry, float sizeMultiplier = 1f)
    {
        if (hit.collider == null || entry == null || entry.sprite == null || Mathf.Abs(hit.normal.y) > 0.65f) return false;
        Quaternion orientation = Quaternion.LookRotation(hit.normal, QuickGraffitiPiece.SurfaceUp(hit.normal));
        Vector2 half = entry.Size * (Mathf.Max(0.01f, sizeMultiplier) * 0.48f);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector3 point = hit.point + orientation * new Vector3(half.x * x, half.y * y, 0f);
                if (!hit.collider.Raycast(new Ray(point + hit.normal * 0.12f, -hit.normal), out RaycastHit support, 0.24f) ||
                    Vector3.Dot(support.normal, hit.normal) < 0.9f) return false;
            }
        return true;
    }

    public bool TryBegin(RaycastHit hit)
    {
        if (!IsQuickMode || !CanStartThisFrame || artworkMaterial == null || !CanPlace(hit, Selected, selectedSizeMultiplier)) return false;
        bool resume = currentPiece != null && !currentPiece.IsComplete && currentPiece.Entry == Selected &&
            Mathf.Approximately(currentPiece.SizeMultiplier, selectedSizeMultiplier) && currentPiece.Surface == hit.collider &&
            Vector3.Distance(currentPiece.transform.position, hit.point) < currentPiece.Size.x * 0.6f;
        if (!resume)
        {
            if (pieceCount >= maximumPieces) return false;
            var obj = new GameObject("Graffiti rápido - " + Selected.title);
            currentPiece = obj.AddComponent<QuickGraffitiPiece>();
            currentPiece.Initialize(Selected, artworkMaterial, hit, sizeMultiplier: selectedSizeMultiplier);
            pieceCount++;
        }
        StrokeActive = true;
        HidePreview();
        return true;
    }

    public bool Advance(float seconds)
    {
        if (!StrokeActive || currentPiece == null) return false;
        currentPiece.Advance(seconds);
        if (painter != null && painter.sprayCan != null)
            painter.sprayCan.SetQuickPaintGesture(QuickGraffitiPiece.SweepUV(currentPiece.Progress) * 2f - Vector2.one, true);
        if (!currentPiece.IsComplete) return false;
        StrokeActive = false;
        ReputationSystem.AddCityImpact(5f);
        return true;
    }

    public void StopStroke()
    {
        StrokeActive = false;
        if (painter != null && painter.sprayCan != null) painter.sprayCan.SetQuickPaintGesture(Vector2.zero, false);
    }

    public Ray StrokeRay()
    {
        Vector3 origin = painter.sprayCan != null ? painter.sprayCan.GetNozzleRay().origin : painter.cam.transform.position;
        return new Ray(origin, (currentPiece.SweepWorldPoint() - origin).normalized);
    }

    public void RefreshPreview()
    {
        if (freeModeHint != null) freeModeHint.SetActive(!IsQuickMode && player != null && player.IsGraffitiMode() &&
            !PauseMenuController.GameplayInputBlocked && (painter == null || !painter.menuOpen));
        bool active = IsQuickMode && player != null && player.IsGraffitiMode() && !PauseMenuController.GameplayInputBlocked;
        if (modeHud != null) modeHud.SetActive(active && !BookOpen);
        if (!active || BookOpen || painter == null || painter.raycastUI == null || Selected == null || artworkMaterial == null)
        { HidePreview(); return; }
        if (StrokeActive)
        {
            HidePreview();
            if (modeLabel != null) modeLabel.text = "GRAFFITI RÁPIDO  /  " + Selected.title.ToUpperInvariant() +
                "  ·  " + Mathf.RoundToInt(currentPiece.Progress * 100f) + "%\nSUELTA EL CLIC PARA PAUSAR";
            return;
        }
        Ray ray = painter.GetSprayRay();
        bool hitWall = Physics.Raycast(ray, out RaycastHit hit, painter.MaximumPaintDistance,
            painter.raycastUI.wallLayer, QueryTriggerInteraction.Ignore);
        bool valid = hitWall && CanPlace(hit, Selected, selectedSizeMultiplier) && pieceCount < maximumPieces;
        if (modeLabel != null) modeLabel.text = "GRAFFITI RÁPIDO  /  " + Selected.title.ToUpperInvariant() +
            "  ·  " + Mathf.RoundToInt(selectedSizeMultiplier * 100f) + "%" +
            "\n[E] LIBRE   ·   [Q] LIBRETA   ·   RUEDA: TAMAÑO   ·   " + (valid ? "MANTÉN CLIC PARA PINTAR" : "BUSCA ESPACIO EN UNA PARED");
        if (!hitWall) { HidePreview(); return; }
        if (preview == null || previewEntry != Selected)
        {
            if (preview != null) Destroy(preview.gameObject);
            previewEntry = Selected;
            preview = new GameObject("Vista previa - Graffiti rápido").AddComponent<QuickGraffitiPiece>();
            preview.Initialize(Selected, artworkMaterial, hit, true, selectedSizeMultiplier);
        }
        preview.gameObject.SetActive(true);
        preview.SetPreviewPose(hit, valid);
    }

    void HidePreview() { if (preview != null) preview.gameObject.SetActive(false); }
    void OnDisable()
    {
        if (BookOpen) book.Close();
        StopStroke();
        HidePreview();
        if (modeHud != null) modeHud.SetActive(false);
        if (freeModeHint != null) freeModeHint.SetActive(false);
    }
    void OnDestroy() { if (preview != null) Destroy(preview.gameObject); }
}
