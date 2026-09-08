using System;
using System.Collections.Generic;
using System.Linq;
using FlowState.Rendering;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FlowState.Menu.Editor
{
    public static class MainMenuBuilder
    {
        public const string ScenePath="Assets/02_Escenas/MainMenu_FlowState.unity";
        const string Root="Assets/09_MainMenu";
        const string Fssrs="Assets/07_StylizedRendering/FSSRS";
        static T Asset<T>(string path) where T:UnityEngine.Object
        {
            var a=AssetDatabase.LoadAssetAtPath<T>(path);
            if(!a)throw new InvalidOperationException("Missing required menu asset: "+path);return a;
        }
        static Material Material(string name,string shader)
        {
            var s=Shader.Find(shader);if(!s)throw new InvalidOperationException("Missing shader: "+shader);
            var mat=new Material(s){name=name};AssetDatabase.CreateAsset(mat,Root+"/Materials/"+name+".mat");return mat;
        }
        static GameObject Node(string name,Transform parent=null)
        {
            var go=new GameObject(name);if(parent)go.transform.SetParent(parent,false);return go;
        }
        static GameObject Shape(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material mat,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;
            if(parent)go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static TextMeshPro Text(string name,string text,Vector3 pos,Vector2 box,float size,TMP_FontAsset font,Transform parent)
        {
            var go=Node(name,parent);go.transform.localPosition=pos;
            var t=go.AddComponent<TextMeshPro>();t.font=font;t.text=text;t.fontSize=size;t.color=new Color(.07f,.065f,.075f);
            t.alignment=TextAlignmentOptions.TopLeft;t.rectTransform.sizeDelta=box;t.rectTransform.pivot=new Vector2(0,1);t.textWrappingMode=TextWrappingModes.NoWrap;
            return t;
        }
        static Light Lamp(string name,Vector3 pos,Color color,float intensity,Transform parent)
        {
            var l=Node(name,parent).AddComponent<Light>();l.transform.localPosition=pos;l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=16;return l;
        }
        [MenuItem("FLOW STATE/Main Menu/Create Living Graffiti Scene")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before authoring.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))throw new InvalidOperationException("Menu scene exists. Open it to edit; builder never overwrites it.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save your scene before building the menu.");
            foreach(string folder in new[]{"Materials","Options","Profiles"})if(!AssetDatabase.IsValidFolder(Root+"/"+folder))AssetDatabase.CreateFolder(Root,folder);
            AssetDatabase.Refresh();
            foreach(string file in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Textures"}))
            {
                var imp=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(file));
                imp.sRGBTexture=false;imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
            }
            var model=Asset<GameObject>("Assets/LATA/Spray Test update.fbx");
            var owned=Asset<TMP_FontAsset>("Assets/05_Tipografias/owned SDF.asset");
            var secondary=Asset<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var logo=Asset<Texture2D>("Assets/04_Materiales e Imagenes/imagenes/YakuzaStudio_FLOWSTATE (1).png");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=Node("FLOW STATE · Living Graffiti Menu");
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.62f,.57f);RenderSettings.fog=false;

            var wallMat=Material("LivingWall","FLOWSTATE/Menu/Living Wall");
            wallMat.SetColor("_BaseColor",new Color(.85f,.83f,.76f));
            wallMat.SetTexture("_MainTex",Asset<Texture2D>(Fssrs+"/Textures/Grunge/FS_PrintGrain_01.png"));
            var wall=Shape("Memory Wall · consolidated pigment",PrimitiveType.Quad,new Vector3(0,0,2),new Vector3(16,9,1),wallMat,root.transform);
            var composite=Material("PaintAccumulator","FLOWSTATE/Menu/Paint Composite");
            var painter=root.AddComponent<GraffitiMenuPainter>();painter.wall=wall.GetComponent<Renderer>();painter.compositeTemplate=composite;painter.seed=0;

            var dark=Material("StructuralInk","Universal Render Pipeline/Unlit");dark.color=new Color(.06f,.055f,.06f);
            Shape("Top rail",PrimitiveType.Cube,new Vector3(0,4.2f,1.8f),new Vector3(16,.06f,.04f),dark,root.transform);
            Shape("Bottom rail",PrimitiveType.Cube,new Vector3(0,-3.92f,1.8f),new Vector3(16,.025f,.04f),dark,root.transform);
            Shape("Left steel seam",PrimitiveType.Cube,new Vector3(-7.15f,0,1.85f),new Vector3(.025f,8.4f,.04f),dark,root.transform);
            var slash=Shape("Black registration slash",PrimitiveType.Quad,new Vector3(5.85f,2.6f,1.8f),new Vector3(.045f,4,1),dark,root.transform);slash.transform.localEulerAngles=new Vector3(0,0,-33);
            var logoMat=Material("OfficialLogo","Universal Render Pipeline/Unlit");logoMat.mainTexture=logo;
            logoMat.SetFloat("_AlphaClip",1);logoMat.EnableKeyword("_ALPHATEST_ON");logoMat.SetFloat("_Cutoff",.08f);
            var logoGO=Shape("YakuzaStudio · official FLOWSTATE print",PrimitiveType.Quad,new Vector3(-3.3f,2.65f,1.7f),new Vector3(5.1f,1.8f,1),logoMat,root.transform);
            logoGO.transform.localEulerAngles=new Vector3(0,0,-4);
            Text("Print edition","YAKUZA STUDIO   /   FLOW STATE",new Vector3(-6.2f,3.65f,1.6f),new Vector2(7,.4f),2.2f,secondary,root.transform);
            Text("Wall caption","LA INDECISIÓN PRODUCE ARTE.",new Vector3(-6.2f,-2.78f,1.6f),new Vector2(8,.5f),2.4f,secondary,root.transform);
            var legend=Text("Input legend","←  →   GIRAR     ENTER / A   PINTAR EL CAMINO",new Vector3(-6.2f,-3.28f,1.6f),new Vector2(10,.5f),2.1f,secondary,root.transform);
            var exit=Text("Last mark confirmation","",new Vector3(-5.8f,-1.6f,1.45f),new Vector2(9,1.1f),2.8f,secondary,root.transform);
            var preview=Text("Editor preview · replaced by actual spray in Play","JUGAR",new Vector3(-5.9f,.6f,1.5f),new Vector2(7,2),15,owned,root.transform);

            var cameraGO=Node("Menu Camera",root.transform);cameraGO.tag="MainCamera";
            cameraGO.transform.position=new Vector3(0,0,-10);
            var camera=cameraGO.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.84f,.82f,.75f);camera.fieldOfView=38;camera.nearClipPlane=.1f;camera.farClipPlane=60;camera.cullingMask=~(1<<31);
            cameraGO.AddComponent<AudioListener>();var cameraData=camera.GetUniversalAdditionalCameraData();cameraData.SetRenderer(1);cameraData.renderPostProcessing=true;cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var feedback=cameraGO.AddComponent<MenuCameraFeedback>();feedback.menuCamera=camera;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/Profiles/MenuPrint.asset");
            var fx=profile.Add<FSSRSVolumeComponent>(true);fx.outlineIntensity.Override(.78f);fx.outlineThickness.Override(1.2f);fx.halftoneIntensity.Override(.12f);fx.hatchIntensity.Override(.08f);fx.paletteInfluence.Override(.11f);fx.paperLift.Override(.05f);fx.colorSaturation.Override(.05f);fx.accentBoost.Override(.06f);fx.inkFleckIntensity.Override(.012f);
            AssetDatabase.AddObjectToAsset(fx,profile);
            ConfigureOptics(profile);
            var vol=Node("Menu Print Volume",root.transform).AddComponent<Volume>();vol.isGlobal=true;vol.sharedProfile=profile;
            var palettes=root.AddComponent<FlowStatePaletteController>();
            var neutral=Asset<FlowPaletteProfile>(Fssrs+"/Palettes/FP_Neutral_Unfinished.asset");
            var doubt=Asset<FlowPaletteProfile>(Fssrs+"/Palettes/FP_Doubt.asset");
            var anger=Asset<FlowPaletteProfile>(Fssrs+"/Palettes/FP_Anger.asset");
            var flow=Asset<FlowPaletteProfile>(Fssrs+"/Palettes/FP_CreativeFlow.asset");
            var clarity=Asset<FlowPaletteProfile>(Fssrs+"/Palettes/FP_Clarity.asset");
            palettes.Configure(flow,null,vol);palettes.ConfigureEmotionPalettes(neutral,doubt,anger,flow,clarity,FlowEmotion.CreativeFlow);

            var canRig=Node("Can · inertia pivot",root.transform);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);instance.transform.SetParent(canRig.transform,false);
            var canRenderer=instance.GetComponentInChildren<Renderer>();
            Bounds bounds=canRenderer.bounds;float factor=7.5f/bounds.size.y;
            instance.transform.localPosition-=bounds.center;instance.transform.localScale*=factor;instance.transform.localPosition*=factor;
            var canMat=Material("OfficialCan_FSSRS","FLOWSTATE/FSSRS/Stylized Lit");
            canMat.SetTexture("_BaseMap",Asset<Texture2D>("Assets/LATA/Spray_Test_update_Spray_BaseColor.png"));
            canMat.SetTexture("_BumpMap",Asset<Texture2D>("Assets/LATA/Spray_Test_update_Spray_Normal_OpenGL.png"));canMat.EnableKeyword("_NORMALMAP");
            canMat.SetTexture("_InkTexture",Asset<Texture2D>(Fssrs+"/Textures/Ink/FS_InkBreakup_01.png"));canMat.SetTexture("_HatchTexture",Asset<Texture2D>(Fssrs+"/Textures/Hatching/FS_Hatching_01.png"));
            canMat.SetFloat("_PaletteInfluence",.88f);canMat.SetFloat("_HalftoneStrength",.4f);canMat.SetFloat("_HalftoneScale",3);canMat.SetFloat("_RimStrength",.12f);canMat.SetFloat("_BandCount",4);canMat.SetFloat("_HatchStrength",.35f);canMat.SetFloat("_AmbientStrength",.28f);canMat.SetFloat("_LightColorStrength",.15f);canRenderer.sharedMaterial=canMat;
            canRig.transform.localScale=Vector3.one*2.3f;
            canRig.transform.localPosition=new Vector3(4,-5.9f,-1);canRig.transform.localEulerAngles=new Vector3(8,12,7);
            var nozzle=Node("Nozzle · emission anchor",canRig.transform);nozzle.transform.localPosition=new Vector3(0,3.48f,-.16f);
            var motion=root.AddComponent<CanMotion>();motion.can=canRig.transform;motion.nozzle=nozzle.transform;
            var key=Node("Key · graphic daylight",root.transform).AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.8f;key.transform.rotation=Quaternion.Euler(35,-35,0);key.shadows=LightShadows.Soft;
            var accent=Lamp("Pigment bounce",new Vector3(5,2,-4),new Color(1,.6f,.12f),4,root.transform);
            Lamp("Cold metal edge",new Vector3(-1,3,-2),new Color(.4f,.7f,1),3,root.transform);
            var visual=root.AddComponent<CanVisualState>();visual.paletteController=palettes;visual.canRenderer=canRenderer;visual.accentLight=accent;

            var mistGO=Node("Aerosol · bounded mist",root.transform);var ps=mistGO.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.startLifetime=.27f;main.startSpeed=12;main.startSize=new ParticleSystem.MinMaxCurve(.015f,.075f);main.maxParticles=128;main.simulationSpace=ParticleSystemSimulationSpace.World;main.useUnscaledTime=true;
            var emission=ps.emission;emission.rateOverTime=120;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=5;shape.radius=.016f;
            var overLife=ps.colorOverLifetime;overLife.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.7f,0),new GradientAlphaKey(0,1)});overLife.color=gradient;
            var particleMat=Material("SprayMist","Universal Render Pipeline/Particles/Unlit");particleMat.SetFloat("_Surface",1);particleMat.SetFloat("_Blend",0);particleMat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);particleMat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);particleMat.SetFloat("_ZWrite",0);particleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particleMat.renderQueue=3000;particleMat.mainTexture=Asset<Texture2D>("Assets/04_Materiales e Imagenes/GraffitiTools/Nozzle_SoftRound.png");ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=particleMat;
            var spray=mistGO.AddComponent<MenuSpray>();spray.mist=ps;spray.nozzle=nozzle.transform;spray.painter=painter;
            var audio=root.AddComponent<MenuAudio>();audio.metal=Node("Can Foley",root.transform).AddComponent<AudioSource>();audio.aerosol=Node("Aerosol pressure",root.transform).AddComponent<AudioSource>();audio.music=Node("Neon Dust · menu bed",root.transform).AddComponent<AudioSource>();
            foreach(var source in new[]{audio.metal,audio.aerosol,audio.music}){source.playOnAwake=false;source.spatialBlend=0;}
            audio.music.clip=Asset<AudioClip>("Assets/08_Musica/Neon Dust.mp3");audio.music.loop=true;audio.music.volume=.13f;audio.music.playOnAwake=true;

            var opts=new MenuOptionData[4];string[] words={"JUGAR","GALERÍA","CONFIG","SALIR"};var emotions=new[]{FlowEmotion.CreativeFlow,FlowEmotion.Anger,FlowEmotion.Clarity,FlowEmotion.Neutral};var profiles=new[]{flow,anger,clarity,neutral};
            var accents=new[]{new Color(1,.55f,.04f),new Color(.93f,.15f,.10f),new Color(.24f,.32f,.88f),new Color(.22f,.25f,.26f)};
            for(int i=0;i<4;i++)
            {
                var o=ScriptableObject.CreateInstance<MenuOptionData>();o.optionId=words[i];o.displayName=words[i];o.action=(MenuAction)i;o.visualState=emotions[i];o.palette=profiles[i];o.accentColor=accents[i];o.wordMask=Asset<Texture2D>(Root+"/Textures/"+i.ToString("00")+"_"+words[i]+".png");o.paintColor=new Color(.035f,.03f,.045f);
                o.rattle=Asset<AudioClip>(Root+"/Audio/Can_Rattle.wav");o.clack=Asset<AudioClip>(Root+"/Audio/Can_Clack.wav");o.spray=Asset<AudioClip>(Root+"/Audio/Can_Spray.wav");o.grain=i==1?.38f:.16f;o.drips=i==1?.95f:.4f;o.halftone=i==3?.65f:.3f;o.pressure=i==1?1:.7f;o.pitch=i==1?1.15f:1;o.turnDuration=i==1?.53f:.65f;
                AssetDatabase.CreateAsset(o,Root+"/Options/"+i.ToString("00")+"_"+words[i]+".asset");opts[i]=o;
            }
            painter.options=opts;
            var input=root.AddComponent<MenuInput>();input.actions=Asset<InputActionAsset>("Assets/InputSystem_Actions.inputactions");

            var transitionRoot=Node("Paint invasion · survives scene load");transitionRoot.transform.position=new Vector3(10000,10000,10000);
            var overlayCamera=transitionRoot.AddComponent<Camera>();overlayCamera.orthographic=true;overlayCamera.orthographicSize=1;overlayCamera.nearClipPlane=.1f;overlayCamera.farClipPlane=2;overlayCamera.depth=100;overlayCamera.clearFlags=CameraClearFlags.Depth;overlayCamera.cullingMask=1<<31;overlayCamera.GetUniversalAdditionalCameraData().SetRenderer(0);
            var inkMat=Material("PaintInvasion","FLOWSTATE/Menu/Ink Transition");
            var ink=Shape("Lens pigment",PrimitiveType.Quad,new Vector3(0,0,1),new Vector3(12,4,1),inkMat,transitionRoot.transform);ink.layer=31;
            overlayCamera.enabled=false;
            var transition=transitionRoot.AddComponent<MenuTransition>();transition.inkPlane=ink.GetComponent<Renderer>();
            var submenu=root.AddComponent<MenuSubmenu>();submenu.motion=motion;submenu.cameraFeedback=feedback;submenu.sound=audio;
            var sub=Node("Pasted sheets · submenus",root.transform);submenu.presentation=sub;
            var sheetMat=Material("ArchivePaper","Universal Render Pipeline/Unlit");sheetMat.color=new Color(.9f,.87f,.79f);
            var sheet=Shape("Archive sheet",PrimitiveType.Quad,new Vector3(-3,.1f,-.15f),new Vector3(6.3f,5.1f,1),sheetMat,sub.transform);sheet.transform.localEulerAngles=new Vector3(0,0,1.5f);
            submenu.heading=Text("Archive heading","GALERÍA",new Vector3(-5.7f,2.1f,-.2f),new Vector2(6,1),6.8f,owned,sub.transform);
            submenu.body=Text("Archive instructions","",new Vector3(-5.6f,1.05f,-.25f),new Vector2(6,3),2.9f,secondary,sub.transform);
            var artMat=Material("ArchiveArtwork","Universal Render Pipeline/Unlit");
            submenu.artwork=Shape("Original project artwork",PrimitiveType.Quad,new Vector3(-3,-.7f,-.3f),new Vector3(4,2.3f,1),artMat,sub.transform).GetComponent<Renderer>();
            submenu.gallery=new Texture[]{logo,Asset<Texture2D>("Assets/04_Materiales e Imagenes/imagenes/ChatGPT Image 3 may 2026, 09_07_59 p.m..png"),Asset<Texture2D>("Assets/04_Materiales e Imagenes/imagenes/yslogo-removebg-preview.png")};sub.SetActive(false);
            var controller=root.AddComponent<MainMenuController>();controller.options=opts;controller.input=input;controller.motion=motion;controller.visual=visual;controller.painter=painter;controller.audioFeedback=audio;controller.cameraFeedback=feedback;controller.spray=spray;controller.transition=transition;controller.submenu=submenu;controller.legend=legend;controller.exitTag=exit;controller.previewWord=preview.gameObject;
            exit.gameObject.SetActive(false);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            var buildScenes=EditorBuildSettings.scenes.ToList();if(!buildScenes.Any(s=>s.path==ScenePath))buildScenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=buildScenes.ToArray();
            Selection.activeGameObject=root;
            Debug.Log("FLOW STATE main menu authored. Play to paint. Original scenes preserved.");
        }
        public static void ConfigureOptics(VolumeProfile profile)
        {
            // Scene-local overrides: project defaults contain strong chromatic blur and grain.
            Get<ChromaticAberration>(profile).intensity.Override(0);
            Get<FilmGrain>(profile).intensity.Override(.025f);
            Get<Vignette>(profile).intensity.Override(.12f);
            Get<MotionBlur>(profile).intensity.Override(0);
            Get<DepthOfField>(profile).mode.Override(DepthOfFieldMode.Off);
            Get<LensDistortion>(profile).intensity.Override(0);
            Get<ColorAdjustments>(profile).postExposure.Override(0);
            Get<ColorAdjustments>(profile).contrast.Override(5);
            Get<ColorAdjustments>(profile).saturation.Override(0);
            Get<Bloom>(profile).intensity.Override(.08f);
            EditorUtility.SetDirty(profile);
        }
        static T Get<T>(VolumeProfile profile) where T:VolumeComponent
        {
            if(profile.TryGet<T>(out var c))return c;
            c=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(c,profile);return c;
        }
    }
}
