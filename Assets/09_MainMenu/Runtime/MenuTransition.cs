using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
namespace FlowState.Menu
{
    public sealed class MenuTransition : MonoBehaviour
    {
        public Renderer inkPlane;
        [Header("Loading presentation")]
        [Min(.25f)] public float minimumLoadingSeconds=12f;
        [Min(0f)] public float completedHoldSeconds=.5f;
        Material ink;
        Camera overlay;
        public bool IsBusy {get;private set;}
        static readonly int LoadingModeId=Shader.PropertyToID("_LoadingMode");
        static readonly int LoadingProgressId=Shader.PropertyToID("_LoadProgress");
        static readonly int LoadingStartTimeId=Shader.PropertyToID("_LoadingStartTime");
        static readonly int BackdropMaskId=Shader.PropertyToID("_BackdropMask");
        static readonly int BackdropParallaxId=Shader.PropertyToID("_BackdropParallax");
        void Awake()
        {
            ink=new Material(inkPlane.sharedMaterial);inkPlane.sharedMaterial=ink;ink.SetFloat("_Progress",0);
            ink.SetFloat(LoadingModeId,0);ink.SetFloat(LoadingProgressId,0);
            overlay=GetComponent<Camera>();
            overlay.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Overlay;
            overlay.enabled=false;
        }
        public void Play(MenuOptionData option,GraffitiMenuPainter painter)
        {
            if(IsBusy)return;
            if(!Application.CanStreamedLevelBeLoaded(option.scenePath)) {Debug.LogError("Menu scene is not enabled in Build Settings: "+option.scenePath,this);return;}
            CopyLivingBackdrop();
            IsBusy=true;painter.Save();DontDestroyOnLoad(gameObject);
            Attach(SceneManager.GetActiveScene(),LoadSceneMode.Single);
            SceneManager.sceneLoaded+=Attach;
            StartCoroutine(Load(option));
        }
        void Attach(Scene scene,LoadSceneMode mode)
        {
            var camera=Camera.main;
            if(!camera||camera==overlay)return;
            var stack=camera.GetUniversalAdditionalCameraData().cameraStack;
            if(stack!=null&&!stack.Contains(overlay))stack.Add(overlay);
            overlay.enabled=true;
        }
        IEnumerator Load(MenuOptionData option)
        {
            Color deepInk=new Color(.035f,.01f,.075f,1);
            ink.SetColor("_Color",deepInk);
            ink.SetColor("_AccentColor",Color.Lerp(option.accentColor,Color.white,.08f));
            ink.SetFloat("_Seed",Random.Range(3f,91f));
            yield return Wipe(0,1,option.transitionDuration);
            ink.SetFloat(LoadingStartTimeId,Time.time);
            yield return BlendLoading(0,1,.2f);
            var load=SceneManager.LoadSceneAsync(option.scenePath);
            if(load!=null)
            {
                load.allowSceneActivation=false;
                float elapsed=0;
                while(elapsed<minimumLoadingSeconds||load.progress<.9f)
                {
                    elapsed+=Time.unscaledDeltaTime;
                    float sceneProgress=Mathf.Clamp01(load.progress/.9f);
                    float presentationProgress=Mathf.Clamp01(elapsed/Mathf.Max(.25f,minimumLoadingSeconds));
                    ink.SetFloat(LoadingProgressId,Mathf.Min(sceneProgress,presentationProgress));
                    UpdateLoadingParallax();
                    yield return null;
                }
                ink.SetFloat(LoadingProgressId,1);
                if(completedHoldSeconds>0)yield return new WaitForSecondsRealtime(completedHoldSeconds);
                load.allowSceneActivation=true;
                while(!load.isDone){UpdateLoadingParallax();yield return null;}
            }
            // Keep one rendered gameplay frame safely behind the opaque pigment.
            yield return new WaitForEndOfFrame();
            yield return BlendLoading(1,0,.18f);
            yield return Wipe(1,0,option.transitionDuration*.9f);
            Destroy(gameObject);
        }
        void CopyLivingBackdrop()
        {
            var motion=FindFirstObjectByType<MenuBackdropMotion>();
            if(!motion||!motion.wall||!motion.wall.sharedMaterial)return;
            var source=motion.wall.sharedMaterial;
            if(source.HasProperty(BackdropMaskId))ink.SetTexture(BackdropMaskId,source.GetTexture(BackdropMaskId));
        }
        IEnumerator BlendLoading(float from,float to,float duration)
        {
            float elapsed=0;
            while(elapsed<duration)
            {
                elapsed+=Time.unscaledDeltaTime;
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration)));
                ink.SetFloat(LoadingModeId,Mathf.Lerp(from,to,t));
                UpdateLoadingParallax();
                yield return null;
            }
            ink.SetFloat(LoadingModeId,to);
        }
        void UpdateLoadingParallax()
        {
            if(!ink)return;
            Vector2 pointer=new Vector2(
                Screen.width>0?(Input.mousePosition.x/Screen.width-.5f)*2f:0,
                Screen.height>0?(Input.mousePosition.y/Screen.height-.5f)*2f:0);
            ink.SetVector(BackdropParallaxId,new Vector4(pointer.x,pointer.y,0,0));
        }
        IEnumerator Wipe(float from,float to,float duration)
        {
            float t=0;
            while(t<1)
            {
                t+=Time.unscaledDeltaTime/Mathf.Max(.1f,duration);
                float normalized=Mathf.Clamp01(t);
                float eased=to>from?1-Mathf.Pow(1-normalized,3):Mathf.SmoothStep(0,1,normalized);
                ink.SetFloat("_Progress",Mathf.Lerp(from,to,eased));
                UpdateLoadingParallax();
                yield return null;
            }
            ink.SetFloat("_Progress",to);
        }
        void OnDestroy()
        {
            SceneManager.sceneLoaded-=Attach;
            if(overlay)
            {
                foreach(var camera in Camera.allCameras)
                {
                    if(!camera||camera==overlay)continue;
                    var stack=camera.GetUniversalAdditionalCameraData().cameraStack;
                    if(stack!=null)stack.Remove(overlay);
                }
            }
            if(ink)Destroy(ink);
        }
    }
}
