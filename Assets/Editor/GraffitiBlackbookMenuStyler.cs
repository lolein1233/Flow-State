using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Aplica arte de libreta a los objetos existentes sin sustituir sus callbacks.</summary>
public static class GraffitiBlackbookMenuStyler
{
    const string Art = "Assets/04_Materiales e Imagenes/GraffitiBlackbook/";
    static readonly Color Ink = new Color32(27, 22, 28, 255);
    static readonly Rect Full = new Rect(0f, 0f, 1f, 1f);
    // Deja fuera la punta del tag vecino al recortar la tira del atlas.
    static readonly Rect TornPaper = new Rect(0.55f, 0.02f, 0.435f, 0.325f);

    [MenuItem("FLOWSTATE/UI/Aplicar libreta de graffiti a la pausa")]
    public static void Apply()
    {
        if (Application.isPlaying) return;
        var controller = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (controller == null) return;
        var scene = controller.gameObject.scene;
        var root = controller.transform.Find("PauseMenu");
        var shade = root.Find("NightShade");
        var panel = (RectTransform)shade.Find("GraffitiPaper");
        var shadow = (RectTransform)shade.Find("PanelShadow");
        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Diseñar libreta de graffiti");

        PrepareTexture(Art + "Blackbook_OpenPaper.png");
        PrepareTexture(Art + "Blackbook_StickersAtlas.png");
        PrepareTexture(Art + "Reference_HandstylePages.png");
        PrepareTexture(Art + "Reference_NeonMarker.png");
        Texture2D book = Load("Blackbook_OpenPaper.png");
        Texture2D atlas = Load("Blackbook_StickersAtlas.png");
        Texture2D graffiti = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04_Materiales e Imagenes/imagenes/graffitis.png");

        panel.sizeDelta = shadow.sizeDelta = new Vector2(1700f, 1063f);
        panel.anchoredPosition = Vector2.zero;
        panel.localRotation = Quaternion.Euler(0f, 0f, -0.55f);
        shadow.anchoredPosition = new Vector2(18f, -16f);
        shadow.localRotation = panel.localRotation;
        panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
        shadow.GetComponent<UnityEngine.UI.Image>().enabled = false;
        RawImage surface = Raw("BookSurface - Papel y lomo", panel, book, Vector2.zero, panel.sizeDelta, Full);
        surface.transform.SetAsFirstSibling();
        RawImage contactShadow = Raw("BookContactShadow", shadow, book, Vector2.zero, shadow.sizeDelta, Full);
        contactShadow.color = new Color(0f, 0f, 0f, 0.24f);

        foreach (string name in new[] { "TopInk", "TopAccent", "SideAccent", "Divider", "PauseSubtitle", "CyanRegistration", "MagentaRegistration" })
        {
            Transform existing = panel.Find(name);
            if (existing != null) existing.gameObject.SetActive(false);
        }

        var title = panel.Find("Title").GetComponent<TextMeshProUGUI>();
        Place(title.rectTransform, new Vector2(405f, 330f), new Vector2(650f, 155f), -1.8f);
        title.fontSize = title.fontSizeMax = 142f;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        TMP_FontAsset marker = title.font;
        TMP_FontAsset readable = FindReadableFont();

        Text("BlackbookEdition", panel, readable, "FLOWSTATE   /   BLACKBOOK 01", new Vector2(395f, 410f), new Vector2(620f, 32f), 20f, -1f);
        if (panel.Find("HandwrittenPauseNote") != null)
            panel.Find("HandwrittenPauseNote").gameObject.SetActive(false);

        // Cada recorte conserva su textura y su UV Rect editable en el Inspector.
        var collage = FindOrCreate("BlackbookCollage - Recortes editables", panel);
        Place(collage, Vector2.zero, panel.sizeDelta);
        Raw("Recorte handstyles - referencia", collage, Load("Reference_HandstylePages.png"),
            new Vector2(-392f, 290f), new Vector2(635f, 215f), new Rect(0.56f, 0.51f, 0.38f, 0.43f), 3.4f);
        Raw("Pieza verde - graffitis.png", collage, graffiti, new Vector2(-450f, 245f),
            new Vector2(172f, 455f), new Rect(0.315f, 0.52f, 0.145f, 0.46f), 86f);
        Raw("Pieza rosa - graffitis.png", collage, graffiti, new Vector2(-605f, 130f),
            new Vector2(155f, 305f), new Rect(0.01f, 0.51f, 0.145f, 0.48f), 94f);
        Raw("FLOWSTATE - tag original", collage, atlas, new Vector2(-390f, 20f),
            new Vector2(720f, 240f), new Rect(0f, 0.015f, 0.55f, 0.37f), -3f);
        if (collage.Find("Foto neon - referencia") != null)
            collage.Find("Foto neon - referencia").gameObject.SetActive(false);
        Raw("Pieza naranja - graffitis.png", collage, graffiti, new Vector2(-410f, -180f),
            new Vector2(170f, 440f), new Rect(0.615f, 0.49f, 0.15f, 0.5f), 86f);
        Raw("Pieza amarilla - graffitis.png", collage, graffiti, new Vector2(-455f, -330f),
            new Vector2(155f, 450f), new Rect(0.16f, 0.54f, 0.155f, 0.45f), 94f);
        Raw("Pieza lila - graffitis.png", collage, graffiti, new Vector2(-360f, -120f),
            new Vector2(125f, 355f), new Rect(0.625f, 0.005f, 0.135f, 0.49f), 89f);
        Raw("Pieza violeta - graffitis.png", collage, graffiti, new Vector2(-590f, -205f),
            new Vector2(126f, 280f), new Rect(0.19f, 0.005f, 0.14f, 0.53f), 92f);
        Raw("Pieza verde repetida", collage, graffiti, new Vector2(-215f, -255f),
            new Vector2(110f, 290f), new Rect(0.315f, 0.52f, 0.145f, 0.46f), 82f);
        Raw("Pulgar holografico - recorte", collage, atlas, new Vector2(-130f, 300f),
            new Vector2(185f, 180f), new Rect(0.08f, 0.39f, 0.4f, 0.58f), -13f);
        Raw("Libreta con ojos - recorte", collage, atlas, new Vector2(-135f, -295f),
            new Vector2(177f, 175f), new Rect(0.56f, 0.38f, 0.42f, 0.6f), 12f);
        Raw("Pulgar repetido - esquina", collage, atlas, new Vector2(-680f, -325f),
            new Vector2(128f, 128f), new Rect(0.08f, 0.39f, 0.4f, 0.58f), 17f);
        Raw("Cinta superior", collage, atlas, new Vector2(-650f, 372f), new Vector2(160f, 37f), TornPaper, -18f);
        Raw("Cinta foto", collage, atlas, new Vector2(-660f, -95f), new Vector2(143f, 34f), TornPaper, 18f);
        if (collage.Find("Nota margen") != null)
            collage.Find("Nota margen").gameObject.SetActive(false);

        string[] names = { "ResumeButton", "ControlsButton", "MainMenuButton", "QuitButton" };
        float[] angles = { 1.2f, -1.4f, 0.8f, -1f };
        for (int i = 0; i < names.Length; i++)
        {
            var button = panel.Find(names[i]).GetComponent<UnityEngine.UI.Button>();
            var rect = button.GetComponent<RectTransform>();
            Place(rect, new Vector2(412f, 137f - i * 143f), new Vector2(620f, 128f), angles[i]);
            button.GetComponent<UnityEngine.UI.Image>().enabled = false;
            button.transform.Find("Accent").gameObject.SetActive(false);
            RawImage strip = Raw("TornPaper - Papel rasgado", rect, atlas, Vector2.zero, rect.sizeDelta, TornPaper);
            strip.transform.SetAsFirstSibling();
            strip.raycastTarget = true;
            button.targetGraphic = strip;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.72f, 1f);
            colors.selectedColor = new Color(1f, 0.89f, 0.55f, 1f);
            colors.pressedColor = new Color(0.84f, 0.76f, 0.57f, 1f);
            button.colors = colors;

            var label = button.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            label.color = Ink;
            label.fontSize = label.fontSizeMax = 49f;
            label.fontSizeMin = 31f;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(85f, 0f);
            label.rectTransform.offsetMax = new Vector2(-24f, 0f);
            label.rectTransform.localScale = Vector3.one;
            var number = button.transform.Find("Number").GetComponent<TMP_Text>();
            number.color = new Color32(149, 33, 36, 255);
            button.GetComponent<PunkMenuButton>().Configure(Ink, Ink);
        }

        var hint = panel.Find("Hint").GetComponent<TMP_Text>();
        Place(hint.rectTransform, new Vector2(405f, -398f), new Vector2(610f, 42f), -1f);
        hint.color = Ink;
        hint.alpha = 0.7f;
        hint.fontSize = 22f;
        hint.text = "ESC  /  volver al juego";

        var backdrop = shade.Find("HolographicBackdrop").GetComponent<UnityEngine.UI.Image>();
        root.GetComponent<PunkMenuMotion>().Configure(panel, shadow, title.rectTransform, root.GetComponent<CanvasGroup>(), true, backdrop);
        root.gameObject.SetActive(true);
        root.GetComponent<CanvasGroup>().alpha = 1f;
        controller.transform.Find("ControlsMenu - Original Pause Design").gameObject.SetActive(false);
        foreach (Component component in controller.GetComponentsInChildren<Component>(true))
            if (component != null) EditorUtility.SetDirty(component);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = panel.gameObject;
        SceneView.RepaintAll();
    }

    static void PrepareTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    static Texture2D Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Art + name);

    static RectTransform FindOrCreate(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return (RectTransform)existing;
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size, float angle = 0f)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    static RawImage Raw(string name, Transform parent, Texture texture, Vector2 position, Vector2 size, Rect uv, float angle = 0f)
    {
        RectTransform rect = FindOrCreate(name, parent);
        Place(rect, position, size, angle);
        RawImage image = rect.GetComponent<RawImage>();
        if (image == null) image = rect.gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.uvRect = uv;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    static void Text(string name, Transform parent, TMP_FontAsset font, string text, Vector2 position, Vector2 size, float fontSize, float angle)
    {
        RectTransform rect = FindOrCreate(name, parent);
        Place(rect, position, size, angle);
        var label = rect.GetComponent<TextMeshProUGUI>();
        if (label == null) label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.color = Ink;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
    }

    static TMP_FontAsset FindReadableFont()
    {
        foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (font.name == "SpaceGrotesk-Bold SDF") return font;
        return TMP_Settings.defaultFontAsset;
    }
}
