using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;
namespace FlowState.Menu.Editor
{
    public static class IntroPrototypeBuilder
    {
        const string Folder="Assets/09_MainMenu/IntroPrototype";
        [MenuItem("FLOW STATE/Intro/Install three-trace prototype (once)")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
            var intro=Object.FindFirstObjectByType<FlowStateIntroDirector>();
            if(!intro||intro.gameObject.scene.path!="Assets/02_Escenas/MainMenu_FlowState.unity")
                throw new System.InvalidOperationException("Open the existing main menu scene first.");
            if(intro.prototypePaint)throw new System.InvalidOperationException("Prototype already installed. Edit its Inspector/Timeline; installation will not overwrite it.");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/09_MainMenu","IntroPrototype");
            var paint=Undo.AddComponent<FlowStateIntroPaintController>(intro.gameObject);
            paint.surface=intro.menu.painter.wall;
            paint.brushShader=Shader.Find("Hidden/FlowState/IntroSprayMask");paint.compositeShader=Shader.Find("FlowState/IntroWorldPigment");
            paint.officialLogo=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04_Materiales e Imagenes/imagenes/YakuzaStudio_FLOWSTATE (1).png");
            paint.fragments=Definitions();
            var provider=Undo.AddComponent<FlowStateIntroContentProvider>(intro.gameObject);
            var stage=new GameObject("INTRO CONTENT DIORAMA · temporary");Undo.RegisterCreatedObjectUndo(stage,"Intro prototype");
            stage.transform.position=new Vector3(100,0,0);stage.layer=30;provider.diorama=stage;
            var concrete=MaterialAsset("Concrete",new Color(.23f,.26f,.25f));
            var ink=MaterialAsset("Iron",new Color(.025f,.031f,.035f));
            var amber=MaterialAsset("Faded ochre",new Color(.45f,.22f,.075f));
            Cube(stage.transform,"Concrete wall",new Vector3(0,1.8f,2.6f),new Vector3(11,5,.25f),concrete);
            Cube(stage.transform,"Kerb",new Vector3(0,-.18f,1),new Vector3(11,.35f,5),ink);
            Cube(stage.transform,"Faded painted band",new Vector3(0,1.65f,2.44f),new Vector3(11,.64f,.025f),amber);
            for(int i=0;i<5;i++)Cube(stage.transform,"Steel mullion "+i,new Vector3(-4+i*2,2.3f,2.37f),new Vector3(.12f,4.5f,.18f),ink);
            Cube(stage.transform,"Vent",new Vector3(1.9f,.9f,2.22f),new Vector3(1.4f,.95f,.3f),ink);
            for(int i=0;i<5;i++)Cube(stage.transform,"Vent louvre "+i,new Vector3(1.9f,.55f+i*.15f,2.04f),new Vector3(1.2f,.055f,.08f),concrete);
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LATA/Spray Test update.fbx");
            var can=Object.Instantiate(model,stage.transform);can.name="Existing spray can · animated";
            can.transform.localPosition=Vector3.zero;can.transform.localRotation=Quaternion.Euler(-90,0,0);
            var renderers=can.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            can.transform.localScale*=3.3f/Mathf.Max(.01f,bounds.size.y);
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            can.transform.position+=stage.transform.position+new Vector3(-.55f,1.7f,.25f)-bounds.center;
            foreach(var r in renderers)r.sharedMaterial=intro.menu.visual.canRenderer.sharedMaterial;
            var pivot=new GameObject("Can turntable").transform;pivot.SetParent(stage.transform,false);pivot.position=stage.transform.position+new Vector3(-.55f,1.7f,.25f);
            can.transform.SetParent(pivot,true);provider.animatedCan=pivot;
            var key=new GameObject("Temporary warm key",typeof(Light));key.transform.SetParent(stage.transform,false);
            key.transform.localPosition=new Vector3(-2,3,-2);var light=key.GetComponent<Light>();light.type=LightType.Point;light.range=12;light.intensity=8;light.color=new Color(1,.82f,.65f);light.cullingMask=1<<30;
            var camGO=new GameObject("Intro Content Camera",typeof(Camera));camGO.transform.SetParent(stage.transform,false);
            camGO.transform.localPosition=new Vector3(0,1.8f,-6.8f);camGO.transform.LookAt(stage.transform.position+new Vector3(0,1.65f,1));
            var cam=camGO.GetComponent<Camera>();cam.fieldOfView=38;cam.nearClipPlane=.1f;cam.farClipPlane=18;cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.018f,.023f,.023f);cam.allowHDR=false;cam.allowMSAA=false;cam.enabled=false;cam.depth=-10;
            var data=camGO.AddComponent<UniversalAdditionalCameraData>();data.SetRenderer(0);data.renderPostProcessing=false;data.volumeLayerMask=0;
            provider.contentCamera=cam;
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            stage.SetActive(false);
            var timeline=ScriptableObject.CreateInstance<TimelineAsset>();timeline.name="Six diorama panels · 5.10 seconds";
            AssetDatabase.CreateAsset(timeline,Folder+"/ThreeTraces.playable");
            timeline.durationMode=TimelineAsset.DurationMode.FixedLength;timeline.fixedDuration=5.10;
            var track=timeline.CreateTrack<IntroPaintTrack>(null,"Spray → six diorama panels");
            double[] starts={.45,.90,1.35};
            for(int f=0;f<3;f++)
            {
                var clip=track.CreateClip<IntroPaintClip>();
                var asset=(IntroPaintClip)clip.asset;asset.fragment=f;asset.stage=IntroPaintStage.Reveal;
                clip.start=starts[f];clip.duration=paint.fragments[f].duration;clip.displayName=((char)('A'+f))+" · two panels";
            }
            var push=track.CreateClip<IntroPaintClip>();var pushAsset=(IntroPaintClip)push.asset;
            pushAsset.fragment=0;pushAsset.stage=IntroPaintStage.CameraPush;push.start=4.20;push.duration=.65;push.displayName="Mini Camera Push";
            var director=Undo.AddComponent<PlayableDirector>(intro.gameObject);director.playableAsset=timeline;
            director.playOnAwake=false;director.timeUpdateMode=DirectorUpdateMode.UnscaledGameTime;director.extrapolationMode=DirectorWrapMode.Hold;
            director.SetGenericBinding(track,intro);
            Undo.RecordObject(intro,"Wire intro prototype");intro.prototypePaint=paint;intro.contentProvider=provider;intro.prototypeTimeline=director;
            EditorUtility.SetDirty(intro);EditorUtility.SetDirty(paint);EditorUtility.SetDirty(provider);EditorUtility.SetDirty(director);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(intro.gameObject.scene);EditorSceneManager.SaveScene(intro.gameObject.scene);
        }
        static Material MaterialAsset(string name,Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Intro temporary · "+name};
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.22f);
            AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;
        }
        static void Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        static Vector2[] Points(params float[] p)
        {var a=new Vector2[p.Length/2];for(int i=0;i<a.Length;i++)a[i]=new Vector2(p[i*2],p[i*2+1]);return a;}
        public static IntroFragmentDefinition[] Definitions()
        {
            return new[]{
                new IntroFragmentDefinition {label="Panel pair A · flowing slats",position=new Vector2(.15f,.54f),orientation=-3,
                    treatment=IntroTreatment.CreativeFlow,width=.20f,edgeErosion=.12f,duration=1.55f,overspray=.12f,secondaryDelay=.38f,seed=41,
                    silhouette=Points(-.055f,-.24f,.050f,-.24f,.060f,.20f,-.035f,.24f),
                    path=Points(0,-.26f,-.005f,-.08f,.005f,.08f,.012f,.26f),
                    secondarySilhouette=Points(.10f,-.17f,.165f,-.18f,.175f,.15f,.105f,.18f),
                    secondaryPath=Points(.135f,-.20f,.135f,-.06f,.14f,.08f,.14f,.20f),
                    contentWindow=new Rect(.09f,.28f,.23f,.52f),contentCrop=new Rect(.05f,0,.345f,.95f)},
                new IntroFragmentDefinition {label="Panel pair B · cut centre",position=new Vector2(.45f,.54f),orientation=2,
                    treatment=IntroTreatment.Anger,width=.22f,edgeErosion=.22f,duration=1.45f,overspray=.24f,secondaryDelay=.34f,seed=73,
                    revealCurve=AnimationCurve.Linear(0,0,1,1),
                    silhouette=Points(-.060f,-.27f,.060f,-.23f,.050f,.22f,-.070f,.19f),
                    path=Points(0,-.29f,.005f,-.10f,0,.08f,-.005f,.24f),
                    secondarySilhouette=Points(.10f,-.18f,.165f,-.18f,.155f,.28f,.09f,.24f),
                    secondaryPath=Points(.13f,-.21f,.13f,-.04f,.125f,.12f,.125f,.30f),
                    contentWindow=new Rect(.385f,.25f,.24f,.58f),contentCrop=new Rect(.4925f,0,.36f,.95f)},
                new IntroFragmentDefinition {label="Panel pair C · precise right",position=new Vector2(.73f,.53f),orientation=1,
                    treatment=IntroTreatment.ClarityNeutral,width=.18f,edgeErosion=.06f,duration=1.50f,overspray=.06f,secondaryDelay=.42f,seed=107,
                    silhouette=Points(-.050f,-.21f,.055f,-.21f,.055f,.19f,-.050f,.22f),
                    path=Points(0,-.23f,0,-.07f,.002f,.08f,0,.24f),
                    secondarySilhouette=Points(.10f,-.28f,.17f,-.28f,.175f,.14f,.105f,.16f),
                    secondaryPath=Points(.135f,-.30f,.135f,-.12f,.14f,.03f,.14f,.18f),
                    contentWindow=new Rect(.68f,.24f,.23f,.58f),contentCrop=new Rect(.22f,.06f,.50f,.88f)}
            };
        }
    }
}
