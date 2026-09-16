using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Entradas por proximidad; exige salir del área antes de reactivar.</summary>
public class SewerTeleportNetwork : MonoBehaviour
{
    public FPSController player;
    public Transform exteriorEntrance;
    public Transform exteriorArrival;
    public Transform headquartersArrival;
    public Transform[] returnEntrances;
    public float horizontalRadius = 3.5f;
    public float verticalTolerance = 6f;
    public float cooldown = 1f;
    [Header("Transición de teletransporte")]
    [Min(0.01f)] public float fadeOutDuration = 0.3f;
    [Min(0f)] public float blackScreenDuration = 0.12f;
    [Min(0.01f)] public float fadeInDuration = 0.4f;

    float nextTeleportTime;
    bool waitingForExit;
    bool transitioning;
    CanvasGroup fadeOverlay;

    public bool IsTransitioning => transitioning;
    public float TransitionOpacity => fadeOverlay != null ? fadeOverlay.alpha : 0f;

    void Update()
    {
        if (transitioning || player == null || !player.isActiveAndEnabled || Time.timeScale == 0f)
            return;

        Transform destination = null;
        if (IsNear(exteriorEntrance)) destination = headquartersArrival;
        if (returnEntrances != null)
            foreach (Transform entrance in returnEntrances)
                if (IsNear(entrance)) { destination = exteriorArrival; break; }

        if (destination == null) { waitingForExit = false; return; }
        if (waitingForExit || Time.time < nextTeleportTime) return;
        waitingForExit = true;
        StartCoroutine(Transition(destination));
    }

    IEnumerator Transition(Transform destination)
    {
        transitioning = true;
        EnsureOverlay();
        fadeOverlay.gameObject.SetActive(true);
        player.SetTeleportTransition(true);
        yield return Fade(0f, 1f, fadeOutDuration);
        // Mantener al menos un fotograma cubierto antes de cambiar de lugar.
        yield return null;
        if (player != null && destination != null)
            player.TeleportTo(destination.position, destination.rotation);
        yield return new WaitForSecondsRealtime(blackScreenDuration);
        yield return Fade(1f, 0f, fadeInDuration);
        FinishTransition();
        nextTeleportTime = Time.time + cooldown;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        fadeOverlay.alpha = to;
    }

    void EnsureOverlay()
    {
        if (fadeOverlay != null) return;
        var overlay = new GameObject("TP_Transition", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        fadeOverlay = overlay.GetComponent<CanvasGroup>();
        fadeOverlay.alpha = 0f;
        var panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = Color.black;
        panel.GetComponent<Image>().raycastTarget = false;
    }

    void FinishTransition()
    {
        if (player != null) player.SetTeleportTransition(false);
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.gameObject.SetActive(false);
        }
        transitioning = false;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        FinishTransition();
    }

    bool IsNear(Transform entrance)
    {
        if (entrance == null) return false;
        Vector3 delta = player.transform.position - entrance.position;
        return Mathf.Abs(delta.y) <= verticalTolerance &&
            delta.x * delta.x + delta.z * delta.z <= horizontalRadius * horizontalRadius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (exteriorEntrance != null) Gizmos.DrawWireSphere(exteriorEntrance.position, horizontalRadius);
        if (returnEntrances != null)
            foreach (Transform entrance in returnEntrances)
                if (entrance != null) Gizmos.DrawWireSphere(entrance.position, horizontalRadius);
    }
}
