using TMPro;
using UnityEngine;
namespace FlowState.Menu
{
    public sealed class MenuSubmenu : MonoBehaviour
    {
        public const float ReducedMotionScale=.6f;
        public GameObject presentation;
        public TMP_Text heading,body;
        public Renderer artwork;
        public Texture[] gallery;
        public CanMotion motion;
        public MenuCameraFeedback cameraFeedback;
        public MenuAudio sound;
        MenuAction mode;
        int selected;
        Material artMaterial;
        public bool IsOpen=>presentation.activeSelf;
        void Awake()
        {
            artMaterial=new Material(artwork.sharedMaterial);artwork.sharedMaterial=artMaterial;
            float savedMotion=PlayerPrefs.GetFloat("FS.Menu.Motion",1);
            float motionScale=savedMotion<.8f?ReducedMotionScale:1;
            motion.motionScale=cameraFeedback.motionScale=motionScale;
            // Older builds stored zero for the UI label "REDUCIDO", which disabled
            // the reactive camera completely. Persist the corrected reduced value.
            if(!Mathf.Approximately(savedMotion,motionScale))PlayerPrefs.SetFloat("FS.Menu.Motion",motionScale);
            AudioListener.volume=PlayerPrefs.GetFloat("FS.Audio.Master",1);
            presentation.SetActive(false);
        }
        public void Open(MenuAction action){mode=action;selected=0;presentation.SetActive(true);Refresh();}
        public void Navigate(int direction){selected=GraffitiPlacement.Wrap(selected+direction,mode==MenuAction.Gallery?Mathf.Max(1,gallery.Length):3);Refresh();}
        public void Confirm()
        {
            if(mode==MenuAction.Gallery){Navigate(1);return;}
            if(selected==0){AudioListener.volume=AudioListener.volume>.99f?0:Mathf.Min(1,AudioListener.volume+.25f);PlayerPrefs.SetFloat("FS.Audio.Master",AudioListener.volume);}
            else if(selected==1){float value=motion.motionScale>.8f?ReducedMotionScale:1;motion.motionScale=cameraFeedback.motionScale=value;PlayerPrefs.SetFloat("FS.Menu.Motion",value);}
            else Close();
            Refresh();
        }
        public void Close(){presentation.SetActive(false);PlayerPrefs.Save();}
        void Refresh()
        {
            bool art=mode==MenuAction.Gallery;artwork.gameObject.SetActive(art&&gallery.Length>0);
            heading.text=art?"GALERÍA":"CONFIG";
            if(art){body.text="ARCHIVO VISUAL  /  "+(selected+1).ToString("00")+"\n\n←  →   EXPLORAR\nESC / B   VOLVER";if(gallery.Length>0)artMaterial.mainTexture=gallery[selected];}
            else body.text=(selected==0?"> ":"  ")+"AUDIO     "+Mathf.RoundToInt(AudioListener.volume*100)+"%\n"+(selected==1?"> ":"  ")+"MOVIMIENTO     "+(motion.motionScale>.5f?"SÍ":"REDUCIDO")+"\n"+(selected==2?"> ":"  ")+"VOLVER\n\n← → ELEGIR    ENTER / A CAMBIAR";
        }
        void OnDestroy(){if(artMaterial)Destroy(artMaterial);}
    }
}
