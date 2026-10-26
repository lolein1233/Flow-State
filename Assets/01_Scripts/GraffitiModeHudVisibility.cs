using UnityEngine;

/// <summary>Hides only the assigned gameplay hints, preserving their previous visibility.</summary>
public sealed class GraffitiModeHudVisibility : MonoBehaviour
{
    public FPSController player;
    public GameObject[] hideWhilePainting;
    bool[] previous;
    bool hiding;

    void LateUpdate() => Refresh();

    public void Refresh()
    {
        bool shouldHide = player != null && player.IsGraffitiMode();
        if (shouldHide == hiding) return;
        if (shouldHide)
        {
            previous = new bool[hideWhilePainting.Length];
            for (int i = 0; i < hideWhilePainting.Length; i++)
                if (hideWhilePainting[i] != null)
                {
                    previous[i] = hideWhilePainting[i].activeSelf;
                    hideWhilePainting[i].SetActive(false);
                }
            hiding = true;
        }
        else Restore();
    }

    void Restore()
    {
        if (!hiding) return;
        for (int i = 0; i < hideWhilePainting.Length && i < previous.Length; i++)
            if (hideWhilePainting[i] != null) hideWhilePainting[i].SetActive(previous[i]);
        hiding = false;
    }

    void OnDisable() => Restore();
}
