using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class QuickGraffitiBookBuilder
{
    const string Art = "Assets/04_Materiales e Imagenes/GraffitiBlackbook/";
    static readonly Color Ink = new Color32(37, 25, 44, 255);
    static readonly Color Paper = new Color32(244, 232, 207, 255);

    public static QuickGraffitiEntry[] LoadCatalog()
    {
        var entries = new List<QuickGraffitiEntry>();
        Sprite[] stickers = AssetDatabase.LoadAllAssetsAtPath(Art + "Blackbook_StickersAtlas.png").OfType<Sprite>().ToArray();
        foreach (var pair in new[] { ("Blackbook_StickersAtlas_1", "Pulgar holográfico"), ("Blackbook_StickersAtlas_2", "Libreta con ojos") })
            entries.Add(new QuickGraffitiEntry { title = pair.Item2, sprite = stickers.Single(s => s.name == pair.Item1),
                width = 1.35f, paintSeconds = 3f, sprayColor = new Color(0.5f, 0.7f, 1f) });
        string[] titles = { "Bubble rosa", "Sombra violeta", "Flecha dorada", "Wildstyle púrpura", "Throw-up lima", "Lata rosa",
            "Bubble malva", "Wildstyle cian", "Personaje naranja", "Oliva", "Violeta doble", "Vagón" };
        Color[] colors = { Color.magenta, new Color(0.35f,0.1f,0.7f), Color.yellow, new Color(0.5f,0f,1f),
            Color.green, Color.magenta, new Color(0.8f,0.3f,0.6f), Color.cyan, new Color(1f,0.4f,0f),
            new Color(0.6f,0.65f,0.4f), new Color(0.5f,0.2f,1f), new Color(0.8f,0.3f,0.1f) };
        Sprite[] tags = AssetDatabase.LoadAllAssetsAtPath(Art + "graffitis.png").OfType<Sprite>().ToArray();
        for (int i = 0; i < 12; i++)
        {
            int index = i;
            entries.Add(new QuickGraffitiEntry { title = titles[i], sprite = tags.Single(s => s.name.EndsWith("_" + index)),
                rotateArtwork = true, width = 2f, paintSeconds = 3.8f, sprayColor = colors[i] });
        }
        return entries.ToArray();
    }

    [MenuItem("FLOWSTATE/UI/Crear libreta editable de graffiti rápido")]
    public static void Create()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/02_Escenas/Gameplay.unity")
            throw new System.InvalidOperationException("Abre Gameplay fuera de Play Mode.");
        var existing = Object.FindFirstObjectByType<QuickGraffitiBook>(FindObjectsInactive.Include);
        if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
        var player = Object.FindFirstObjectByType<FPSController>();
        var quick = Undo.AddComponent<GraffitiQuickPainter>(player.gameObject);
        quick.player = player;
        quick.painter = player.GetComponent<GraffitiPainter>();
        quick.library = LoadCatalog();
        const string materialPath = "Assets/Resources/Graffiti/QuickGraffitiReveal.mat";
        quick.artworkMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (quick.artworkMaterial == null)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/Graffiti/QuickGraffitiReveal.shader");
            quick.artworkMaterial = new Material(shader) { name = "Quick Graffiti Reveal" };
            AssetDatabase.CreateAsset(quick.artworkMaterial, materialPath);
        }
        var root = new GameObject("MENÚ GRAFFITI RÁPIDO - LIBRETA EDITABLE");
        Undo.RegisterCreatedObjectUndo(root, "Crear libreta de graffiti");
        var book = root.AddComponent<QuickGraffitiBook>();
        book.owner = quick;
        quick.book = book;
        var canvas = Canvas("Libreta - Canvas", root.transform, 32010);
        book.canvasRoot = canvas.gameObject;
        var group = canvas.gameObject.AddComponent<CanvasGroup>();
        var shade = Rect("Sombra de fondo", canvas.transform, Vector2.zero, Vector2.zero);
        Stretch(shade);
        shade.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.015f, 0.045f, 0.78f);
        var shadow = Rect("Sombra de libreta", canvas.transform, new Vector2(18f, -14f), new Vector2(1700f, 1040f));
        var paper = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Blackbook_OpenPaper.png");
        shadow.gameObject.AddComponent<UnityEngine.UI.RawImage>().texture = paper;
        shadow.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0f,0f,0f,0.5f);
        shadow.GetComponent<UnityEngine.UI.RawImage>().raycastTarget = false;
        var panel = Rect("Papel - Mueve y edita los dibujos aquí", canvas.transform, Vector2.zero, shadow.sizeDelta);
        var surface = panel.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        surface.texture = paper; surface.raycastTarget = false;
        TMP_FontAsset marker = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_Tipografias/owned SDF.asset");
        TMP_FontAsset body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_Tipografias/SpaceGrotesk-Bold SDF.asset");
        Label("Edición", panel, body, "BLACKBOOK  /  COLECCIÓN DE CALLE", new Vector2(0f, 414f), new Vector2(1250f, 38f), 20f);
        var title = Label("Título", panel, marker, "ELIGE TU PRÓXIMA MARCA", new Vector2(0f, 350f), new Vector2(1300f, 95f), 63f);
        book.selectionLabel = Label("Selección actual", panel, body, "EN LA LATA: PULGAR HOLOGRÁFICO", new Vector2(0f,-330f), new Vector2(1270f, 35f), 22f);
        Label("Instrucciones", panel, body, "CLIC: ELEGIR  ·  MANTÉN CLIC EN LA PARED PARA PINTAR\nSUELTA PARA PAUSAR  /  E: VOLVER A LIBRE  /  ESC: CERRAR", new Vector2(0f,-389f), new Vector2(1300f, 60f), 20f);
        book.previousButton = Button("Página anterior", panel, body, "← ANTERIOR", new Vector2(-565f,-447f), new Vector2(240f,43f));
        book.nextButton = Button("Página siguiente", panel, body, "SIGUIENTE →", new Vector2(565f,-447f), new Vector2(240f,43f));
        book.closeButton = Button("Cerrar libreta", panel, body, "CERRAR  /  Q", new Vector2(633f,414f), new Vector2(220f,43f));
        book.pageLabel = Label("Número de página", panel, body, "BLACKBOOK  /  01 — 03", new Vector2(0f,-447f), new Vector2(500f,43f), 21f);
        book.pages = new GameObject[3];
        book.artworkButtons = new UnityEngine.UI.Button[quick.library.Length];
        book.selectionMarks = new GameObject[quick.library.Length];
        float[] angles = { -4f, 3f, 2f, -3f, -2f, 4f };
        for (int page = 0; page < book.pages.Length; page++)
        {
            var spread = Rect("Doble página " + (page+1).ToString("00"), panel, Vector2.zero, panel.sizeDelta);
            book.pages[page] = spread.gameObject;
            // Keep header, navigation and paper readable above the editable artwork pages.
            spread.SetSiblingIndex(0);
            for (int slot = 0; slot < 6; slot++)
            {
                int index = page*6+slot;
                if (index >= quick.library.Length) break;
                var entry = quick.library[index];
                float x = slot%2==0 ? -393f : 393f;
                float y = 213f - slot/2*195f;
                var card = Rect((index+1).ToString("00") + " - " + entry.title, spread, new Vector2(x,y), new Vector2(620f,183f));
                card.localEulerAngles = new Vector3(0,0,angles[slot]);
                var target = card.gameObject.AddComponent<UnityEngine.UI.Image>();
                target.color = new Color(1,1,1,0.001f);
                var button = card.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = target;
                var tint = button.colors;
                tint.normalColor = new Color(1,1,1,0.001f);
                tint.highlightedColor = new Color(0.65f,0.45f,0.8f,0.12f);
                tint.selectedColor = new Color(0.65f,0.45f,0.8f,0.1f);
                button.colors = tint;
                var highlight = Rect("Marca de selección", card, new Vector2(0,-73f), new Vector2(365f,21f));
                highlight.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.75f,0.92f,0.1f,0.6f);
                highlight.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                book.selectionMarks[index] = highlight.gameObject;
                highlight.gameObject.SetActive(index==0);
                Vector2 dimensions = entry.Size;
                dimensions *= Mathf.Min(460f/dimensions.x, 143f/dimensions.y);
                if (entry.rotateArtwork) dimensions = new Vector2(dimensions.y,dimensions.x);
                var imageRect = Rect("Sprite recortado - Editable", card, new Vector2(0,12f), dimensions);
                imageRect.localEulerAngles = new Vector3(0,0,entry.rotateArtwork?90f:0f);
                var image = imageRect.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.sprite = entry.sprite; image.preserveAspect = true; image.raycastTarget = false;
                Label("Nombre", card, body, (index+1).ToString("00") + " / " + entry.title.ToUpperInvariant(), new Vector2(0,-73f), new Vector2(550f,28f), 18f);
                book.artworkButtons[index] = button;
            }
            spread.gameObject.SetActive(page==0);
        }
        canvas.gameObject.AddComponent<PunkMenuMotion>().Configure(panel,shadow,title.rectTransform,group,true);
        canvas.gameObject.SetActive(false);
        var hud = Canvas("Indicador - Modo rápido", root.transform, 650);
        quick.modeHud = hud.gameObject;
        var banner = Rect("Modo y pieza seleccionada", hud.transform, new Vector2(0f,445f), new Vector2(1060f,82f));
        var backing = banner.gameObject.AddComponent<UnityEngine.UI.Image>();
        backing.color = new Color(0.07f,0.025f,0.1f,0.87f); backing.raycastTarget = false;
        quick.modeLabel = Label("Texto - Ayuda contextual", banner, body, "GRAFFITI RÁPIDO", Vector2.zero, new Vector2(1010f,72f), 22f);
        quick.modeLabel.color = Paper;
        hud.gameObject.SetActive(false);
        EnsureFreeModeHint(quick);
        var visibility = player.GetComponent<GraffitiModeHudVisibility>();
        if (visibility == null) visibility = Undo.AddComponent<GraffitiModeHudVisibility>(player.gameObject);
        visibility.player = player;
        visibility.hideWhilePainting = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(c=>c.name=="CAMBIAR EMOCION CANVA"||c.name=="CONTROLES CANVA").Select(c=>c.gameObject).ToArray();
        UpdateControls();
        foreach(var c in root.GetComponentsInChildren<Component>(true)) if(c!=null) EditorUtility.SetDirty(c);
        EditorUtility.SetDirty(quick); EditorUtility.SetDirty(visibility);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
    }

    public static string[,] QuickControls => new string[,]
    {
        { "Q / PARED", "Entrar al modo graffiti" },
        { "E", "Alternar graffiti libre / rápido" },
        { "Q OTRA VEZ", "Colores (libre) / libreta (rápido)" },
        { "MANTÉN CLIC", "Pintar; suelta para pausar la pieza" },
        { "E / CLIC", "Elegir dibujo, color o boquilla" },
        { "← / →", "Pasar páginas de la libreta" },
        { "TAB", "Cambiar boquilla en modo libre" },
        { "R", "Agitar y recuperar la mezcla" },
        { "F", "Cambiar la lata cuando esté vacía" },
        { "RUEDA MOUSE", "Cambiar tamaño del graffiti rápido" },
        { "ESC", "Cerrar menú; otra vez: salir" },
        { "B", "Abrir / cerrar esta ayuda" }
    };

    public static void UpdateControls()
    {
        var pause = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        var so = new SerializedObject(pause);
        var page = (GameObject)so.FindProperty("controlsPages").GetArrayElementAtIndex(0).objectReferenceValue;
        var keys = page.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name.StartsWith("Key")&&r.name!="KeyLabel"&&r.anchoredPosition.x>700f).ToArray();
        var actions = page.GetComponentsInChildren<TextMeshProUGUI>(true).Where(t=>t.name.StartsWith("Action")&&t.rectTransform.anchoredPosition.x>700f).ToArray();
        var keyTemplate = keys.First(r=>r.name=="Key0");
        var actionTemplate = actions.First(t=>t.name=="Action0");
        for(int i=0;i<QuickControls.GetLength(0);i++)
        {
            var key = keys.FirstOrDefault(r=>r.name=="Key"+i);
            if(key==null) { key=Object.Instantiate(keyTemplate,page.transform);key.name="Key"+i; }
            var action = actions.FirstOrDefault(t=>t.name=="Action"+i);
            if(action==null) { action=Object.Instantiate(actionTemplate,page.transform);action.name="Action"+i; }
            float y=-316f-i*39f;
            key.anchoredPosition=new Vector2(872f,y);
            key.GetComponentInChildren<TMP_Text>(true).text=QuickControls[i,0];
            action.rectTransform.anchoredPosition=new Vector2(1204.5f,y);
            action.text=QuickControls[i,1];
            action.fontSizeMax=22f; action.fontSizeMin=17f;
            EditorUtility.SetDirty(key); EditorUtility.SetDirty(action);
        }
    }

    public static void EnsureFreeModeHint(GraffitiQuickPainter quick)
    {
        if (quick.freeModeHint != null) return;
        var canvas = Canvas("Indicador - Graffiti libre (editable)", quick.book.transform, 650);
        Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Añadir ayuda editable de graffiti libre");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_Tipografias/SpaceGrotesk-Bold SDF.asset");
        var text = Label("Texto - Cambiar modo con E", canvas.transform, font,
            "GRAFFITI LIBRE   ·   [E] CAMBIAR A GRAFFITI RÁPIDO", new Vector2(0f,480f), new Vector2(1060f,42f), 22f);
        text.color = Paper;
        var shadow = text.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0f,0f,0f,0.85f);
        shadow.effectDistance = new Vector2(1.5f,-1.5f);
        quick.freeModeHint = canvas.gameObject;
        canvas.gameObject.SetActive(false);
        foreach (var component in canvas.GetComponentsInChildren<Component>(true)) EditorUtility.SetDirty(component);
        EditorUtility.SetDirty(quick);
    }

    static Canvas Canvas(string name,Transform parent,int order)
    {
        var r=Rect(name,parent,Vector2.zero,Vector2.zero);
        var c=r.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=order;
        var scaler=r.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=0.5f;
        r.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();return c;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0.5f,0.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static TextMeshProUGUI Label(string name,Transform parent,TMP_FontAsset font,string text,Vector2 position,Vector2 size,float fontSize)
    {
        var t=Rect(name,parent,position,size).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;
        t.fontSize=fontSize;t.color=Ink;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    static UnityEngine.UI.Button Button(string name,Transform parent,TMP_FontAsset font,string text,Vector2 position,Vector2 size)
    {
        var r=Rect(name,parent,position,size);r.gameObject.AddComponent<UnityEngine.UI.Image>().color=Ink;
        var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();Label("Texto",r,font,text,Vector2.zero,size,20f).color=Paper;return b;
    }
}

[CustomEditor(typeof(QuickGraffitiBook))]
public sealed class QuickGraffitiBookEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if(Application.isPlaying)return;
        var book=(QuickGraffitiBook)target;
        EditorGUILayout.HelpBox("Los dibujos, textos y posiciones se editan en los hijos de esta libreta. No se reconstruye al entrar en Play.",MessageType.Info);
        for(int page=0;page<book.pages.Length;page++)if(GUILayout.Button("Ver doble página "+(page+1)))
        {
            book.canvasRoot.SetActive(true);
            for(int i=0;i<book.pages.Length;i++)book.pages[i].SetActive(i==page);
            book.canvasRoot.GetComponent<CanvasGroup>().alpha=1f;
            EditorSceneManager.MarkSceneDirty(book.gameObject.scene);
        }
        if(GUILayout.Button("Ocultar libreta")){book.canvasRoot.SetActive(false);EditorSceneManager.MarkSceneDirty(book.gameObject.scene);}
    }
}
