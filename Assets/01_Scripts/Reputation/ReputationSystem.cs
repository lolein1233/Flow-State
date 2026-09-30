using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ReputationSystem : MonoBehaviour
{
    const string RecognitionKey = "FlowState_Recognition";
    const string CrewRespectKey = "FlowState_CrewRespect";
    const string CityImpactKey = "FlowState_CityImpact";
    const string PrefabResourcePath = "UI/StreetReputationMenu";

    static ReputationSystem instance;

    [Header("Valores iniciales de desarrollo")]
    [SerializeField] ReputationData initialValues = new ReputationData(35f, 20f, 45f);

    readonly ReputationData currentValues = new ReputationData();

    public static event Action OnReputationChanged;

    public static ReputationSystem Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public static ReputationData Data => Instance.currentValues;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        OnReputationChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance();
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        instance = FindFirstObjectByType<ReputationSystem>(FindObjectsInactive.Include);
        if (instance != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab != null)
        {
            Instantiate(prefab);
            return;
        }

        GameObject fallback = new GameObject("Street Reputation System");
        fallback.AddComponent<ReputationSystem>();
        Debug.LogWarning("Street Reputation System: no se encontró el prefab de UI; el sistema de datos seguirá funcionando.");
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public static void AddRecognition(float amount)
    {
        Instance.ChangeValues(Mathf.Max(0f, amount), 0f, 0f);
    }

    public static void RemoveRecognition(float amount)
    {
        Instance.ChangeValues(-Mathf.Max(0f, amount), 0f, 0f);
    }

    public static void AddCrewRespect(float amount)
    {
        Instance.ChangeValues(0f, Mathf.Max(0f, amount), 0f);
    }

    public static void RemoveCrewRespect(float amount)
    {
        Instance.ChangeValues(0f, -Mathf.Max(0f, amount), 0f);
    }

    public static void AddCityImpact(float amount)
    {
        Instance.ChangeValues(0f, 0f, Mathf.Max(0f, amount));
    }

    public static void RemoveCityImpact(float amount)
    {
        Instance.ChangeValues(0f, 0f, -Mathf.Max(0f, amount));
    }

    public static float GetRecognition() => Data.Recognition;
    public static float GetCrewRespect() => Data.CrewRespect;
    public static float GetCityImpact() => Data.CityImpact;
    public static float GetOverallReputation() => Data.OverallReputation;

    public static void Save()
    {
        ReputationSystem system = Instance;
        PlayerPrefs.SetFloat(RecognitionKey, system.currentValues.Recognition);
        PlayerPrefs.SetFloat(CrewRespectKey, system.currentValues.CrewRespect);
        PlayerPrefs.SetFloat(CityImpactKey, system.currentValues.CityImpact);
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        ReputationSystem system = Instance;
        ReputationData defaults = system.initialValues ?? new ReputationData();

        system.currentValues.Set(
            PlayerPrefs.GetFloat(RecognitionKey, defaults.Recognition),
            PlayerPrefs.GetFloat(CrewRespectKey, defaults.CrewRespect),
            PlayerPrefs.GetFloat(CityImpactKey, defaults.CityImpact));

        OnReputationChanged?.Invoke();
    }

    public static void ResetReputation()
    {
        ReputationSystem system = Instance;
        system.currentValues.Set(0f, 0f, 0f);
        Save();
        OnReputationChanged?.Invoke();
    }

    void ChangeValues(float recognitionDelta, float crewRespectDelta, float cityImpactDelta)
    {
        float previousRecognition = currentValues.Recognition;
        float previousCrewRespect = currentValues.CrewRespect;
        float previousCityImpact = currentValues.CityImpact;

        currentValues.Set(
            previousRecognition + recognitionDelta,
            previousCrewRespect + crewRespectDelta,
            previousCityImpact + cityImpactDelta);

        if (Mathf.Approximately(previousRecognition, currentValues.Recognition)
            && Mathf.Approximately(previousCrewRespect, currentValues.CrewRespect)
            && Mathf.Approximately(previousCityImpact, currentValues.CityImpact))
            return;

        Save();
        OnReputationChanged?.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Add 10 Recognition")]
    void DebugAddRecognition() => AddRecognition(10f);

    [ContextMenu("Debug/Add 10 Crew Respect")]
    void DebugAddCrewRespect() => AddCrewRespect(10f);

    [ContextMenu("Debug/Add 10 City Impact")]
    void DebugAddCityImpact() => AddCityImpact(10f);

    [ContextMenu("Debug/Reset Reputation")]
    void DebugResetReputation() => ResetReputation();
#endif
}
