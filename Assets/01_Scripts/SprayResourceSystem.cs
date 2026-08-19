using UnityEngine;

[DisallowMultipleComponent]
public sealed class SprayResourceSystem : MonoBehaviour
{
    [Header("Pintura")]
    [SerializeField, Range(0f, 100f)] float startingPaint = 100f;
    [SerializeField] float paintConsumptionPerSecond = 4f;
    [SerializeField, Range(0f, 100f)] float criticalPaintLevel = 20f;

    [Header("Mezcla")]
    [SerializeField, Range(0f, 100f)] float startingMixture = 100f;
    [SerializeField] float mixtureDecayWhilePainting = 7.5f;
    [SerializeField] float mixtureDecayWhileIdle = 0.12f;
    [SerializeField, Range(0f, 100f)] float unstableMixtureLevel = 35f;

    [Header("Salida con pocos recursos")]
    [SerializeField, Range(0f, 1f)] float emptyCanOpacity = 0.54f;
    [SerializeField, Range(0f, 1f)] float emptyCanRadius = 0.82f;
    [SerializeField, Range(0f, 1f)] float emptyCanDensity = 0.58f;
    [SerializeField, Range(0f, 1f)] float maximumDropout = 0.48f;

    [Header("Agitado")]
    [SerializeField] KeyCode shakeKey = KeyCode.R;
    [SerializeField, Range(1f, 1.5f)] float shakeDuration = 1.25f;
    [SerializeField, Range(3f, 8f)] float shakeCycles = 5.5f;

    [Header("Audio (opcional)")]
    [SerializeField] AudioClip paintSprayClip;
    [SerializeField] AudioClip airOnlyClip;
    [SerializeField] AudioClip muffledSprayClip;
    [SerializeField] AudioClip rattleClip;
    [SerializeField] bool muteSprayAudio = true;
    [SerializeField, Range(0f, 1f)] float sprayVolume = 0.42f;
    [SerializeField, Range(0f, 1f)] float rattleVolume = 0.55f;

    [Header("Cambio de lata")]
    [SerializeField] KeyCode changeCanKey = KeyCode.F;
    [SerializeField, Range(0.6f, 1f)] float changeCanDuration = 0.82f;
    [SerializeField] bool infiniteCanSupply = true;
    [SerializeField, Min(0)] int startingSpareCans = 3;
    [SerializeField, Range(0f, 100f)] float newCanStartingMixture = 100f;
    [SerializeField] AudioClip discardCanClip;
    [SerializeField] AudioClip drawCanClip;
    [SerializeField, Range(0f, 1f)] float canChangeVolume = 0.65f;

    AudioSource spraySource;
    AudioSource rattleSource;
    float paint;
    float mixture;
    float shakeTimer;
    float canChangeTimer;
    int previousShakeBeat = -1;
    int remainingSpareCans;
    bool spraying;
    bool drawSoundPlayed;
    GraffitiNozzleShape activeNozzle;

    public float Paint => paint;
    public float Mixture => mixture;
    public float Paint01 => paint / 100f;
    public float Mixture01 => mixture / 100f;
    public float CriticalPaintLevel => criticalPaintLevel;
    public float UnstableMixtureLevel => unstableMixtureLevel;
    public KeyCode ShakeKey => shakeKey;
    public KeyCode ChangeCanKey => changeCanKey;
    public bool IsShaking => shakeTimer > 0f;
    public bool IsChangingCan => canChangeTimer > 0f;
    public bool IsSpraying => spraying;
    public bool HasPaint => paint > 0.001f;
    public bool NeedsShake => mixture <= unstableMixtureLevel;
    public float ShakeNormalized => IsShaking ? 1f - Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
    public float CanChangeNormalized => IsChangingCan ? 1f - Mathf.Clamp01(canChangeTimer / changeCanDuration) : 0f;
    public bool HasCanAvailable => infiniteCanSupply || remainingSpareCans > 0;
    public bool CanStartCanChange => !HasPaint && !IsChangingCan && HasCanAvailable;
    public int RemainingSpareCans => remainingSpareCans;
    public bool InfiniteCanSupply => infiniteCanSupply;
    public bool IsSprayAudioMuted => muteSprayAudio;

    public float Instability
    {
        get
        {
            if (mixture >= unstableMixtureLevel)
                return 0f;

            return 1f - Mathf.InverseLerp(0f, unstableMixtureLevel, mixture);
        }
    }

    public float OutputOpacity => Mathf.Lerp(emptyCanOpacity, 1f, Mathf.SmoothStep(0f, 1f, Paint01)) * Mathf.Lerp(0.58f, 1f, 1f - Instability);
    public float OutputRadius => Mathf.Lerp(emptyCanRadius, 1f, Mathf.SmoothStep(0f, 1f, Paint01));
    public float OutputDensity => Mathf.Lerp(emptyCanDensity, 1f, Mathf.SmoothStep(0f, 1f, Paint01));
    public float DropoutChance => maximumDropout * Instability;

    void Awake()
    {
        paint = Mathf.Clamp(startingPaint, 0f, 100f);
        mixture = Mathf.Clamp(startingMixture, 0f, 100f);
        remainingSpareCans = Mathf.Max(0, startingSpareCans);
        EnsureAudioSources();
    }

    void Update()
    {
        if (IsChangingCan)
        {
            UpdateCanChange(Time.deltaTime);
            return;
        }

        if (IsShaking)
        {
            UpdateShake(Time.deltaTime);
            return;
        }

        if (!spraying)
            mixture = Mathf.Max(0f, mixture - mixtureDecayWhileIdle * Time.deltaTime);
    }

    public void BeginSpray(GraffitiNozzleShape nozzle)
    {
        if (IsShaking || IsChangingCan)
            return;

        activeNozzle = nozzle;
        spraying = true;
        RefreshSprayAudio();
    }

    public bool TickSpray(float deltaTime, GraffitiNozzleShape nozzle)
    {
        if (IsShaking || IsChangingCan || deltaTime <= 0f)
            return false;

        activeNozzle = nozzle;
        spraying = true;

        if (HasPaint)
        {
            paint = Mathf.Max(0f, paint - paintConsumptionPerSecond * GetNozzleConsumption(nozzle) * deltaTime);
            mixture = Mathf.Max(0f, mixture - mixtureDecayWhilePainting * deltaTime);
        }

        RefreshSprayAudio();
        return CanEmitAt(Time.time);
    }

    public void EndSpray()
    {
        spraying = false;
        if (spraySource != null)
            spraySource.Stop();
    }

    public bool CanEmitAt(float time)
    {
        if (!HasPaint || IsShaking || IsChangingCan)
            return false;

        float dropout = DropoutChance;
        if (dropout <= 0.001f)
            return true;

        float noise = Mathf.PerlinNoise(time * 9.7f, mixture * 0.071f + 13.2f);
        return noise >= dropout;
    }

    public bool TryStartShake()
    {
        if (IsShaking || IsChangingCan)
            return false;

        EnsureRattleAudio();
        EndSpray();
        shakeTimer = shakeDuration;
        previousShakeBeat = -1;
        return true;
    }

    public bool TryStartCanChange()
    {
        if (HasPaint || IsChangingCan || !HasCanAvailable)
            return false;

        EndSpray();
        shakeTimer = 0f;
        previousShakeBeat = -1;
        canChangeTimer = changeCanDuration;
        drawSoundPlayed = false;
        PlayOptionalOneShot(discardCanClip, canChangeVolume);
        return true;
    }

    public float GetShakeTravel()
    {
        if (!IsShaking)
            return 0f;

        float normalized = ShakeNormalized;
        float envelope = Mathf.Sin(normalized * Mathf.PI);
        return Mathf.Sin(normalized * shakeCycles * Mathf.PI * 2f) * envelope;
    }

    // Punto de integracion para el futuro inventario o cambio de lata.
    public void ReloadPaint(float amount = 100f)
    {
        paint = Mathf.Clamp(amount, 0f, 100f);
        RefreshSprayAudio();
    }

    public void SetMixture(float amount)
    {
        mixture = Mathf.Clamp(amount, 0f, 100f);
        RefreshSprayAudio();
    }

    public void SetPaint(float amount)
    {
        paint = Mathf.Clamp(amount, 0f, 100f);
        RefreshSprayAudio();
    }

    public float GetNozzleConsumption(GraffitiNozzleShape nozzle)
    {
        switch (nozzle)
        {
            case GraffitiNozzleShape.Needle: return 0.65f;
            case GraffitiNozzleShape.FatCap: return 1.55f;
            case GraffitiNozzleShape.Chisel: return 1.25f;
            case GraffitiNozzleShape.Splatter: return 1.4f;
            default: return 1f;
        }
    }

    void UpdateShake(float deltaTime)
    {
        shakeTimer = Mathf.Max(0f, shakeTimer - deltaTime);
        int beat = Mathf.FloorToInt(ShakeNormalized * shakeCycles * 2f);
        if (beat != previousShakeBeat)
        {
            previousShakeBeat = beat;
            if (rattleSource != null && rattleSource.clip != null)
            {
                rattleSource.pitch = Random.Range(0.92f, 1.08f);
                rattleSource.PlayOneShot(rattleSource.clip, rattleVolume);
            }
        }

        if (shakeTimer <= 0f)
        {
            mixture = 100f;
            previousShakeBeat = -1;
        }
    }

    void UpdateCanChange(float deltaTime)
    {
        canChangeTimer = Mathf.Max(0f, canChangeTimer - deltaTime);
        float progress = 1f - Mathf.Clamp01(canChangeTimer / changeCanDuration);
        if (!drawSoundPlayed && progress >= 0.48f)
        {
            drawSoundPlayed = true;
            PlayOptionalOneShot(drawCanClip, canChangeVolume);
        }

        if (canChangeTimer > 0f)
            return;

        // TODO: connect this counter to the future inventory/equipment system.
        if (!infiniteCanSupply)
            remainingSpareCans = Mathf.Max(0, remainingSpareCans - 1);

        paint = 100f;
        mixture = Mathf.Clamp(newCanStartingMixture, 0f, 100f);
        drawSoundPlayed = false;
        RefreshSprayAudio();
    }

    void EnsureAudioSources()
    {
        spraySource = gameObject.AddComponent<AudioSource>();
        spraySource.playOnAwake = false;
        spraySource.loop = true;
        spraySource.spatialBlend = 0f;
        spraySource.volume = muteSprayAudio ? 0f : sprayVolume;

        rattleSource = gameObject.AddComponent<AudioSource>();
        rattleSource.playOnAwake = false;
        rattleSource.loop = false;
        rattleSource.spatialBlend = 0f;

        if (paintSprayClip == null)
            paintSprayClip = CreateNoiseClip("Spray Paint - Procedural", 1.2f, 0.86f, 0.12f);
        if (airOnlyClip == null)
            airOnlyClip = CreateNoiseClip("Spray Air - Procedural", 1.2f, 0.55f, 0.04f);
        if (muffledSprayClip == null)
            muffledSprayClip = CreateNoiseClip("Spray Muffled - Procedural", 1.2f, 0.28f, 0.18f);
        EnsureRattleAudio();
    }

    void RefreshSprayAudio()
    {
        if (spraySource == null || !spraying)
            return;

        if (muteSprayAudio)
        {
            spraySource.volume = 0f;
            if (spraySource.isPlaying)
                spraySource.Stop();
            return;
        }

        AudioClip desired = !HasPaint ? airOnlyClip : NeedsShake ? muffledSprayClip : paintSprayClip;
        if (spraySource.clip != desired)
        {
            spraySource.clip = desired;
            spraySource.Play();
        }
        else if (!spraySource.isPlaying)
        {
            spraySource.Play();
        }

        spraySource.volume = !HasPaint ? sprayVolume * 0.72f : sprayVolume;
        spraySource.pitch = Mathf.Lerp(0.86f, 1.04f, Mixture01) * Mathf.Lerp(0.96f, 1.03f, (int)activeNozzle / 4f);
    }

    void PlayOptionalOneShot(AudioClip clip, float volume)
    {
        if (rattleSource != null && clip != null)
            rattleSource.PlayOneShot(clip, volume);
    }

    void EnsureRattleAudio()
    {
        if (rattleSource == null)
        {
            rattleSource = gameObject.AddComponent<AudioSource>();
            rattleSource.playOnAwake = false;
            rattleSource.loop = false;
            rattleSource.spatialBlend = 0f;
        }

        if (rattleClip == null)
            rattleClip = CreateRattleClip();

        rattleSource.clip = rattleClip;
    }

    static AudioClip CreateNoiseClip(string clipName, float seconds, float smoothing, float lowPulse)
    {
        const int sampleRate = 22050;
        int count = Mathf.CeilToInt(sampleRate * seconds);
        float[] data = new float[count];
        float filtered = 0f;

        for (int i = 0; i < count; i++)
        {
            float noise = Random.Range(-1f, 1f);
            filtered = Mathf.Lerp(noise, filtered, smoothing);
            float pulse = Mathf.Sin(i / (float)sampleRate * Mathf.PI * 2f * 34f) * lowPulse;
            data[i] = Mathf.Clamp(filtered * 0.48f + pulse, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, count, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateRattleClip()
    {
        const int sampleRate = 22050;
        int count = Mathf.CeilToInt(sampleRate * 0.12f);
        float[] data = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float envelope = Mathf.Exp(-t * 11f);
            phase += Mathf.Lerp(0.38f, 0.16f, t);
            data[i] = (Mathf.Sin(phase) * 0.62f + Random.Range(-0.12f, 0.12f)) * envelope;
        }

        AudioClip clip = AudioClip.Create("Spray Rattle - Procedural", count, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
