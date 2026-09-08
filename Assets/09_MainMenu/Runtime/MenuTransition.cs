using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
namespace FlowState.Menu
{
    public sealed class MenuTransition : MonoBehaviour
    {
        public Renderer inkPlane;
        Material ink;
        Camera overlay;
        public bool IsBusy {get;private set;}
        void Awake()
        {
            ink=new Material(inkPlane.sharedMaterial);inkPlane.sharedMaterial=ink;ink.SetFloat("_Progress",0);
            overlay=GetComponent<Camera>();
            overlay.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Overlay;
            overlay.enabled=false;
        }
        public void Play(MenuOptionData option,GraffitiMenuPainter painter)
        {
            if(IsBusy)return;
            if(!Application.CanStreamedLevelBeLoaded(option.scenePath)) {Debug.LogError("Menu scene is not enabled in Build Settings: "+option.scenePath,this);return;}
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
            ink.SetColor("_Color",option.accentColor*.22f);
            yield return Wipe(0,1,option.transitionDuration);
            var load=SceneManager.LoadSceneAsync(option.scenePath);
            if(load!=null)while(!load.isDone)yield return null;
            // The same pigment is lifted from the gameplay camera after the scene loads.
            yield return null;yield return Wipe(1,0,option.transitionDuration*.75f);
            Destroy(gameObject);
        }
        IEnumerator Wipe(float from,float to,float duration)
        {
            float t=0;
            while(t<1){t+=Time.unscaledDeltaTime/Mathf.Max(.1f,duration);ink.SetFloat("_Progress",Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,t)));yield return null;}
            ink.SetFloat("_Progress",to);
        }
        void OnDestroy(){SceneManager.sceneLoaded-=Attach;if(ink)Destroy(ink);}
    }
}
