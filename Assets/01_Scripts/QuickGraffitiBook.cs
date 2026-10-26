using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>References authored scene objects; never rebuilds or replaces the editable notebook.</summary>
public sealed class QuickGraffitiBook : MonoBehaviour
{
    public GraffitiQuickPainter owner;
    public GameObject canvasRoot;
    public GameObject[] pages;
    public UnityEngine.UI.Button[] artworkButtons;
    public GameObject[] selectionMarks;
    public UnityEngine.UI.Button previousButton;
    public UnityEngine.UI.Button nextButton;
    public UnityEngine.UI.Button closeButton;
    public TMP_Text pageLabel;
    public TMP_Text selectionLabel;
    public bool IsOpen { get; private set; }
    int page;
    bool previousCanMove;

    void Awake()
    {
        canvasRoot.SetActive(false);
        for (int i = 0; i < artworkButtons.Length; i++)
        {
            int index = i;
            artworkButtons[i].onClick.AddListener(() => owner.SelectArtwork(index));
        }
        previousButton.onClick.AddListener(() => ChangePage(-1));
        nextButton.onClick.AddListener(() => ChangePage(1));
        closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (IsOpen) return;
        previousCanMove = owner.player.canMove;
        owner.player.canMove = false;
        owner.painter.menuOpen = true;
        IsOpen = true;
        canvasRoot.SetActive(true);
        page = Mathf.Clamp(owner.selectedIndex / 6, 0, pages.Length - 1);
        Refresh();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        canvasRoot.SetActive(false);
        owner.painter.menuOpen = false;
        owner.player.canMove = previousCanMove;
        owner.NotifyBookClosed();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void ChangePage(int direction)
    {
        page = (page + direction + pages.Length) % pages.Length;
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == page);
        for (int i = 0; i < selectionMarks.Length; i++) selectionMarks[i].SetActive(i == owner.selectedIndex);
        if (pageLabel != null)
            pageLabel.text = "BLACKBOOK  /  " + (page + 1).ToString("00") + " — " + pages.Length.ToString("00");
        if (selectionLabel != null && owner.Selected != null)
            selectionLabel.text = "EN LA LATA: " + owner.Selected.title.ToUpperInvariant();
        if (EventSystem.current != null && artworkButtons.Length > page * 6)
            EventSystem.current.SetSelectedGameObject(artworkButtons[page * 6].gameObject);
    }

    void OnDisable() { if (IsOpen) Close(); }
}
