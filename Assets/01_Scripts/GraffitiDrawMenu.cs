using UnityEngine;
using System.Collections;
public class GraffitiDrawMenu : MonoBehaviour
{
    public Transform visualRoot;
    public float drawTime = 0.5f;
    public ParticleSystem drawParticles;

    Vector3 originalScale;
    Collider[] colliders;

    public bool IsReady { get; private set; }

    void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        originalScale = visualRoot.localScale;
        colliders = GetComponentsInChildren<Collider>(true);

        SetColliders(false);
    }

    void Start()
    {
        Open();
    }

    public void Open()
    {
        StopAllCoroutines();
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        IsReady = false;
        SetColliders(false);

        if (drawParticles != null)
            drawParticles.Play();

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / drawTime;

            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualRoot.localScale = originalScale * Mathf.Lerp(0.05f, 1f, eased);

            yield return null;
        }

        visualRoot.localScale = originalScale;

        if (drawParticles != null)
            drawParticles.Stop();

        IsReady = true;
        SetColliders(true);
    }

    public void CloseAndDestroy()
    {
        StopAllCoroutines();
        StartCoroutine(CloseRoutine());
    }

    IEnumerator CloseRoutine()
    {
        IsReady = false;
        SetColliders(false);

        float t = 0f;
        Vector3 startScale = visualRoot.localScale;

        while (t < 1f)
        {
            t += Time.deltaTime / drawTime;

            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualRoot.localScale = Vector3.Lerp(startScale, originalScale * 0.05f, eased);

            yield return null;
        }

        Destroy(gameObject);
    }

    void SetColliders(bool value)
    {
        foreach (Collider c in colliders)
        {
            if (c != null)
                c.enabled = value;
        }
    }
}
