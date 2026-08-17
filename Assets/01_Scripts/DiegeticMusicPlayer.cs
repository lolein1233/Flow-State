using TMPro;
using UnityEngine;

public class DiegeticMusicPlayer : MonoBehaviour
{
    [Header("Referencias")]
    public FPSController fps;
    public Transform cameraTransform;
    public GameObject deviceRoot;
    public TMP_Text titleText;
    public TMP_Text statusText;
    public TMP_Text bpmText;
    public TMP_Text trackText;
    public TMP_Text padText;
    public TMP_Text hintText;
    public AudioSource audioSource;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.M;
    public KeyCode playKey = KeyCode.P;
    public KeyCode bpmUpKey = KeyCode.UpArrow;
    public KeyCode bpmDownKey = KeyCode.DownArrow;

    [Header("Pose diegetica")]
    public Vector3 shownLocalPosition = new Vector3(-0.32f, -0.025f, 0.72f);
    public Vector3 hiddenLocalPosition = new Vector3(-1.02f, -0.08f, 0.82f);
    public Vector3 shownLocalEuler = new Vector3(-2f, -36f, -1.2f);
    public Vector3 hiddenLocalEuler = new Vector3(3f, -58f, -8f);
    public float shownScale = 0.56f;
    public float hiddenScale = 0.43f;
    public float openSmooth = 7.5f;

    [Header("Zoom al abrir")]
    public bool applyCameraZoom = true;
    public float openFovOffset = -4f;
    public float zoomSharpness = 8f;

    [Header("Maqueta musical")]
    [Range(60, 180)] public int bpm = 92;
    public string[] tracks =
    {
        "LoFi Kick",
        "Neon Snare",
        "Brick Hat",
        "Spray Bass"
    };

    bool isOpen;
    bool isPlaying;
    int selectedTrack;
    float openAmount;
    float currentFovOffset;
    float pulseTimer;
    AudioClip previewClip;

    void Awake()
    {
        if (fps == null)
            fps = GetComponent<FPSController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (deviceRoot != null)
        {
            openAmount = deviceRoot.activeSelf ? 1f : 0f;
            deviceRoot.SetActive(openAmount > 0.001f);
        }

        RefreshWorldSpaceCanvases();

        EnsureAudioSource();
        UpdateTexts();
    }

    void Update()
    {
        bool canUseDevice = fps == null || (!fps.IsGraffitiMode() && !fps.IsClimbing());

        if (!canUseDevice && isOpen)
            SetOpen(false);

        if (canUseDevice && Input.GetKeyDown(toggleKey))
            SetOpen(!isOpen);

        if (isOpen)
            HandleDeviceInput();

        UpdateDevicePose();
        UpdateCameraZoom();
        UpdateAudioPulse();
    }

    void OnDisable()
    {
        ClearCameraZoom();
    }

    void OnDestroy()
    {
        ClearCameraZoom();
    }

    void SetOpen(bool value)
    {
        isOpen = value;

        if (deviceRoot != null && isOpen)
        {
            deviceRoot.SetActive(true);
            RefreshWorldSpaceCanvases();
        }

        UpdateTexts();
    }

    public void ShowDevice()
    {
        SetOpen(true);
    }

    public void HideDevice()
    {
        SetOpen(false);
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    void HandleDeviceInput()
    {
        if (Input.GetKeyDown(playKey))
        {
            isPlaying = !isPlaying;
            UpdateTexts();
        }

        if (Input.GetKeyDown(bpmUpKey))
        {
            bpm = Mathf.Clamp(bpm + 2, 60, 180);
            UpdateTexts();
        }

        if (Input.GetKeyDown(bpmDownKey))
        {
            bpm = Mathf.Clamp(bpm - 2, 60, 180);
            UpdateTexts();
        }

        for (int i = 0; i < Mathf.Min(4, tracks.Length); i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                selectedTrack = i;
                TriggerPreviewSound();
                UpdateTexts();
            }
        }
    }

    void UpdateDevicePose()
    {
        if (deviceRoot == null)
            return;

        float target = isOpen ? 1f : 0f;
        openAmount = Mathf.MoveTowards(openAmount, target, openSmooth * Time.deltaTime);

        if (openAmount <= 0.001f && !isOpen)
        {
            deviceRoot.SetActive(false);
            return;
        }

        if (!deviceRoot.activeSelf)
        {
            deviceRoot.SetActive(true);
            RefreshWorldSpaceCanvases();
        }

        float eased = openAmount * openAmount * (3f - 2f * openAmount);
        deviceRoot.transform.localPosition = Vector3.Lerp(hiddenLocalPosition, shownLocalPosition, eased);
        deviceRoot.transform.localRotation = Quaternion.Slerp(Quaternion.Euler(hiddenLocalEuler), Quaternion.Euler(shownLocalEuler), eased);

        float pulse = isPlaying ? Mathf.Sin(Time.time * 8f) * 0.018f : 0f;
        float scale = Mathf.Lerp(hiddenScale, shownScale, eased) + pulse;
        deviceRoot.transform.localScale = Vector3.one * scale;
    }

    void UpdateCameraZoom()
    {
        if (!applyCameraZoom || fps == null)
        {
            ClearCameraZoom();
            return;
        }

        float targetOffset = openFovOffset * openAmount;
        float blend = 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime);
        currentFovOffset = Mathf.Lerp(currentFovOffset, targetOffset, blend);

        if (Mathf.Abs(currentFovOffset) < 0.01f && Mathf.Abs(targetOffset) < 0.01f)
            currentFovOffset = 0f;

        fps.SetThirdPersonFovOffset(currentFovOffset);
    }

    void ClearCameraZoom()
    {
        if (fps != null)
            fps.SetThirdPersonFovOffset(0f);

        currentFovOffset = 0f;
    }

    void UpdateAudioPulse()
    {
        if (!isPlaying || audioSource == null)
            return;

        pulseTimer -= Time.deltaTime;

        if (pulseTimer > 0f)
            return;

        pulseTimer = 60f / Mathf.Max(1, bpm);
        TriggerPreviewSound();
    }

    void TriggerPreviewSound()
    {
        EnsureAudioSource();

        if (audioSource == null)
            return;

        if (previewClip == null)
            previewClip = BuildPreviewClip();

        audioSource.pitch = Mathf.Lerp(0.8f, 1.35f, selectedTrack / 3f);
        audioSource.PlayOneShot(previewClip, 0.18f);
    }

    void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        if (deviceRoot != null)
            audioSource = deviceRoot.GetComponent<AudioSource>();

        if (audioSource == null && deviceRoot != null)
            audioSource = deviceRoot.AddComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.1f;
        }
    }

    void RefreshWorldSpaceCanvases()
    {
        if (deviceRoot == null || Camera.main == null)
            return;

        Canvas[] canvases = deviceRoot.GetComponentsInChildren<Canvas>(true);

        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas.worldCamera = Camera.main;
                canvas.enabled = false;
                canvas.enabled = true;
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    AudioClip BuildPreviewClip()
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.RoundToInt(sampleRate * 0.09f);
        float[] samples = new float[sampleCount];
        float frequency = 110f + selectedTrack * 55f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-t * 28f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("MusicDevicePreview", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void UpdateTexts()
    {
        if (titleText != null)
            titleText.text = "FLOWBOX-01";

        if (statusText != null)
            statusText.text = isPlaying ? "PLAYING  /  LIVE LOOP" : "STANDBY  /  SKETCH";

        if (bpmText != null)
            bpmText.text = bpm + " BPM";

        if (trackText != null)
            trackText.text = BuildTrackList();

        if (padText != null)
            padText.text = "1  2  3  4";

        if (hintText != null)
            hintText.text = "[M] guardar  [P] play  [1-4] pads  [UP/DOWN] bpm";
    }

    string BuildTrackList()
    {
        if (tracks == null || tracks.Length == 0)
            return "No tracks";

        string result = "";

        for (int i = 0; i < tracks.Length; i++)
        {
            string prefix = i == selectedTrack ? "> " : "  ";
            result += prefix + tracks[i];

            if (i < tracks.Length - 1)
                result += "\n";
        }

        return result;
    }
}
