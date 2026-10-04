#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GraffitiMenuArtDirector
{
    const string PrefabPath = "Assets/03_Prefabs/DrawPaintMenu.prefab";
    const string SourceNozzlesPrefabPath = "Assets/03_Prefabs/boquillas.prefab";
    const string ArtRootName = "ART_NEO_SPRAY_LAB";
    const string RuntimePreviewName = "__TEMP_NEO_SPRAY_LAB_PREVIEW__";
    const string AssetFolder = "Assets/04_Materiales e Imagenes/GraffitiMenuRedesign";

    static readonly Color Ink = Hex("191A1B");
    static readonly Color Plum = Hex("353533");
    static readonly Color Paper = Hex("EEE9DC");
    static readonly Color Cyan = Hex("00E5FF");
    static readonly Color Magenta = Hex("FF1478");
    static readonly Color Acid = Hex("D7FF00");
    static readonly Color Orange = Hex("FF6A00");
    static readonly Color Violet = Hex("703BFF");
    static readonly Color Coral = Hex("FF3D34");
    static readonly Color Mint = Hex("19F2BE");
    static readonly Color Steel = Hex("777B8B");
    static readonly Color Ochre = Hex("D6AF66");

    [MenuItem("Flow State/Graffiti Menu/Build Neo Spray Lab")]
    public static void Build()
    {
        EnsureFolder(AssetFolder);

        Material ink = MaterialAsset("M_Menu_Ink", Ink, true, 0f, 0.18f);
        Material plum = MaterialAsset("M_Menu_Plum", Plum, true, 0f, 0.2f);
        Material paper = MaterialAsset("M_Menu_Paper", Paper, true, 0f, 0.1f);
        Material accent = MaterialAsset("M_Menu_Ochre", Ochre, true, 0f, 0.2f);
        Material hologram = FoilMaterial("M_Menu_HolographicFoil", false);
        Material projection = FoilMaterial("M_Nozzle_Projection", false);
        projection.SetFloat("_SurfaceMode", 2f);
        Material cyan = MaterialAsset("M_Menu_Cyan", Cyan, true, 0f, 0.26f);
        Material magenta = MaterialAsset("M_Menu_Magenta", Magenta, true, 0f, 0.25f);
        Material acid = MaterialAsset("M_Menu_Acid", Acid, true, 0f, 0.2f);
        Material orange = MaterialAsset("M_Menu_Orange", Orange, true, 0f, 0.3f);
        Material violet = MaterialAsset("M_Menu_Violet", Violet, true, 0f, 0.3f);
        Material coral = MaterialAsset("M_Menu_Coral", Coral, true, 0f, 0.26f);
        Material mint = MaterialAsset("M_Menu_Mint", Mint, true, 0f, 0.24f);
        Material steel = FoilMaterial("M_Nozzle_HolographicSatin", true);
        Material blackMetal = MaterialAsset("M_Nozzle_Black", Hex("17171E"), false, 0.62f, 0.4f);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform oldArt = root.transform.Find(ArtRootName);
            if (oldArt != null)
                UnityEngine.Object.DestroyImmediate(oldArt.gameObject);

            Transform legacyColorLabel = root.transform.Find("Canvas");
            Transform legacyNozzleLabel = root.transform.Find("Canvas (2)");
            if (legacyColorLabel != null)
                legacyColorLabel.gameObject.SetActive(false);
            if (legacyNozzleLabel != null)
                legacyNozzleLabel.gameObject.SetActive(false);

            Transform art = NewRoot(ArtRootName, root.transform);
            BuildBackdrop(root.transform, art, ink, plum, paper, accent);
            BuildColorLab(root.transform, art, ink, paper, plum);
            BuildNozzleRack(root.transform, art, ink, paper, steel, blackMetal, plum, accent, projection);
            art.Find("Ink slab").GetComponent<Renderer>().sharedMaterial = hologram;
            BuildComicFrame(root.transform);

            GraffitiDrawMenu drawMenu = root.GetComponent<GraffitiDrawMenu>();
            if (drawMenu != null)
            {
                drawMenu.visualRoot = root.transform;
                drawMenu.drawTime = 0.32f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Flow State] Neo Spray Lab art direction rebuilt without changing graffiti paint profiles.");
    }

    [MenuItem("Flow State/Graffiti Menu/Log Source Nozzle Layout")]
    public static void LogSourceNozzleLayout()
    {
        GameObject source = PrefabUtility.LoadPrefabContents(SourceNozzlesPrefabPath);
        try
        {
            foreach (MeshFilter meshFilter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                Renderer renderer = meshFilter.GetComponent<Renderer>();
                Vector3 center = renderer != null
                    ? source.transform.InverseTransformPoint(renderer.bounds.center)
                    : meshFilter.transform.localPosition;
                Bounds bounds = meshFilter.sharedMesh != null ? meshFilter.sharedMesh.bounds : default;
                Debug.Log($"[Flow State] Source nozzle {meshFilter.name}: local={meshFilter.transform.localPosition}, center={center}, meshCenter={bounds.center}, meshSize={bounds.size}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
        }
    }

    [MenuItem("Flow State/Graffiti Menu/Open Neo Spray Lab Prefab")]
    public static void OpenPrefab()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        AssetDatabase.OpenAsset(Selection.activeObject);
        EditorApplication.delayCall += FramePrefabWithoutSelection;
    }

    [MenuItem("Flow State/Graffiti Menu/Capture Neo Spray Lab Preview")]
    public static void CapturePreview()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        foreach (TMP_Text text in instance.GetComponentsInChildren<TMP_Text>(true))
            text.ForceMeshUpdate(true, true);

        var preview = new PreviewRenderUtility();
        try
        {
            preview.AddSingleGO(instance);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.12f, 0.045f, 0.035f, 1f);
            preview.camera.transform.position = new Vector3(0f, 0f, -4.5f);
            preview.camera.transform.rotation = Quaternion.identity;
            preview.camera.fieldOfView = 28f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 20f;
            preview.ambientColor = new Color(0.52f, 0.5f, 0.62f, 1f);

            if (preview.lights.Length > 0)
            {
                preview.lights[0].intensity = 3.2f;
                preview.lights[0].transform.rotation = Quaternion.Euler(18f, 180f, 0f);
            }
            if (preview.lights.Length > 1)
            {
                preview.lights[1].intensity = 1.2f;
                preview.lights[1].color = Cyan;
                preview.lights[1].transform.rotation = Quaternion.Euler(330f, 215f, 0f);
            }

            Rect rect = new Rect(0f, 0f, 1400f, 800f);
            preview.BeginPreview(rect, GUIStyle.none);
            preview.Render(true);
            RenderTexture rendered = preview.EndPreview() as RenderTexture;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rendered;
            Texture2D png = new Texture2D(rendered.width, rendered.height, TextureFormat.RGBA32, false);
            png.ReadPixels(new Rect(0f, 0f, rendered.width, rendered.height), 0, 0);
            png.Apply();
            RenderTexture.active = previous;

            string output = Path.GetFullPath("Assets/Screenshots/GraffitiMenu_Refined_Close.png");
            File.WriteAllBytes(output, png.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(png);
            AssetDatabase.ImportAsset("Assets/Screenshots/GraffitiMenu_Refined_Close.png", ImportAssetOptions.ForceUpdate);
            Debug.Log("[Flow State] Preview captured: " + output);
        }
        finally
        {
            preview.Cleanup();
        }
    }

    [MenuItem("Flow State/Graffiti Menu/Place Temporary Camera Preview")]
    public static void PlaceTemporaryCameraPreview()
    {
        StageUtility.GoToMainStage();
        RemoveTemporaryCameraPreview();

        Camera camera = Camera.main;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (camera == null || prefab == null)
        {
            Debug.LogError("[Flow State] MainCamera or graffiti menu prefab was not found.");
            return;
        }

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        instance.name = RuntimePreviewName;
        instance.hideFlags = HideFlags.DontSave;
        instance.transform.position = camera.transform.position + camera.transform.forward * 4.2f;
        instance.transform.rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
        instance.transform.localScale = Vector3.one * 0.92f;
        Debug.Log("[Flow State] Temporary camera preview placed without modifying the scene asset.");
    }

    [MenuItem("Flow State/Graffiti Menu/Remove Temporary Camera Preview")]
    public static void RemoveTemporaryCameraPreview()
    {
        GameObject existing = GameObject.Find(RuntimePreviewName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    static void FramePrefabWithoutSelection()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null || stage.prefabContentsRoot == null)
            return;

        Selection.activeGameObject = stage.prefabContentsRoot;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView view = SceneView.lastActiveSceneView;
            view.drawGizmos = false;
            view.showGrid = false;
            view.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Textured);
            view.LookAt(stage.prefabContentsRoot.transform.position, Quaternion.identity, 1.85f, true);
            view.Repaint();
        }
        Selection.activeObject = null;
    }

    static void BuildBackdrop(Transform root, Transform art, Material ink, Material plum, Material paper, Material accent)
    {
        Transform background = root.Find("Fondo");
        if (background != null)
        {
            background.localPosition = new Vector3(0f, 0f, 0.035f);
            background.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            background.localScale = new Vector3(3.08f, 1.72f, 0.055f);
            MeshRenderer renderer = background.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = plum;
        }

        CreateBlock("Ink slab", art, new Vector3(0.02f, -0.005f, 0.02f), new Vector3(2.96f, 1.62f, 0.035f), new Vector3(0f, 0f, 0.8f), ink);
        CreateBlock("Header underline", art, new Vector3(-0.77f, 0.565f, -0.05f), new Vector3(1.12f, 0.018f, 0.018f), new Vector3(0f, 0f, -1f), accent);
        CreateText("Title", art, "FLOW STATE", new Vector3(-1.31f, 0.69f, -0.085f), new Vector2(1.45f, 0.24f), 1.25f, Paper, TextAlignmentOptions.Left, FontStyles.Bold);
        CreateText("Index", art, "TU MURO. TU HUELLA.", new Vector3(0.25f, 0.67f, -0.087f), new Vector2(1.05f, 0.20f), 0.5f, Ochre, TextAlignmentOptions.Right, FontStyles.Normal);
    }

    static void BuildReferenceMotifs(Transform art, Material ink, Material paper, Material cyan, Material magenta, Material acid, Material orange, Material violet, Material coral, Material mint)
    {
        Mesh spark = MeshAsset("MESH_Menu_ConcaveSpark", new[]
        {
            new Vector2(1f, 0f), new Vector2(0.16f, 0.13f),
            new Vector2(0f, 1f), new Vector2(-0.16f, 0.13f),
            new Vector2(-1f, 0f), new Vector2(-0.16f, -0.13f),
            new Vector2(0f, -1f), new Vector2(0.16f, -0.13f)
        });
        Mesh eye = MeshAsset("MESH_Menu_StreetEye", new[]
        {
            new Vector2(-1f, 0f), new Vector2(-0.62f, 0.48f), new Vector2(0f, 0.68f),
            new Vector2(0.62f, 0.48f), new Vector2(1f, 0f), new Vector2(0.62f, -0.48f),
            new Vector2(0f, -0.68f), new Vector2(-0.62f, -0.48f)
        });

        // Concave star and eye symbols echo the supplied editorial/graffiti references.
        CreateMeshShape("Spark outline", art, spark, new Vector3(0.70f, 0.085f, -0.047f), new Vector3(0.18f, 0.19f, 1f), -11f, magenta);
        CreateMeshShape("Spark face", art, spark, new Vector3(0.69f, 0.10f, -0.058f), new Vector3(0.145f, 0.155f, 1f), -11f, acid);
        CreateMeshShape("Spark cut", art, spark, new Vector3(0.69f, 0.10f, -0.067f), new Vector3(0.06f, 0.07f, 1f), 18f, ink);

        CreateMeshShape("Eye outline", art, eye, new Vector3(1.02f, 0.075f, -0.048f), new Vector3(0.25f, 0.13f, 1f), 8f, paper);
        CreateMeshShape("Eye iris", art, eye, new Vector3(1.02f, 0.075f, -0.059f), new Vector3(0.19f, 0.08f, 1f), 8f, violet);
        CreateMeshShape("Eye pupil", art, spark, new Vector3(1.02f, 0.075f, -0.070f), new Vector3(0.055f, 0.06f, 1f), 23f, cyan);

        // Layered spray strokes replace the previous single geometric bar.
        CreateBlock("Spray streak cyan", art, new Vector3(0.99f, 0.225f, -0.052f), new Vector3(0.38f, 0.022f, 0.018f), new Vector3(0f, 0f, -8f), cyan);
        CreateBlock("Spray streak coral", art, new Vector3(1.04f, 0.197f, -0.055f), new Vector3(0.28f, 0.016f, 0.018f), new Vector3(0f, 0f, -13f), coral);
        CreateBlock("Spray streak violet", art, new Vector3(1.09f, 0.17f, -0.058f), new Vector3(0.18f, 0.012f, 0.018f), new Vector3(0f, 0f, -18f), violet);

        // A compact registration constellation borrows the repeated micro-symbol rhythm.
        Vector3[] marks =
        {
            new Vector3(0.54f, 0.225f, -0.058f), new Vector3(0.59f, 0.185f, -0.058f),
            new Vector3(0.55f, 0.14f, -0.058f), new Vector3(0.61f, 0.105f, -0.058f)
        };
        Material[] markMaterials = { orange, mint, paper, magenta };
        for (int i = 0; i < marks.Length; i++)
            CreateMeshShape("Micro spark " + i, art, spark, marks[i], Vector3.one * (0.036f + i * 0.005f), i * 17f, markMaterials[i]);
    }

    static void BuildColorLab(Transform root, Transform art, Material ink, Material paper, Material neutral)
    {
        Transform wheel = root.Find("RGB_ColorWheel");
        if (wheel != null)
        {
            wheel.localPosition = new Vector3(-0.94f, 0.14f, -0.085f);
            wheel.localScale = Vector3.one * 0.50f;
            CreateBlock("Wheel card", art, new Vector3(-0.94f, 0.15f, -0.035f), new Vector3(0.76f, 0.61f, 0.028f), new Vector3(0f, 0f, -2f), paper);
            CreateBlock("Wheel shadow", art, new Vector3(-0.92f, 0.13f, -0.02f), new Vector3(0.79f, 0.62f, 0.02f), new Vector3(0f, 0f, -1f), neutral);
            CreateText("Wheel label", art, "COLOR", new Vector3(-1.28f, 0.488f, -0.095f), new Vector2(0.68f, 0.11f), 0.55f, Paper, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        Transform slider = root.Find("RGB_ValueSlider");
        if (slider != null)
        {
            slider.localPosition = new Vector3(-0.35f, 0.2f, -0.09f);
            slider.localScale = new Vector3(0.12f, 0.56f, 0.12f);
            CreateBlock("Pressure rail", art, new Vector3(-0.35f, 0.2f, -0.043f), new Vector3(0.23f, 0.63f, 0.025f), Vector3.zero, ink);
        }

        Transform preview = root.Find("RGB_ColorPreview");
        if (preview != null)
        {
            preview.localPosition = new Vector3(0.02f, 0.23f, -0.105f);
            preview.localScale = new Vector3(0.30f, 0.30f, 0.07f);
            CreateBlock("Preview frame", art, new Vector3(0.02f, 0.23f, -0.047f), new Vector3(0.37f, 0.37f, 0.026f), new Vector3(0f, 0f, 2f), paper);
            CreateText("Preview label", art, "TU TINTA", new Vector3(-0.18f, -0.025f, -0.09f), new Vector2(0.42f, 0.1f), 0.42f, Paper, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        CreateBlock("Lab divider", art, new Vector3(0.35f, 0.20f, -0.035f), new Vector3(0.012f, 0.57f, 0.02f), Vector3.zero, neutral);
        CreateText("Rack label", art, "ELIGE TU\nBOQUILLA", new Vector3(0.48f, 0.32f, -0.09f), new Vector2(0.85f, 0.32f), 0.95f, Paper, TextAlignmentOptions.Left, FontStyles.Bold);
        CreateText("Controls", art, "CLIC / E  ELEGIR\nQ  VOLVER A PINTAR", new Vector3(0.48f, 0.035f, -0.09f), new Vector2(0.86f, 0.18f), 0.40f, Ochre, TextAlignmentOptions.Left, FontStyles.Normal);
    }

    static void BuildNozzleRack(Transform root, Transform art, Material ink, Material paper, Material steel, Material blackMetal, Material neutral, Material accent, Material projection)
    {
        string[] names = { "Boquilla_Needle", "Boquilla_Splatter", "Boquilla_FatCap", "Boquilla_Chisel", "Boquilla_Soft" };
        string[] titles = { "PEQUEÑA", "MEDIANA", "GRANDE", "TRAZO", "DIFUMINAR" };
        string[] sourceParts = { "tripo_part_0", "tripo_part_3", "tripo_part_4", "tripo_part_2", "tripo_part_1" };
        float[] xs = { -1.08f, -0.54f, 0f, 0.54f, 1.08f };

        for (int i = 0; i < names.Length; i++)
        {
            Transform button = root.Find(names[i]);
            if (button == null)
                continue;

            button.localPosition = new Vector3(xs[i], -0.42f, -0.07f);
            button.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -0.7f : 0.7f);
            button.localScale = Vector3.one;
            button.GetComponent<GraffitiDrawnMenuButton>().nozzleName = titles[i];

            MeshRenderer legacyRenderer = button.GetComponent<MeshRenderer>();
            if (legacyRenderer != null)
                legacyRenderer.enabled = false;

            BoxCollider collider = button.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.size = new Vector3(0.46f, 0.56f, 0.16f);
                collider.center = new Vector3(0f, 0f, -0.08f);
            }

            Transform oldVisual = button.Find("ART_CAP");
            if (oldVisual != null)
                UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);

            Transform cardRoot = NewRoot("ART_CAP", button);
            CreateBlock("Card edge", cardRoot, new Vector3(0.009f, -0.012f, 0.045f), new Vector3(0.47f, 0.54f, 0.025f), Vector3.zero, neutral);
            CreateBlock("Card", cardRoot, new Vector3(0f, 0f, 0.025f), new Vector3(0.455f, 0.53f, 0.028f), Vector3.zero, ink);
            Renderer halo = CreateBlock("Hover halo", cardRoot, new Vector3(0f, 0.065f, -0.005f), new Vector3(0.405f, 0.33f, 0.015f), Vector3.zero, projection).GetComponent<Renderer>();
            Transform marker = CreateBlock("Selected underline", cardRoot, new Vector3(0f, -0.237f, -0.025f), new Vector3(0.36f, 0.018f, 0.015f), Vector3.zero, accent);
            marker.gameObject.SetActive(false);

            Transform modelPivot = NewRoot("Physical cap", cardRoot);
            modelPivot.localPosition = new Vector3(0f, 0.075f, -0.105f);
            modelPivot.localRotation = Quaternion.Euler(-8f, -24f + i * 12f, -4f + i * 2f);
            modelPivot.localScale = Vector3.one;
            BuildImportedNozzleModel(sourceParts[i], modelPivot, steel, blackMetal, paper);

            TMP_Text title = CreateText("Name", cardRoot, titles[i], new Vector3(-0.22f, -0.165f, -0.075f), new Vector2(0.44f, 0.14f), 0.66f, Paper, TextAlignmentOptions.Center, FontStyles.Bold);

            GraffitiNozzleVisual visual = button.GetComponent<GraffitiNozzleVisual>();
            if (visual == null)
                visual = button.gameObject.AddComponent<GraffitiNozzleVisual>();

            SerializedObject serialized = new SerializedObject(visual);
            serialized.FindProperty("modelPivot").objectReferenceValue = modelPivot;
            serialized.FindProperty("haloRenderer").objectReferenceValue = halo;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("selectionMarker").objectReferenceValue = marker.gameObject;
            serialized.FindProperty("idleColor").colorValue = Hex("484945");
            serialized.FindProperty("activeColor").colorValue = Ochre;
            serialized.FindProperty("hoverLift").floatValue = 0.025f;
            serialized.FindProperty("hoverDepth").floatValue = 0.025f;
            serialized.FindProperty("hoverTilt").floatValue = 6f;
            serialized.FindProperty("response").floatValue = 10f;
            Transform capModel = modelPivot.Find(sourceParts[i]);
            serialized.FindProperty("capRenderer").objectReferenceValue = capModel != null ? capModel.GetComponent<Renderer>() : null;
            serialized.FindProperty("idleMotion").floatValue = 1f;
            serialized.FindProperty("animationPhase").floatValue = i * 1.17f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

    }

    static void BuildImportedNozzleModel(string sourcePartName, Transform parent, Material steel, Material blackMetal, Material accent)
    {
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceNozzlesPrefabPath);
        Transform sourcePart = FindDescendant(sourcePrefab != null ? sourcePrefab.transform : null, sourcePartName);
        MeshFilter sourceFilter = sourcePart != null ? sourcePart.GetComponent<MeshFilter>() : null;
        Mesh mesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
        if (mesh == null)
        {
            Debug.LogWarning($"[Flow State] No se encontró la malla {sourcePartName} en {SourceNozzlesPrefabPath}.");
            CreatePrimitive("Fallback cap", PrimitiveType.Cylinder, parent, Vector3.zero, new Vector3(0.1f, 0.08f, 0.1f), new Vector3(90f, 0f, 0f), steel);
            return;
        }

        float largestFace = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y);
        float scale = largestFace > 0.0001f ? 0.29f / largestFace : 1f;

        GameObject silhouette = new GameObject("Accent silhouette", typeof(MeshFilter), typeof(MeshRenderer));
        silhouette.layer = 6;
        silhouette.transform.SetParent(parent, false);
        silhouette.transform.localPosition = -mesh.bounds.center * scale + new Vector3(0.012f, -0.012f, 0.026f);
        silhouette.transform.localScale = Vector3.one * scale * 1.06f;
        silhouette.GetComponent<MeshFilter>().sharedMesh = mesh;
        ConfigureMeshRenderer(silhouette.GetComponent<MeshRenderer>(), accent);

        GameObject model = new GameObject(sourcePartName, typeof(MeshFilter), typeof(MeshRenderer));
        model.layer = 6;
        model.transform.SetParent(parent, false);
        model.transform.localPosition = -mesh.bounds.center * scale;
        model.transform.localScale = Vector3.one * scale;
        model.GetComponent<MeshFilter>().sharedMesh = mesh;
        ConfigureMeshRenderer(model.GetComponent<MeshRenderer>(), steel);

        Transform port = CreatePrimitive("Nozzle aperture", PrimitiveType.Sphere, parent, new Vector3(0f, 0.035f, -0.12f), new Vector3(0.026f, 0.018f, 0.012f), Vector3.zero, blackMetal);
        port.localRotation = Quaternion.Euler(0f, 0f, 4f);
    }

    static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
            return null;

        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            if (candidate.name == name)
                return candidate;

        return null;
    }

    static void ConfigureMeshRenderer(MeshRenderer renderer, Material material)
    {
        if (renderer == null)
            return;

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static Mesh MeshAsset(string name, Vector2[] perimeter)
    {
        string path = AssetFolder + "/" + name + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
        }
        else
        {
            mesh.Clear();
        }

        Vector3[] vertices = new Vector3[perimeter.Length + 1];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < perimeter.Length; i++)
            vertices[i + 1] = new Vector3(perimeter[i].x, perimeter[i].y, 0f);

        int[] triangles = new int[perimeter.Length * 3];
        float signedArea = 0f;
        for (int i = 0; i < perimeter.Length; i++)
        {
            Vector2 current = perimeter[i];
            Vector2 nextPoint = perimeter[(i + 1) % perimeter.Length];
            signedArea += current.x * nextPoint.y - nextPoint.x * current.y;
        }

        for (int i = 0; i < perimeter.Length; i++)
        {
            int next = (i + 1) % perimeter.Length;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = signedArea >= 0f ? next + 1 : i + 1;
            triangles[i * 3 + 2] = signedArea >= 0f ? i + 1 : next + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static Transform CreateMeshShape(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, float zRotation, Material material)
    {
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.layer = 6;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        ConfigureMeshRenderer(go.GetComponent<MeshRenderer>(), material);
        return go.transform;
    }

    static Transform CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale, Vector3 rotation, Material material)
    {
        return CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, rotation, material);
    }

    static Transform CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Vector3 rotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localEulerAngles = rotation;
        go.transform.localScale = scale;
        go.layer = 6;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return go.transform;
    }

    static TMP_Text CreateText(string name, Transform parent, string value, Vector3 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment, FontStyles style, float zRotation = 0f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.layer = 6;
        go.transform.SetParent(parent, false);
        Quaternion rotation = Quaternion.Euler(0f, 0f, zRotation);
        go.transform.localRotation = rotation;
        go.transform.localPosition = position + rotation * new Vector3(size.x * 0.5f, 0f, 0f);

        TextMeshPro text = go.GetComponent<TextMeshPro>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_Tipografias/owned SDF.asset");
        if (font != null)
            text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = false;
        text.rectTransform.sizeDelta = size;
        text.renderer.sortingOrder = 8;
        return text;
    }

    static Transform NewRoot(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.layer = 6;
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    [MenuItem("Flow State/Graffiti Menu/Apply Character Comic Frame")]
    public static void ApplyComicFrame()
    {
        // This targeted path preserves all other menu art and paint settings.
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BuildComicFrame(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    static void BuildComicFrame(Transform root)
    {
        const string frameName = "ART_COMIC_FRAME";
        Shader shader = Shader.Find("FLOWSTATE/FSSRS/Player Comic Plate");
        if (shader == null)
            throw new InvalidOperationException("No se encontro el shader de contorno del personaje.");

        Mesh mesh = ComicFrameMesh();
        Transform previous = root.Find(frameName);
        if (previous != null)
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        Transform frame = NewRoot(frameName, root);
        frame.localPosition = new Vector3(0f, 0f, -0.022f);
        frame.localRotation = Quaternion.Euler(0f, 0f, -1.2f);

        string[] names = { "Echo", "Paper", "Color", "Ink" };
        float[] roles = { 3f, 0f, 1f, 2f };
        float[] widths = { 0.084f, 0.059f, 0.037f, 0.014f };
        float[] alpha = { 0.45f, 1f, 0.92f, 1f };
        Vector2[] offsets = { new Vector2(0.7f, -0.4f), new Vector2(-0.25f, 0.15f), new Vector2(0.4f, -0.2f), Vector2.zero };
        for (int i = 0; i < names.Length; i++)
        {
            string path = AssetFolder + "/M_Menu_Comic" + names[i] + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "M_Menu_Comic" + names[i] };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.renderQueue = 3004 + i;
            material.SetFloat("_PlateRole", roles[i]);
            material.SetFloat("_ShellWidth", widths[i]);
            material.SetVector("_RegistrationOffset", new Vector4(offsets[i].x, offsets[i].y, 0f, 0f));
            material.SetFloat("_JitterAmount", i == 1 ? 0.025f : 0.075f);
            material.SetFloat("_Breakup", i == 1 ? 0f : 0.045f);
            material.SetFloat("_Alpha", alpha[i]);
            material.SetFloat("_AnimationPhase", i * 0.65f);
            material.SetFloat("_PulseAmount", i == 0 ? 0.045f : 0.018f);
            material.SetFloat("_FlowSpeed", 0.35f);
            material.SetFloat("_PanelMotion", i == 0 ? 0.15f : 0.06f);
            EditorUtility.SetDirty(material);
            CreateMeshShape(names[i] + " perimeter", frame, mesh, Vector3.zero, Vector3.one, 0f, material);
        }
    }

    static Mesh ComicFrameMesh()
    {
        const float halfWidth = 1.54f;
        const float halfHeight = 0.86f;
        const float chamfer = 0.045f;
        const float band = 0.052f;
        const int segmentsPerSide = 16;
        Vector2[] corners =
        {
            new Vector2(-halfWidth + chamfer, -halfHeight), new Vector2(halfWidth - chamfer, -halfHeight),
            new Vector2(halfWidth, -halfHeight + chamfer), new Vector2(halfWidth, halfHeight - chamfer),
            new Vector2(halfWidth - chamfer, halfHeight), new Vector2(-halfWidth + chamfer, halfHeight),
            new Vector2(-halfWidth, halfHeight - chamfer), new Vector2(-halfWidth, -halfHeight + chamfer)
        };
        int count = corners.Length * segmentsPerSide;
        Vector3[] vertices = new Vector3[count * 2];
        Vector3[] normals = new Vector3[vertices.Length];
        int[] triangles = new int[count * 6];
        for (int i = 0; i < count; i++)
        {
            int side = i / segmentsPerSide;
            Vector2 p = Vector2.Lerp(corners[side], corners[(side + 1) % corners.Length], (i % segmentsPerSide) / (float)segmentsPerSide);
            vertices[i * 2] = new Vector3(p.x, p.y, 0f);
            vertices[i * 2 + 1] = new Vector3(p.x * (halfWidth - band) / halfWidth, p.y * (halfHeight - band) / halfHeight, 0f);
            // Radial XY normals adapt the player's inverted hull to a flat menu.
            // Back-facing winding is deliberate: the original shader uses Cull Front.
            normals[i * 2] = normals[i * 2 + 1] = new Vector3(p.x / halfWidth, p.y / halfHeight, 0f).normalized;
            int next = (i + 1) % count;
            triangles[i * 6] = i * 2;
            triangles[i * 6 + 1] = next * 2;
            triangles[i * 6 + 2] = i * 2 + 1;
            triangles[i * 6 + 3] = i * 2 + 1;
            triangles[i * 6 + 4] = next * 2;
            triangles[i * 6 + 5] = next * 2 + 1;
        }
        string path = AssetFolder + "/MESH_Menu_ComicFrame.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = "Menu Comic Frame" };
            AssetDatabase.CreateAsset(mesh, path);
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        Bounds bounds = mesh.bounds;
        bounds.Expand(0.26f); // Include the shader's outward displacement in culling.
        mesh.bounds = bounds;
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static Material FoilMaterial(string name, bool nozzle)
    {
        Shader shader = Shader.Find("FLOWSTATE/Graffiti/HolographicFoil");
        if (shader == null)
            throw new InvalidOperationException("Importa GraffitiHolographicFoil.shader antes de reconstruir el menu.");
        string path = AssetFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetColor("_BaseColor", nozzle ? Hex("62686A") : Hex("182127"));
        material.SetColor("_CoolTint", Hex("779B9E"));
        material.SetColor("_WarmTint", Hex("BCA47C"));
        material.SetFloat("_Intensity", nozzle ? 0.38f : 0.3f);
        material.SetFloat("_Speed", nozzle ? 0.35f : 0.27f);
        material.SetFloat("_SurfaceMode", nozzle ? 1f : 0f);
        material.SetFloat("_HalftoneStrength", nozzle ? 0f : 0.28f);
        material.SetFloat("_HalftoneScale", 12f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material MaterialAsset(string name, Color color, bool emission, float metallic, float smoothness)
    {
        string path = AssetFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find(emission
            ? "Universal Render Pipeline/Unlit"
            : "Universal Render Pipeline/Lit");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (emission)
        {
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.8f);
            }
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static Color Hex(string value)
    {
        if (!ColorUtility.TryParseHtmlString("#" + value, out Color color))
            throw new ArgumentException("Invalid hex color: " + value);
        return color;
    }
}
#endif
