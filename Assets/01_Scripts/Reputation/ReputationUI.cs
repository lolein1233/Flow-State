using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ReputationUI : MonoBehaviour
{
    static ReputationUI instance;

    [Header("Presentación")]
    [SerializeField] CanvasGroup menuGroup;
    [SerializeField] RectTransform animatedRoot;
    [SerializeField, Min(0.01f)] float transitionDuration = 0.18f;
    [SerializeField] bool startClosed = true;

    [Header("Valores")]
    [SerializeField] TMP_Text overallValue;
    [SerializeField] TMP_Text recognitionValue;
    [SerializeField] TMP_Text crewRespectValue;
    [SerializeField] TMP_Text cityImpactValue;

    [Header("Barras")]
    [SerializeField] Image overallFill;
    [SerializeField] Image recognitionFill;
    [SerializeField] Image crewRespectFill;
    [SerializeField] Image cityImpactFill;

    Coroutine transitionRoutine;

    public static ReputationUI Instance => instance;
    public bool IsOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (menuGroup == null)
            menuGroup = GetComponent<CanvasGroup>();

        if (animatedRoot == null)
            animatedRoot = transform as RectTransform;

        SetVisibleImmediate(!startClosed);
    }

    void OnEnable()
    {
        ReputationSystem.OnReputationChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        ReputationSystem.OnReputationChanged -= Refresh;
    }

    public void Open()
    {
        if (IsOpen && transitionRoutine == null)
            return;

        IsOpen = true;
        PlayTransition(true);
    }

    public void Close()
    {
        if (!IsOpen && transitionRoutine == null)
            return;

        IsOpen = false;
        PlayTransition(false);
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Refresh()
    {
        ReputationData data = ReputationSystem.Data;
        SetValue(overallValue, overallFill, data.OverallReputation);
        SetValue(recognitionValue, recognitionFill, data.Recognition);
        SetValue(crewRespectValue, crewRespectFill, data.CrewRespect);
        SetValue(cityImpactValue, cityImpactFill, data.CityImpact);
    }

    void PlayTransition(bool opening)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionRoutine(opening));
    }

    IEnumerator TransitionRoutine(bool opening)
    {
        if (menuGroup == null)
        {
            transitionRoutine = null;
            yield break;
        }

        menuGroup.blocksRaycasts = opening;
        menuGroup.interactable = opening;

        float startAlpha = menuGroup.alpha;
        float targetAlpha = opening ? 1f : 0f;
        Vector3 startScale = animatedRoot != null ? animatedRoot.localScale : Vector3.one;
        Vector3 targetScale = Vector3.one * (opening ? 1f : 0.94f);
        Quaternion startRotation = animatedRoot != null ? animatedRoot.localRotation : Quaternion.identity;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, opening ? 0f : -1.5f);
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
            menuGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            if (animatedRoot != null)
            {
                animatedRoot.localScale = Vector3.Lerp(startScale, targetScale, t);
                animatedRoot.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            }

            yield return null;
        }

        menuGroup.alpha = targetAlpha;
        if (animatedRoot != null)
        {
            animatedRoot.localScale = targetScale;
            animatedRoot.localRotation = targetRotation;
        }

        transitionRoutine = null;
    }

    void SetVisibleImmediate(bool visible)
    {
        IsOpen = visible;

        if (menuGroup != null)
        {
            menuGroup.alpha = visible ? 1f : 0f;
            menuGroup.interactable = visible;
            menuGroup.blocksRaycasts = visible;
        }

        if (animatedRoot != null)
        {
            animatedRoot.localScale = Vector3.one * (visible ? 1f : 0.94f);
            animatedRoot.localRotation = Quaternion.Euler(0f, 0f, visible ? 0f : -1.5f);
        }
    }

    static void SetValue(TMP_Text valueText, Image fill, float value)
    {
        float clamped = Mathf.Clamp(value, 0f, 100f);

        if (valueText != null)
            valueText.text = Mathf.RoundToInt(clamped).ToString("00");

        if (fill != null)
            fill.fillAmount = clamped / 100f;
    }
}
