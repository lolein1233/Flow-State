using UnityEngine;
using UnityEngine.Rendering.Universal;
public class GraffitiFade : MonoBehaviour
{
    public float paintTime = 2f; // 👈 TIEMPO TOTAL PARA COMPLETAR

    float progress = 0f;

    DecalProjector proj;

    void Start()
    {
        proj = GetComponent<DecalProjector>();
        proj.fadeFactor = 0f;
    }

    public void AddPaint(float amount)
    {
        progress += amount / paintTime;
        progress = Mathf.Clamp01(progress);

        proj.fadeFactor = progress;
    }

    public bool IsComplete()
    {
        return progress >= 1f;
    }
}
