using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class RenderPoseController : MonoBehaviour
{
    [Header("Pose para render")]
    [SerializeField] Animator animator;
    [SerializeField] AnimationClip sittingClip;
    [SerializeField] KeyCode toggleKey = KeyCode.F;
    [SerializeField] bool listenForInput = true;
    [SerializeField] bool startSitting;
    [SerializeField, Min(0.01f)] float transitionDuration = 0.28f;
    [SerializeField, Range(0.1f, 2f)] float playbackSpeed = 0.82f;

    [Header("Integracion")]
    [SerializeField] FPSController movementController;
    [SerializeField] bool freezeMovement = true;
    [SerializeField] bool reserveFForGraffiti = true;

    PlayableGraph poseGraph;
    AnimationMixerPlayable mixer;
    AnimationClipPlayable sittingPlayable;
    AnimatorControllerPlayable controllerPlayable;
    bool hasControllerPlayable;
    bool wantsSitting;
    bool movementWasEnabled;
    bool movementFrozen;
    float blendWeight;
    float targetWeight;

    public bool IsSitting => wantsSitting;

    void Awake()
    {
        ResolveReferences();
    }

    void Start()
    {
        if (startSitting)
            SetSitting(true);
    }

    void Update()
    {
        if (listenForInput && Input.GetKeyDown(toggleKey))
        {
            if (!reserveFForGraffiti || movementController == null || !movementController.IsGraffitiMode())
                ToggleSitting();
        }

        if (!poseGraph.IsValid())
            return;

        UpdateClipLoop();
        UpdateBlend();
    }

    void OnDisable()
    {
        wantsSitting = false;
        DestroyGraphAndRestore();
    }

    public void Configure(Animator targetAnimator, AnimationClip clip, FPSController controller)
    {
        animator = targetAnimator;
        sittingClip = clip;
        movementController = controller;
    }

    [ContextMenu("Toggle Sitting Pose")]
    public void ToggleSitting()
    {
        SetSitting(!wantsSitting);
    }

    public void SetSitting(bool value)
    {
        if (value == wantsSitting)
            return;

        if (value)
        {
            if (!CanEnterPose() || !CreatePoseGraph())
                return;

            wantsSitting = true;
            targetWeight = 1f;
            FreezeMovement();
            return;
        }

        wantsSitting = false;
        targetWeight = 0f;
    }

    bool CanEnterPose()
    {
        ResolveReferences();

        if (animator == null || sittingClip == null)
        {
            Debug.LogWarning("Render pose needs an Animator and a sitting AnimationClip.", this);
            return false;
        }

        if (movementController != null && (movementController.IsClimbing() || movementController.IsParkouring()))
            return false;

        return true;
    }

    bool CreatePoseGraph()
    {
        DestroyGraphAndRestore();

        poseGraph = PlayableGraph.Create("Flow State Render Pose");
        poseGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
        hasControllerPlayable = runtimeController != null;
        int sittingInput = hasControllerPlayable ? 1 : 0;
        mixer = AnimationMixerPlayable.Create(poseGraph, sittingInput + 1);

        if (hasControllerPlayable)
        {
            controllerPlayable = AnimatorControllerPlayable.Create(poseGraph, runtimeController);
            poseGraph.Connect(controllerPlayable, 0, mixer, 0);
            mixer.SetInputWeight(0, 1f);
            PrepareIdleControllerPose();
        }

        sittingPlayable = AnimationClipPlayable.Create(poseGraph, sittingClip);
        sittingPlayable.SetApplyFootIK(false);
        sittingPlayable.SetSpeed(playbackSpeed);
        poseGraph.Connect(sittingPlayable, 0, mixer, sittingInput);
        mixer.SetInputWeight(sittingInput, 0f);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(poseGraph, "Render Pose Output", animator);
        output.SetSourcePlayable(mixer);
        blendWeight = 0f;
        poseGraph.Play();
        return true;
    }

    void PrepareIdleControllerPose()
    {
        SetControllerFloat("Speed", 0f);
        SetControllerFloat("VerticalSpeed", 0f);
        SetControllerBool("Grounded", true);
        SetControllerBool("GraffitiMode", false);
        SetControllerBool("Parkour", false);
        SetControllerBool("Climbing", false);
        SetControllerBool("Falling", false);
    }

    void UpdateClipLoop()
    {
        if (!sittingPlayable.IsValid() || sittingClip.length <= 0.01f)
            return;

        double duration = sittingClip.length;
        double time = sittingPlayable.GetTime();

        if (time >= duration)
            sittingPlayable.SetTime(time % duration);
    }

    void UpdateBlend()
    {
        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, transitionDuration);
        blendWeight = Mathf.MoveTowards(blendWeight, targetWeight, step);
        int sittingInput = hasControllerPlayable ? 1 : 0;

        if (hasControllerPlayable)
            mixer.SetInputWeight(0, 1f - blendWeight);

        mixer.SetInputWeight(sittingInput, blendWeight);

        if (!wantsSitting && blendWeight <= 0f)
            DestroyGraphAndRestore();
    }

    void FreezeMovement()
    {
        if (!freezeMovement || movementController == null)
            return;

        movementWasEnabled = movementController.canMove;
        movementController.canMove = false;
        movementFrozen = true;
    }

    void DestroyGraphAndRestore()
    {
        bool hadGraph = poseGraph.IsValid();

        if (poseGraph.IsValid())
            poseGraph.Destroy();

        hasControllerPlayable = false;
        blendWeight = 0f;
        targetWeight = 0f;

        if (movementFrozen && movementController != null)
            movementController.canMove = movementWasEnabled;

        movementFrozen = false;

        if (hadGraph && animator != null && animator.isActiveAndEnabled)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    void ResolveReferences()
    {
        if (movementController == null)
            movementController = GetComponent<FPSController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    void SetControllerFloat(string parameterName, float value)
    {
        if (hasControllerPlayable && HasParameter(parameterName, AnimatorControllerParameterType.Float))
            controllerPlayable.SetFloat(parameterName, value);
    }

    void SetControllerBool(string parameterName, bool value)
    {
        if (hasControllerPlayable && HasParameter(parameterName, AnimatorControllerParameterType.Bool))
            controllerPlayable.SetBool(parameterName, value);
    }

    bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
                return true;
        }

        return false;
    }
}
