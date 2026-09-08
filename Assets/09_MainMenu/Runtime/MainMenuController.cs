using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace FlowState.Menu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public MenuOptionData[] options;
        public MenuInput input;
        public CanMotion motion;
        public CanVisualState visual;
        public GraffitiMenuPainter painter;
        public MenuAudio audioFeedback;
        public MenuCameraFeedback cameraFeedback;
        public MenuSpray spray;
        public MenuTransition transition;
        public MenuSubmenu submenu;
        public TMP_Text legend,exitTag;
        public GameObject previewWord;
        readonly Queue<int> queue=new Queue<int>(4);
        enum Phase{Ready,Turning,Painting,Leaving}
        Phase phase;
        bool pendingConfirm,exitArmed;
        float exitDeadline;
        CursorLockMode previousLock;
        bool previousCursor;
        bool previousBackground;
        public int SelectedIndex {get;private set;}
        public int QueuedInputs=>queue.Count;
        public bool IsReady=>phase==Phase.Ready;
        public string SelectedName=>options[SelectedIndex].displayName;
        void OnEnable(){input.Navigate+=Navigate;input.Confirm+=Confirm;input.Back+=Back;}
        void Start()
        {
            if(options==null||options.Length!=4){Debug.LogError("Main menu needs four option assets.",this);enabled=false;return;}
            previousLock=Cursor.lockState;previousCursor=Cursor.visible;
            previousBackground=Application.runInBackground;Application.runInBackground=true;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
            if(previewWord)previewWord.SetActive(false);
            exitTag.gameObject.SetActive(false);
            visual.Apply(options[0],.01f);Paint();
        }
        public void Navigate(int direction)
        {
            if(direction==0||phase==Phase.Leaving)return;
            if(submenu.IsOpen){submenu.Navigate(direction);return;}
            DisarmExit();pendingConfirm=false;
            // Bounded queue retains chronological taps; once full the latest intent wins.
            if(queue.Count==4)queue.Dequeue();queue.Enqueue(direction>0?1:-1);
            if(phase==Phase.Ready)Next();
        }
        void Next()
        {
            if(queue.Count==0)return;
            int direction=queue.Dequeue();SelectedIndex=GraffitiPlacement.Wrap(SelectedIndex+direction,options.Length);
            var option=options[SelectedIndex];phase=Phase.Turning;
            motion.Turn(direction,option.turnDuration);
            visual.Apply(option,option.turnDuration*.85f);
            audioFeedback.Turn(option);cameraFeedback.Impulse(direction*option.cameraImpulse);
        }
        void Paint()
        {
            var option=options[SelectedIndex];phase=Phase.Painting;
            painter.Begin(option,SelectedIndex);motion.Spray();spray.Fire(option);
            audioFeedback.Spray(option);cameraFeedback.Impulse(-.22f*option.cameraImpulse);
            legend.text="←  →   GIRAR     ENTER / A   PINTAR EL CAMINO";
        }
        void Update()
        {
            if(exitArmed&&Time.unscaledTime>exitDeadline)DisarmExit();
            if(phase==Phase.Turning&&!motion.IsTurning)Paint();
            else if(phase==Phase.Painting&&!painter.IsPainting)
            {
                audioFeedback.StopSpray();phase=Phase.Ready;
                if(queue.Count>0)Next();else if(pendingConfirm){pendingConfirm=false;Confirm();}
            }
        }
        public void Confirm()
        {
            if(phase==Phase.Leaving)return;
            if(submenu.IsOpen){submenu.Confirm();return;}
            if(phase!=Phase.Ready||queue.Count>0){pendingConfirm=true;return;}
            var option=options[SelectedIndex];
            if(option.action==MenuAction.Quit)
            {
                if(!exitArmed){exitArmed=true;exitDeadline=Time.unscaledTime+5;exitTag.text="¿ÚLTIMA MARCA?\nENTER / A  SALIR    ESC / B  SEGUIR";exitTag.gameObject.SetActive(true);motion.Spray();audioFeedback.Spray(option);return;}
                painter.Save();
                #if UNITY_EDITOR
                Debug.Log("FLOW STATE: exit confirmed (player would quit). Editor remains open.");DisarmExit();
                #else
                Application.Quit();
                #endif
                return;
            }
            option.onConfirmed?.Invoke();
            if(option.action==MenuAction.Play){motion.Spray();audioFeedback.Spray(option);transition.Play(option,painter);if(transition.IsBusy)phase=Phase.Leaving;}
            else if(option.action==MenuAction.Gallery||option.action==MenuAction.Settings)submenu.Open(option.action);
        }
        public void Back()
        {
            pendingConfirm=false;queue.Clear();DisarmExit();
            if(submenu.IsOpen)submenu.Close();
        }
        void DisarmExit(){exitArmed=false;exitTag.gameObject.SetActive(false);}
        void OnDisable()
        {
            input.Navigate-=Navigate;input.Confirm-=Confirm;input.Back-=Back;
            queue.Clear();pendingConfirm=false;
            Cursor.lockState=previousLock;Cursor.visible=previousCursor;
            Application.runInBackground=previousBackground;
        }
    }
}
