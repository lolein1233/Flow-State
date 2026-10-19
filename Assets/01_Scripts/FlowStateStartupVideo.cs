using System.Collections;
using FlowState.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays the branded startup film once per application session, then reveals the main menu.
/// The UI is created at runtime so the menu scene remains untouched.
/// </summary>
[DefaultExecutionOrder(-20000)]
public sealed class FlowStateStartupVideo : MonoBehaviour
{
    private const string MainMenuScenePath = "Assets/02_Escenas/MainMenu_FlowState.unity";
    private const string IntroResourcePath = "Video/FlowStateIntro";
    private const float SkipDelay = 0.75f;
    private const float PrepareTimeout = 10f;
    private const float FadeDuration = 0.45f;

    private static bool hasPlayedThisSession;

    private CanvasGroup canvasGroup;
    private RawImage videoImage;
    private VideoPlayer videoPlayer;
    private AudioSource videoAudio;
    private RenderTexture renderTexture;
    private MenuInput menuInput;
    private bool menuInputWasEnabled;
    private bool previousAudioPause;
    private bool stateRestored;
    private bool prepared;
    private bool finishing;
    private float startedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        hasPlayedThisSession = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (hasPlayedThisSession || SceneManager.GetActiveScene().path != MainMenuScenePath)
            return;

        if (Object.FindFirstObjectByType<FlowStateStartupVideo>(FindObjectsInactive.Include) != null)
            return;

        hasPlayedThisSession = true;
        new GameObject("FLOW STATE · Startup Video").AddComponent<FlowStateStartupVideo>();
    }

    private void Awake()
    {
        menuInput = Object.FindFirstObjectByType<MenuInput>(FindObjectsInactive.Include);
        menuInputWasEnabled = menuInput != null && menuInput.enabled;

        if (menuInputWasEnabled)
            menuInput.enabled = false;

        previousAudioPause = AudioListener.pause;
        AudioListener.pause = true;
        BuildOverlay();
    }

    private void Start()
    {
        VideoClip clip = Resources.Load<VideoClip>(IntroResourcePath);

        if (clip == null)
        {
            Debug.LogError($"FLOW STATE intro video was not found at Resources/{IntroResourcePath}.", this);
            FinishImmediately();
            return;
        }

        int textureWidth = clip.width > 0 ? Mathf.Min((int)clip.width, 1920) : 1920;
        int textureHeight = clip.height > 0 ? Mathf.Min((int)clip.height, 1080) : 1080;
        renderTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32)
        {
            name = "FLOW STATE Intro Render Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();
        videoImage.texture = renderTexture;

        videoAudio = gameObject.AddComponent<AudioSource>();
        videoAudio.playOnAwake = false;
        videoAudio.ignoreListenerPause = true;
        videoAudio.spatialBlend = 0f;

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        if (clip.audioTrackCount > 0)
        {
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, videoAudio);
        }

        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.loopPointReached += HandleCompleted;
        videoPlayer.errorReceived += HandleError;
        startedAt = Time.realtimeSinceStartup;
        videoPlayer.Prepare();
    }

    private void Update()
    {
        if (finishing)
            return;

        float elapsed = Time.realtimeSinceStartup - startedAt;

        if (!prepared && elapsed >= PrepareTimeout)
        {
            Debug.LogError("FLOW STATE intro video preparation timed out; continuing to the main menu.", this);
            BeginFinish();
            return;
        }

        if (prepared && elapsed >= SkipDelay &&
            (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            BeginFinish();
        }
    }

    private void HandlePrepared(VideoPlayer source)
    {
        prepared = true;
        source.Play();
    }

    private void HandleCompleted(VideoPlayer source)
    {
        BeginFinish();
    }

    private void HandleError(VideoPlayer source, string message)
    {
        Debug.LogError($"FLOW STATE intro video could not play: {message}", this);
        BeginFinish();
    }

    private void BeginFinish()
    {
        if (finishing)
            return;

        finishing = true;

        if (videoPlayer != null)
            videoPlayer.Pause();

        StartCoroutine(FadeOutAndFinish());
    }

    private IEnumerator FadeOutAndFinish()
    {
        float elapsed = 0f;

        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeDuration);
            yield return null;
        }

        FinishImmediately();
    }

    private void FinishImmediately()
    {
        RestoreMenuState();
        Destroy(gameObject);
    }

    private void RestoreMenuState()
    {
        if (stateRestored)
            return;

        stateRestored = true;
        AudioListener.pause = previousAudioPause;

        if (menuInput != null && menuInputWasEnabled)
            menuInput.enabled = true;
    }

    private void OnDestroy()
    {
        RestoreMenuState();

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.loopPointReached -= HandleCompleted;
            videoPlayer.errorReceived -= HandleError;
            videoPlayer.Stop();
            videoPlayer.targetTexture = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("Startup Video Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        RectTransform background = CreateFullScreenRect("Black Background", canvasObject.transform);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = Color.black;
        backgroundImage.raycastTarget = true;

        RectTransform videoRect = CreateFullScreenRect("Intro Film", background);
        videoImage = videoRect.gameObject.AddComponent<RawImage>();
        videoImage.color = Color.white;
        videoImage.raycastTarget = true;
    }

    private static RectTransform CreateFullScreenRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }
}
