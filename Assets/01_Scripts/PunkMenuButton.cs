using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Animar el texto mantiene quieta la superficie que recibe el cursor.</summary>
public sealed class PunkMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] TMP_Text label, number;
    [SerializeField] RectTransform labelRect;
    Vector2 restPosition;
    [SerializeField] Color paper, ink;
    bool hovered, selected;
    float amount, motionScale;

    public void Configure(Color lightInk, Color darkInk)
    {
        paper = lightInk;
        ink = darkInk;
        CacheReferences();
    }

    void Awake() { CacheReferences(); }

    void CacheReferences()
    {
        if (label == null)
            label = transform.Find("Label").GetComponent<TMP_Text>();
        if (number == null)
            number = transform.Find("Number").GetComponent<TMP_Text>();
        labelRect = label.rectTransform;
        restPosition = labelRect.anchoredPosition;
        motionScale = PlayerPrefs.GetFloat("FS.Menu.Motion", 1f) < 0.8f ? 0.6f : 1f;
    }

    void Update()
    {
        if (label == null) return;
        amount = Mathf.Lerp(amount, hovered || selected ? 1f : 0f, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        labelRect.anchoredPosition = restPosition + Vector2.right * (amount * 10f * motionScale);
        labelRect.localScale = Vector3.one * (1f + amount * 0.025f * motionScale);
        label.color = Color.Lerp(paper, ink, amount);
        number.color = label.color;
    }

    void OnDisable() { hovered = selected = false; amount = 0f; }
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; }
    public void OnSelect(BaseEventData eventData) { selected = true; }
    public void OnDeselect(BaseEventData eventData) { selected = false; }
}
