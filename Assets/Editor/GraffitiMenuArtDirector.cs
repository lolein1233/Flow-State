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

    static readonly Color Ink = Hex("111018");
    static readonly Color Plum = Hex("291324");
    static readonly Color Paper = Hex("EEE9DC");
    static readonly Color Cyan = Hex("00E5FF");
    static readonly Color Magenta = Hex("FF1478");
    static readonly Color Acid = Hex("D7FF00");
    static readonly Color Orange = Hex("FF6A00");
    static readonly Color Violet = Hex("703BFF");
    static readonly Color Coral = Hex("FF3D34");
    static readonly Color Mint = Hex("19F2BE");
    static readonly Color Steel = Hex("777B8B");

    [MenuItem("Flow State/Graffiti Menu/Build Neo Spray Lab")]
    public static void Build()
    {
        EnsureFolder(AssetFolder);

        Material ink = MaterialAsset("M_Menu_Ink", Ink, false, 0f, 0.18f);
        Material plum = MaterialAsset("M_Menu_Plum", Plum, false, 0f, 0.2f);
        Material paper = MaterialAsset("M_Menu_Paper", Paper, false, 0f, 0.1f);
        Material cyan = MaterialAsset("M_Menu_Cyan", Cyan, true, 0f, 0.26f);
        Material magenta = MaterialAsset("M_Menu_Magenta", Magenta, true, 0f, 0.25f);
        Material acid = MaterialAsset("M_Menu_Acid", Acid, true, 0f, 0.2f);
        Material orange = MaterialAsset("M_Menu_Orange", Orange, true, 0f, 0.3f);
        Material violet = MaterialAsset("M_Menu_Violet", Violet, true, 0f, 0.3f);
        Material coral = MaterialAsset("M_Menu_Coral", Coral, true, 0f, 0.26f);
        Material mint = MaterialAsset("M_Menu_Mint", Mint, true, 0f, 0.24f);
        Material steel = MaterialAsset("M_Nozzle_Steel", Steel, false, 0.78f, 0.56f);
        Material blackMetal = MaterialAsset("M_Nozzle_Black", Hex("17171E"), false, 0.62f, 0.4f);
        steel.EnableKeyword("_EMISSION");
        steel.SetColor("_EmissionColor", Steel * 0.32f);
        EditorUtility.SetDirty(steel);

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
            BuildBackdrop(root.transform, art, ink, plum, paper, cyan, magenta, acid, orange, violet, coral, mint);
            BuildColorLab(root.transform, art, ink, paper, cyan, magenta, acid);
            BuildNozzleRack(root.transform, art, ink, paper, steel, blackMetal, cyan, magenta, acid, orange, violet);

            GraffitiDrawMenu drawMenu = root.GetComponent<GraffitiDrawMenu>();
            if (drawMenu != null)
            {
                drawMenu.visualRoot = root.transform;
                drawMenu.drawTime = 0.48f;
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

    static void BuildBackdrop(Transform root, Transform art, Material ink, Material plum, Material paper, Material cyan, Material magenta, Material acid, Material orange, Material violet, Material coral, Material mint)
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
        CreateBlock("Magenta torn edge", art, new Vector3(-1.43f, 0.08f, -0.015f), new Vector3(0.1f, 1.36f, 0.025f), new Vector3(0f, 0f, -7f), magenta);
        CreateBlock("Cyan torn edge", art, new Vector3(1.43f, -0.08f, -0.016f), new Vector3(0.08f, 1.28f, 0.026f), new Vector3(0f, 0f, 5f), cyan);
        CreateBlock("Acid header tape", art, new Vector3(-0.58f, 0.685f, -0.045f), new Vector3(1.58f, 0.13f, 0.022f), new Vector3(0f, 0f, -1.8f), acid);
        CreateBlock("Orange index", art, new Vector3(1.20f, 0.66f, -0.047f), new Vector3(0.30f, 0.11f, 0.022f), new Vector3(0f, 0f, 4f), orange);
        BuildReferenceMotifs(art, ink, paper, cyan, magenta, acid, orange, violet, coral, mint);

        CreateText("Title", art, "FLOW STATE", new Vector3(-1.31f, 0.69f, -0.085f), new Vector2(1.45f, 0.24f), 1.25f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        CreateText("Spray Lab", art, "SPRAY LAB", new Vector3(0.25f, 0.675f, -0.087f), new Vector2(0.70f, 0.20f), 0.72f, Magenta, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateText("Index", art, "FS.02", new Vector3(1.06f, 0.655f, -0.087f), new Vector2(0.28f, 0.18f), 0.5f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
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

    static void BuildColorLab(Transform root, Transform art, Material ink, Material paper, Material cyan, Material magenta, Material acid)
    {
        Transform wheel = root.Find("RGB_ColorWheel");
        if (wheel != null)
        {
            wheel.localPosition = new Vector3(-0.94f, 0.14f, -0.085f);
            wheel.localScale = Vector3.one * 0.50f;
            CreateBlock("Wheel card", art, new Vector3(-0.94f, 0.15f, -0.035f), new Vector3(0.76f, 0.61f, 0.028f), new Vector3(0f, 0f, -2f), paper);
            CreateBlock("Wheel shadow", art, new Vector3(-0.90f, 0.11f, -0.02f), new Vector3(0.79f, 0.62f, 0.02f), new Vector3(0f, 0f, 1.5f), magenta);
            CreateBlock("Wheel label tape", art, new Vector3(-0.94f, 0.49f, -0.065f), new Vector3(0.72f, 0.115f, 0.018f), new Vector3(0f, 0f, 1.5f), acid);
            CreateText("Wheel label", art, "COLOR / PIGMENT", new Vector3(-1.28f, 0.488f, -0.095f), new Vector2(0.68f, 0.11f), 0.42f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
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
            CreateBlock("Preview frame", art, new Vector3(0.02f, 0.23f, -0.047f), new Vector3(0.44f, 0.44f, 0.026f), new Vector3(0f, 0f, 3f), acid);
            CreateText("Preview label", art, "LIVE INK", new Vector3(-0.18f, -0.055f, -0.09f), new Vector2(0.42f, 0.1f), 0.38f, Paper, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        CreateBlock("Lab divider", art, new Vector3(0.35f, 0.17f, -0.035f), new Vector3(0.025f, 0.67f, 0.02f), new Vector3(0f, 0f, 8f), cyan);
        CreateBlock("Rack banner", art, new Vector3(0.84f, 0.435f, -0.055f), new Vector3(0.96f, 0.15f, 0.022f), new Vector3(0f, 0f, -2f), magenta);
        CreateText("Rack label", art, "BOQUILLAS / 3D CAP RACK", new Vector3(0.44f, 0.43f, -0.09f), new Vector2(0.88f, 0.14f), 0.45f, Paper, TextAlignmentOptions.Left, FontStyles.Bold);
    }

    static void BuildNozzleRack(Transform root, Transform art, Material ink, Material paper, Material steel, Material blackMetal, Material cyan, Material magenta, Material acid, Material orange, Material violet)
    {
        string[] names = { "Boquilla_Needle", "Boquilla_Soft", "Boquilla_FatCap", "Boquilla_Chisel", "Boquilla_Splatter" };
        string[] titles = { "PEQUEÑA", "DIFUMINAR", "GRANDE", "TRAZO", "MEDIANA" };
        string[] descriptors = { "PRECISION", "NUBE SUAVE", "COBERTURA", "LINEA PLANA", "PULSO MEDIO" };
        string[] sourceParts = { "tripo_part_0", "tripo_part_1", "tripo_part_4", "tripo_part_2", "tripo_part_3" };
        Color[] colors = { Cyan, Violet, Magenta, Orange, Acid };
        Material[] accents = { cyan, violet, magenta, orange, acid };
        float[] xs = { -1.08f, -0.54f, 0f, 0.54f, 1.08f };

        for (int i = 0; i < names.Length; i++)
        {
            Transform button = root.Find(names[i]);
            if (button == null)
                continue;

            button.localPosition = new Vector3(xs[i], -0.42f, -0.07f);
            button.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -1.5f : 1.5f);
            button.localScale = Vector3.one;

            MeshRenderer legacyRenderer = button.GetComponent<MeshRenderer>();
            Material strokeMaterial = legacyRenderer != null ? legacyRenderer.sharedMaterial : null;
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
            CreateBlock("Card shadow", cardRoot, new Vector3(0.025f, -0.018f, 0.045f), new Vector3(0.43f, 0.54f, 0.025f), new Vector3(0f, 0f, -3f), accents[i]);
            CreateBlock("Card", cardRoot, new Vector3(0f, 0f, 0.025f), new Vector3(0.41f, 0.53f, 0.028f), new Vector3(0f, 0f, i % 2 == 0 ? 1.5f : -1.5f), i == 2 ? paper : ink);
            Renderer halo = CreateBlock("Hover halo", cardRoot, new Vector3(0f, 0.075f, -0.005f), new Vector3(0.34f, 0.29f, 0.015f), Vector3.zero, accents[i]).GetComponent<Renderer>();

            if (strokeMaterial != null)
            {
                Transform sample = CreatePrimitive("Stroke sample", PrimitiveType.Quad, cardRoot, new Vector3(0.085f, 0.105f, -0.036f), new Vector3(0.27f, 0.18f, 1f), new Vector3(0f, 180f, i * 8f), strokeMaterial);
                Renderer sampleRenderer = sample.GetComponent<Renderer>();
                if (sampleRenderer != null)
                    sampleRenderer.sortingOrder = 1;
            }

            Transform modelPivot = NewRoot("Physical cap", cardRoot);
            modelPivot.localPosition = new Vector3(-0.055f, 0.095f, -0.105f);
            modelPivot.localRotation = Quaternion.Euler(-8f, -24f + i * 12f, -4f + i * 2f);
            modelPivot.localScale = Vector3.one;
            BuildImportedNozzleModel(sourceParts[i], modelPivot, steel, blackMetal, accents[i]);

            TMP_Text title = CreateText("Name", cardRoot, titles[i], new Vector3(-0.18f, -0.145f, -0.075f), new Vector2(0.36f, 0.11f), 0.43f, i == 2 ? Ink : colors[i], TextAlignmentOptions.Center, FontStyles.Bold);
            TMP_Text descriptor = CreateText("Use", cardRoot, descriptors[i], new Vector3(-0.18f, -0.245f, -0.075f), new Vector2(0.36f, 0.09f), 0.28f, i == 2 ? Ink : Paper, TextAlignmentOptions.Center, FontStyles.Bold);

            GraffitiNozzleVisual visual = button.GetComponent<GraffitiNozzleVisual>();
            if (visual == null)
                visual = button.gameObject.AddComponent<GraffitiNozzleVisual>();

            SerializedObject serialized = new SerializedObject(visual);
            serialized.FindProperty("modelPivot").objectReferenceValue = modelPivot;
            serialized.FindProperty("haloRenderer").objectReferenceValue = halo;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("descriptor").objectReferenceValue = descriptor;
            serialized.FindProperty("idleColor").colorValue = Color.Lerp(Steel, colors[i], 0.28f);
            serialized.FindProperty("activeColor").colorValue = colors[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        CreateText("Rack instruction", art, "APUNTA + E  /  ELIGE TU HUELLA", new Vector3(0.43f, 0.31f, -0.085f), new Vector2(0.96f, 0.1f), 0.32f, Paper, TextAlignmentOptions.Left, FontStyles.Bold);
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
