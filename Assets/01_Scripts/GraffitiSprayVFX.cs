using UnityEngine;

/// <summary>
/// Nube visual del aerosol. El pintado real sigue siendo responsabilidad de
/// GraffitiPainter; este componente representa presion, cono, boquilla y color.
/// </summary>
[DisallowMultipleComponent]
public sealed class GraffitiSprayVFX : MonoBehaviour
{
    const string ShaderName = "FLOWSTATE/Graffiti/Cartoon Aerosol Particle";
    const float WallClearance = 0.035f;

    ParticleSystem pigment;
    ParticleSystem mist;
    ParticleSystem droplets;
    ParticleSystem valveBloom;
    Transform effectRoot;
    Material pigmentMaterial;
    Material mistMaterial;
    Material dropletMaterial;
    Material bloomMaterial;
    bool configured;
    bool emitting;
    bool profileReady;
    bool pendingStartBurst;
    bool ownsRoot;
    float nextValvePulseTime;
    Color currentColor = Color.clear;
    GraffitiNozzleShape currentNozzle = (GraffitiNozzleShape)(-1);
    float currentCone = -1f;
    float currentDistance = -1f;
    float currentPressure = -1f;

    public ParticleSystem PrimarySystem => pigment;
    public bool IsConfigured => configured;
    public int SystemCount => configured ? 4 : 0;
    public Transform EffectRoot => effectRoot;

    public ParticleSystem Bind(ParticleSystem existingPrimary)
    {
        if (configured && pigment != null)
            return pigment;

        pigment = existingPrimary;
        if (pigment == null)
        {
            GameObject root = new GameObject("Graffiti Aerosol VFX");
            pigment = root.AddComponent<ParticleSystem>();
            ownsRoot = true;
        }

        effectRoot = pigment.transform;
        effectRoot.name = "Graffiti Aerosol VFX";
        // The authored legacy system was scaled to 0.22218. Particle sizes are
        // specified in metres now, so the presenter must have a neutral frame.
        effectRoot.SetParent(null, true);
        effectRoot.localScale = Vector3.one;
        pigment.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        mist = FindOrCreateLayer("Air Cloud");
        droplets = FindOrCreateLayer("Ink Microdroplets");
        valveBloom = FindOrCreateLayer("Valve Comic Pop");
        CreateMaterials();
        ConfigureSystems();
        configured = true;
        SetEmission(false);
        return pigment;
    }

    public void SetVisible(bool visible)
    {
        if (!configured || effectRoot == null)
            return;

        if (!visible)
        {
            SetEmission(false);
            StopAndClear(pigment);
            StopAndClear(mist);
            StopAndClear(droplets);
            StopAndClear(valveBloom);
        }

        if (effectRoot.gameObject.activeSelf != visible)
            effectRoot.gameObject.SetActive(visible);
    }

    public void UpdatePresentation(Ray sprayRay, float targetDistance, float cone01, Color sprayColor,
        GraffitiNozzleShape nozzle, float pressure, Vector3 nozzleUp = default)
    {
        if (!configured || effectRoot == null)
            return;

        Vector3 direction = sprayRay.direction.sqrMagnitude > 0.0001f
            ? sprayRay.direction.normalized
            : transform.forward;
        Vector3 up = Vector3.ProjectOnPlane(nozzleUp, direction);
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.ProjectOnPlane(Vector3.up, direction);
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.ProjectOnPlane(Vector3.forward, direction);
        effectRoot.SetPositionAndRotation(sprayRay.origin, Quaternion.LookRotation(direction, up.normalized));

        targetDistance = Mathf.Clamp(targetDistance, 0.16f, 3.5f);
        cone01 = Mathf.Clamp01(cone01);
        pressure = Mathf.Clamp01(pressure);
        sprayColor.a = 1f;

        bool profileChanged = nozzle != currentNozzle ||
            Mathf.Abs(cone01 - currentCone) > 0.012f ||
            Mathf.Abs(targetDistance - currentDistance) > 0.018f ||
            Mathf.Abs(pressure - currentPressure) > 0.015f ||
            !Approximately(sprayColor, currentColor);
        if (profileChanged)
        {
            currentNozzle = nozzle;
            currentCone = cone01;
            currentDistance = targetDistance;
            currentPressure = pressure;
            currentColor = sprayColor;
            ApplyDynamicProfile();
            profileReady = true;

            if (pendingStartBurst)
            {
                pendingStartBurst = false;
                EmitStartBurst();
            }
        }

        TryEmitValvePulse();
    }

    public void SetEmission(bool active)
    {
        if (!configured || emitting == active)
            return;

        emitting = active;
        SetSystemEmission(pigment, active);
        SetSystemEmission(mist, active);
        SetSystemEmission(droplets, active);
        SetSystemEmission(valveBloom, active);
        if (active)
        {
            if (profileReady)
                EmitStartBurst();
            else
                pendingStartBurst = true;
            nextValvePulseTime = Time.unscaledTime + 0.075f;
        }
        else
        {
            pendingStartBurst = false;
        }
    }

    public static Vector3 GetNozzleVisualProfile(GraffitiNozzleShape nozzle)
    {
        // x = apertura, y = tamano de puff, z = densidad relativa.
        switch (nozzle)
        {
            case GraffitiNozzleShape.Needle: return new Vector3(0.52f, 0.62f, 0.74f);
            case GraffitiNozzleShape.FatCap: return new Vector3(1.35f, 1.34f, 1.16f);
            case GraffitiNozzleShape.Chisel: return new Vector3(0.86f, 0.82f, 0.94f);
            case GraffitiNozzleShape.Splatter: return new Vector3(1.18f, 1.08f, 0.66f);
            default: return Vector3.one;
        }
    }

    public static Vector2 GetNozzleVisualAspect(GraffitiNozzleShape nozzle)
    {
        switch (nozzle)
        {
            case GraffitiNozzleShape.Needle: return new Vector2(0.7f, 1.05f);
            case GraffitiNozzleShape.FatCap: return new Vector2(1.14f, 1.14f);
            case GraffitiNozzleShape.Chisel: return new Vector2(1.72f, 0.52f);
            case GraffitiNozzleShape.Splatter: return new Vector2(1.2f, 0.92f);
            default: return Vector2.one;
        }
    }

    ParticleSystem FindOrCreateLayer(string layerName)
    {
        Transform child = effectRoot.Find(layerName);
        if (child != null && child.TryGetComponent(out ParticleSystem system))
            return system;

        GameObject layer = new GameObject(layerName);
        layer.transform.SetParent(effectRoot, false);
        layer.transform.localPosition = Vector3.zero;
        layer.transform.localRotation = Quaternion.identity;
        layer.transform.localScale = Vector3.one;
        ParticleSystem created = layer.AddComponent<ParticleSystem>();
        created.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return created;
    }

    void CreateMaterials()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[GraffitiSprayVFX] No se encontro el shader {ShaderName}.", this);
            return;
        }

        pigmentMaterial = CreateMaterial(shader, "Printed Aerosol - Pigment", 0f, 1.25f, 0.08f, 0.06f, 0.026f, 0.025f, 0.17f);
        mistMaterial = CreateMaterial(shader, "Printed Aerosol - Air", 1f, 0.8f, 0.27f, 0.22f, 0.038f, 0.035f, 1.31f);
        dropletMaterial = CreateMaterial(shader, "Printed Aerosol - Droplets", 2f, 1.05f, 0.035f, 0.03f, 0.018f, 0f, 2.73f);
        bloomMaterial = CreateMaterial(shader, "Printed Aerosol - Valve Pop", 3f, 1.4f, 0.11f, 0.1f, 0.055f, 0.075f, 4.19f);
    }

    static Material CreateMaterial(Shader shader, string materialName, float style, float inkWidthPx,
        float softness, float breakup, float registration, float paperFlecks, float phase)
    {
        Material material = new Material(shader)
        {
            name = materialName,
            hideFlags = HideFlags.HideAndDontSave
        };
        material.SetFloat("_Style", style);
        material.SetFloat("_InkWidthPx", inkWidthPx);
        material.SetFloat("_Softness", softness);
        material.SetFloat("_Breakup", breakup);
        material.SetFloat("_Registration", registration);
        material.SetFloat("_PaperFlecks", paperFlecks);
        material.SetFloat("_AnimationPhase", phase);
        return material;
    }

    void ConfigureSystems()
    {
        ConfigureBase(pigment, 220, 420f, 0.006f, 0.016f, 0.015f);
        ConfigureBase(mist, 110, 115f, 0.038f, 0.105f, 0.035f);
        ConfigureBase(droplets, 60, 36f, 0.0045f, 0.014f, 0.16f);
        ConfigureBase(valveBloom, 12, 0f, 0.04f, 0.072f, 0f);

        ConfigureRenderer(pigment, pigmentMaterial, 2);
        ConfigureRenderer(mist, mistMaterial, 0);
        ConfigureRenderer(droplets, dropletMaterial, 3);
        ConfigureRenderer(valveBloom, bloomMaterial, 4);

        ConfigureNoise(pigment, 0.065f, 2.1f, 0.72f);
        ConfigureNoise(mist, 0.19f, 1.25f, 0.42f);
        ConfigureNoise(droplets, 0.14f, 2.7f, 0.88f);
        ConfigureNoise(valveBloom, 0.035f, 3.2f, 1.2f);

        ConfigureColorOverLifetime(pigment, 1f, 1f, 0.92f, 0f);
        ConfigureColorOverLifetime(mist, 0.9f, 0.78f, 0.38f, 0f);
        ConfigureColorOverLifetime(droplets, 1f, 0.96f, 0.72f, 0f);
        ConfigureColorOverLifetime(valveBloom, 1f, 0.86f, 0.34f, 0f);

        ConfigureSizeOverLifetime(pigment, 0.68f, 1f, 0.9f, 0.2f);
        ConfigureSizeOverLifetime(mist, 0.34f, 0.92f, 1.16f, 0.08f);
        ConfigureSizeOverLifetime(droplets, 0.72f, 1f, 0.78f, 0.15f);
        ConfigureSizeOverLifetime(valveBloom, 0.42f, 1.15f, 1.28f, 0.12f);
    }

    static void ConfigureBase(ParticleSystem system, int capacity, float rate, float minimumSize,
        float maximumSize, float gravity)
    {
        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.prewarm = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = capacity;
        main.startSpeed = new ParticleSystem.MinMaxCurve(6.5f, 8.5f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(minimumSize, maximumSize);
        main.startSizeY = new ParticleSystem.MinMaxCurve(minimumSize, maximumSize);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(minimumSize, maximumSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.gravityModifier = gravity;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = 0.002f;
        shape.radiusThickness = 1f;
        shape.length = 0.006f;
        shape.position = Vector3.forward * 0.0025f;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static void ConfigureRenderer(ParticleSystem system, Material material, int sortingOrder)
    {
        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
        renderer.sortingOrder = sortingOrder;
        renderer.maxParticleSize = 0.22f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        if (material != null)
            renderer.sharedMaterial = material;
    }

    static void ConfigureNoise(ParticleSystem system, float strength, float frequency, float scroll)
    {
        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = new ParticleSystem.MinMaxCurve(strength * 0.62f, strength);
        noise.frequency = frequency;
        noise.scrollSpeed = scroll;
        noise.damping = true;
        noise.octaveCount = 2;
        noise.octaveMultiplier = 0.55f;
    }

    static void ConfigureColorOverLifetime(ParticleSystem system, float start, float early, float late, float end)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(start, 0f),
                new GradientAlphaKey(early, 0.12f),
                new GradientAlphaKey(late, 0.76f),
                new GradientAlphaKey(end, 1f)
            });
        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }

    static void ConfigureSizeOverLifetime(ParticleSystem system, float start, float early, float late, float end)
    {
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, start),
            new Keyframe(0.2f, early),
            new Keyframe(0.76f, late),
            new Keyframe(1f, end));
        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    void ApplyDynamicProfile()
    {
        Vector3 nozzle = GetNozzleVisualProfile(currentNozzle);
        Vector2 aspect = GetNozzleVisualAspect(currentNozzle);
        float distance01 = Mathf.InverseLerp(0.2f, 3.2f, currentDistance);
        float speed = Mathf.Lerp(6.8f, 10.6f, distance01);
        float travel = Mathf.Max(0.045f, currentDistance - WallClearance);
        float coneAngle = Mathf.Lerp(1.4f, 9.8f, currentCone) * nozzle.x;
        float sizeScale = Mathf.Lerp(0.72f, 1.3f, currentCone) * nozzle.y;
        float density = Mathf.Lerp(0.38f, 1f, currentPressure) * nozzle.z;
        float pressureAlpha = Mathf.Lerp(0.62f, 1f, currentPressure);

        ApplyLayerProfile(pigment, speed, travel, coneAngle * 0.82f, aspect, 420f * density,
            0.006f * sizeScale, 0.016f * sizeScale, WithAlpha(currentColor, 0.98f * pressureAlpha));
        ApplyLayerProfile(mist, speed * 0.68f, travel, coneAngle,
            Vector2.Lerp(Vector2.one, aspect, 0.48f), 115f * density,
            0.038f * sizeScale, 0.105f * sizeScale, WithAlpha(currentColor, 0.22f * pressureAlpha));
        ApplyLayerProfile(droplets, speed * 1.12f, travel, coneAngle * 1.2f,
            Vector2.Lerp(Vector2.one, aspect, 0.68f), 36f * density,
            0.0045f * sizeScale, 0.014f * sizeScale, WithAlpha(currentColor, 0.9f * pressureAlpha));
        ApplyBloomProfile(aspect, coneAngle, sizeScale, pressureAlpha);

        ParticleSystem.NoiseModule dropletNoise = droplets.noise;
        dropletNoise.strength = currentNozzle == GraffitiNozzleShape.Splatter
            ? new ParticleSystem.MinMaxCurve(0.24f, 0.46f)
            : new ParticleSystem.MinMaxCurve(0.08f, 0.17f);

        SetMaterialEnergy(pigmentMaterial, currentPressure, currentCone);
        SetMaterialEnergy(mistMaterial, currentPressure, currentCone);
        SetMaterialEnergy(dropletMaterial, currentPressure, currentCone);
        SetMaterialEnergy(bloomMaterial, currentPressure, currentCone);
    }

    static void ApplyLayerProfile(ParticleSystem system, float speed, float travelDistance, float angle,
        Vector2 aspect, float rate, float minimumSize, float maximumSize, Color color)
    {
        float maximumSpeed = speed * 1.08f;
        float lifetime = Mathf.Clamp(travelDistance / Mathf.Max(0.1f, maximumSpeed), 0.045f, 0.46f);
        ParticleSystem.MainModule main = system.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.88f, maximumSpeed);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.88f, lifetime);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(minimumSize * aspect.x, maximumSize * aspect.x);
        main.startSizeY = new ParticleSystem.MinMaxCurve(minimumSize * aspect.y, maximumSize * aspect.y);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(minimumSize, maximumSize);
        main.startColor = new ParticleSystem.MinMaxGradient(color);

        ParticleSystem.ShapeModule shape = system.shape;
        shape.angle = Mathf.Clamp(angle, 0.25f, 18f);
        shape.radius = Mathf.Lerp(0.0012f, 0.0055f, Mathf.InverseLerp(1.4f, 14f, angle));
        shape.scale = new Vector3(aspect.x, aspect.y, 1f);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = Mathf.Max(0f, rate);
    }

    void ApplyBloomProfile(Vector2 aspect, float coneAngle, float sizeScale, float pressureAlpha)
    {
        Vector2 bloomAspect = Vector2.Lerp(Vector2.one, aspect, 0.72f);
        ParticleSystem.MainModule main = valveBloom.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.035f, 0.14f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.075f, 0.12f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.038f * sizeScale * bloomAspect.x, 0.072f * sizeScale * bloomAspect.x);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.038f * sizeScale * bloomAspect.y, 0.072f * sizeScale * bloomAspect.y);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(0.038f * sizeScale, 0.072f * sizeScale);
        main.startColor = new ParticleSystem.MinMaxGradient(WithAlpha(currentColor, 0.9f * pressureAlpha));

        ParticleSystem.ShapeModule shape = valveBloom.shape;
        shape.angle = Mathf.Clamp(coneAngle * 0.72f, 0.3f, 11f);
        shape.radius = 0.0012f;
        shape.scale = new Vector3(bloomAspect.x, bloomAspect.y, 1f);
    }

    static void SetMaterialEnergy(Material material, float pressure, float cone)
    {
        if (material == null)
            return;
        material.SetFloat("_SprayEnergy", Mathf.Clamp01(pressure));
        material.SetFloat("_Cone", Mathf.Clamp01(cone));
    }

    void EmitStartBurst()
    {
        if (!profileReady)
            return;
        valveBloom.Emit(2);
        pigment.Emit(20);
        mist.Emit(8);
        droplets.Emit(currentNozzle == GraffitiNozzleShape.Splatter ? 7 : 4);
    }

    void TryEmitValvePulse()
    {
        if (!emitting || !profileReady || Time.unscaledTime < nextValvePulseTime)
            return;

        valveBloom.Emit(1);
        nextValvePulseTime = Time.unscaledTime + Mathf.Lerp(0.095f, 0.068f, currentPressure);
    }

    static void SetSystemEmission(ParticleSystem system, bool active)
    {
        if (system == null)
            return;

        if (active)
        {
            if (!system.isPlaying)
                system.Play(false);
        }
        else if (system.isPlaying || system.particleCount > 0)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    static void StopAndClear(ParticleSystem system)
    {
        if (system != null)
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    static bool Approximately(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f &&
            Mathf.Abs(a.g - b.g) < 0.01f &&
            Mathf.Abs(a.b - b.b) < 0.01f;
    }

    void OnDestroy()
    {
        DestroyRuntimeMaterial(pigmentMaterial);
        DestroyRuntimeMaterial(mistMaterial);
        DestroyRuntimeMaterial(dropletMaterial);
        DestroyRuntimeMaterial(bloomMaterial);

        if (ownsRoot && effectRoot != null)
        {
            if (Application.isPlaying) Destroy(effectRoot.gameObject);
            else DestroyImmediate(effectRoot.gameObject);
        }
    }

    static void DestroyRuntimeMaterial(Material material)
    {
        if (material == null)
            return;
        if (Application.isPlaying) Destroy(material);
        else DestroyImmediate(material);
    }
}
