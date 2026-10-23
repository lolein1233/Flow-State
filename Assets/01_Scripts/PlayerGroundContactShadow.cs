using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class PlayerGroundContactShadow : MonoBehaviour
{
    const string ContactShaderName = "FLOWSTATE/FSSRS/Player Ground Contact";
    static readonly int OpacityId = Shader.PropertyToID("_Opacity");

    [Header("Forma")]
    public Vector2 footprintSize = new Vector2(0.62f, 0.92f);
    public float surfaceOffset = 0.012f;
    public float probeLift = 0.42f;
    public float probeDistance = 1.15f;
    public float fullContactHeight = 0.28f;
    public float fadeContactHeight = 0.62f;

    [Header("Respuesta")]
    public float appearSpeed = 24f;
    public float disappearSpeed = 18f;
    [Range(0f, 1f)] public float minimumGroundNormal = 0.5f;
    public LayerMask collisionMask = ~0;

    CharacterController controller;
    Animator animator;
    Transform visualRoot;
    Transform leftFoot;
    Transform rightFoot;
    Material contactMaterial;
    Mesh contactMesh;
    FootContact leftContact;
    FootContact rightContact;
    readonly RaycastHit[] groundHitBuffer = new RaycastHit[12];
    bool grounded;
    bool isBuilding;

    sealed class FootContact
    {
        public Transform bone;
        public Transform visual;
        public MeshRenderer renderer;
        public MaterialPropertyBlock properties;
        public float opacity;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>(true);
        visualRoot = animator != null ? animator.transform : transform;
        EnsureBuilt();
    }

    public void Configure(CharacterController sourceController, Animator sourceAnimator, Transform sourceVisualRoot, LayerMask groundMask)
    {
        controller = sourceController != null ? sourceController : GetComponent<CharacterController>();
        animator = sourceAnimator != null ? sourceAnimator : GetComponentInChildren<Animator>(true);
        visualRoot = sourceVisualRoot != null ? sourceVisualRoot : (animator != null ? animator.transform : transform);
        collisionMask = groundMask;
        ResolveFeet();
        EnsureBuilt();
    }

    public void SetGrounded(bool value)
    {
        grounded = value;
    }

    void LateUpdate()
    {
        EnsureBuilt();

        bool visualVisible = visualRoot == null || visualRoot.gameObject.activeInHierarchy;
        bool mayShow = grounded && visualVisible && enabled;
        UpdateFoot(leftContact, mayShow);
        UpdateFoot(rightContact, mayShow);
    }

    void OnDisable()
    {
        SetImmediateOpacity(leftContact, 0f);
        SetImmediateOpacity(rightContact, 0f);
    }

    void OnDestroy()
    {
        DestroyRuntimeObject(leftContact != null && leftContact.visual != null ? leftContact.visual.gameObject : null);
        DestroyRuntimeObject(rightContact != null && rightContact.visual != null ? rightContact.visual.gameObject : null);
        DestroyRuntimeObject(contactMaterial);
        DestroyRuntimeObject(contactMesh);
    }

    void EnsureBuilt()
    {
        if (isBuilding)
            return;

        if (contactMesh != null && contactMaterial != null &&
            leftContact != null && leftContact.visual != null &&
            rightContact != null && rightContact.visual != null)
            return;

        isBuilding = true;
        ResolveFeet();

        try
        {
            if (contactMesh == null)
                contactMesh = BuildContactMesh();

            if (contactMaterial == null)
            {
                Shader shader = Shader.Find(ContactShaderName);
                if (shader == null)
                    return;

                contactMaterial = new Material(shader)
                {
                    name = "Runtime Player Ground Contact",
                    hideFlags = HideFlags.DontSave
                };
            }

            if (leftContact == null || leftContact.visual == null)
                leftContact = BuildFootContact("Player Ground Contact - Left", leftFoot);

            if (rightContact == null || rightContact.visual == null)
                rightContact = BuildFootContact("Player Ground Contact - Right", rightFoot);
        }
        finally
        {
            isBuilding = false;
        }
    }

    void ResolveFeet()
    {
        if (animator != null && animator.isHuman)
        {
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        if (leftFoot == null)
            leftFoot = FindBone("leftfoot");

        if (rightFoot == null)
            rightFoot = FindBone("rightfoot");
    }

    Transform FindBone(string token)
    {
        Transform root = visualRoot != null ? visualRoot : transform;

        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            string normalizedName = candidate.name.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
            if (normalizedName.Contains(token))
                return candidate;
        }

        return null;
    }

    FootContact BuildFootContact(string contactName, Transform bone)
    {
        GameObject contactObject = new GameObject(contactName)
        {
            hideFlags = HideFlags.DontSave,
            layer = gameObject.layer
        };

        MeshFilter filter = contactObject.AddComponent<MeshFilter>();
        filter.sharedMesh = contactMesh;

        contactObject.transform.SetParent(transform, true);

        MeshRenderer contactRenderer = contactObject.AddComponent<MeshRenderer>();
        contactRenderer.sharedMaterial = contactMaterial;
        contactRenderer.shadowCastingMode = ShadowCastingMode.Off;
        contactRenderer.receiveShadows = false;
        contactRenderer.lightProbeUsage = LightProbeUsage.Off;
        contactRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        contactRenderer.allowOcclusionWhenDynamic = false;

        FootContact contact = new FootContact
        {
            bone = bone,
            visual = contactObject.transform,
            renderer = contactRenderer,
            properties = new MaterialPropertyBlock(),
            opacity = 0f
        };

        SetImmediateOpacity(contact, 0f);
        return contact;
    }

    void UpdateFoot(FootContact contact, bool mayShow)
    {
        if (contact == null || contact.visual == null || contact.renderer == null)
            return;

        if (contact.bone == null)
            contact.bone = contact == leftContact ? leftFoot : rightFoot;

        RaycastHit groundHit = default;
        bool hasGround = mayShow && contact.bone != null && TryFindGround(contact.bone.position, out groundHit);
        float targetOpacity = 0f;

        if (hasGround)
        {
            float footHeight = Mathf.Max(0f, Vector3.Dot(contact.bone.position - groundHit.point, groundHit.normal));
            targetOpacity = CalculateContactAlpha(footHeight, fullContactHeight, fadeContactHeight);

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundHit.normal);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(Vector3.forward, groundHit.normal);

            contact.visual.SetPositionAndRotation(
                groundHit.point + groundHit.normal * surfaceOffset,
                Quaternion.LookRotation(forward.normalized, groundHit.normal));

            float compressedHeight = Mathf.InverseLerp(fadeContactHeight, 0f, footHeight);
            float scale = Mathf.Lerp(0.78f, 1f, compressedHeight);
            contact.visual.localScale = new Vector3(footprintSize.x * scale, 1f, footprintSize.y * scale);
        }

        float response = targetOpacity > contact.opacity ? appearSpeed : disappearSpeed;
        contact.opacity = Mathf.MoveTowards(contact.opacity, targetOpacity, Mathf.Max(0f, response) * Time.deltaTime);
        ApplyOpacity(contact);
    }

    bool TryFindGround(Vector3 footPosition, out RaycastHit bestHit)
    {
        bestHit = default;
        Vector3 origin = footPosition + Vector3.up * probeLift;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHitBuffer,
            Mathf.Max(0.05f, probeLift + probeDistance),
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = groundHitBuffer[index];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.normal.y < minimumGroundNormal)
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = hit.distance;
            bestHit = hit;
        }

        return bestDistance < float.PositiveInfinity;
    }

    void ApplyOpacity(FootContact contact)
    {
        if (contact == null || contact.renderer == null)
            return;

        contact.renderer.enabled = contact.opacity > 0.001f;
        contact.properties.SetFloat(OpacityId, contact.opacity);
        contact.renderer.SetPropertyBlock(contact.properties);
    }

    void SetImmediateOpacity(FootContact contact, float opacity)
    {
        if (contact == null)
            return;

        contact.opacity = opacity;
        ApplyOpacity(contact);
    }

    static Mesh BuildContactMesh()
    {
        Mesh mesh = new Mesh
        {
            name = "Runtime Player Ground Contact Mesh",
            hideFlags = HideFlags.DontSave
        };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, 0.5f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        return mesh;
    }

    public static float CalculateContactAlpha(float footHeight, float fullHeight, float fadeHeight)
    {
        float safeFullHeight = Mathf.Max(0f, fullHeight);
        float safeFadeHeight = Mathf.Max(safeFullHeight + 0.001f, fadeHeight);
        return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(safeFullHeight, safeFadeHeight, Mathf.Max(0f, footHeight)));
    }

    static void DestroyRuntimeObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
