using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;

namespace FlowState.Menu
{
    public enum IntroMode { Full, Short, Disabled }
    public enum IntroPhase { Preparing, Painting, Composition, LogoReveal, CameraPush, WorldReveal, Handoff, MenuReady }

    [DisallowMultipleComponent]
    public sealed class FlowStateIntroDirector : MonoBehaviour
    {
        public MainMenuController menu;
        public MenuInput input;
        public FlowStateMenuCameraController cameraController;
        [Tooltip("Full/Short play the three-trace prototype and hold its last image. Skip enters the menu.")]
        public IntroMode mode=IntroMode.Disabled;
        [Tooltip("Temporary debug route: immediately complete Full/Short after preparing.")]
        public bool debugCompleteOnStart;
        [Header("Phase 3B prototype only")]
        public FlowStateIntroPaintController prototypePaint;
        public FlowStateIntroContentProvider contentProvider;
        public PlayableDirector prototypeTimeline;
        public bool PrototypeComplete {get;private set;}
        bool prototypeRunning,sprayRunning,musicWasPlaying,previousPostProcessing;
        UniversalAdditionalCameraData cameraData;
        public IntroPhase Phase { get; private set; }=IntroPhase.Preparing;
        public bool WasSkipped { get; private set; }
        public event Action<IntroPhase> PhaseChanged;
        public event Action MenuReady;
        bool begun, finishing, completed;

        void Awake()
        {
            if(!menu||!input||!cameraController)
            {Debug.LogError("IntroDirector requires menu, input and camera references.",this);enabled=false;return;}
            menu.LockMenu();cameraController.SetReactiveEnabled(false);
        }
        void OnEnable(){if(input)input.Skip+=SkipIntro;}
        void OnDisable(){if(input)input.Skip-=SkipIntro;EndPrototype();}
        void Start(){BeginIntro();}

        public void BeginIntro()
        {
            if(begun||completed||!isActiveAndEnabled)return;
            begun=true;
            menu.LockMenu();cameraController.SetReactiveEnabled(false);
            if(!menu.Prepare())return;
            SetPhase(IntroPhase.Preparing);
            if(mode==IntroMode.Disabled||debugCompleteOnStart){CompleteIntroSafely();return;}
            SetPhase(mode==IntroMode.Short?IntroPhase.LogoReveal:IntroPhase.Painting);
            if(prototypePaint&&contentProvider&&prototypeTimeline)
            {
                prototypeRunning=true;PrototypeComplete=false;
                cameraData=cameraController.menuCamera.GetComponent<UniversalAdditionalCameraData>();
                if(cameraData){previousPostProcessing=cameraData.renderPostProcessing;cameraData.renderPostProcessing=false;}
                musicWasPlaying=menu.audioFeedback.music&&menu.audioFeedback.music.isPlaying;
                if(musicWasPlaying)menu.audioFeedback.music.Pause();
                cameraController.ClearIntroPose();
                contentProvider.Begin();prototypePaint.Begin(contentProvider.Surface);
                prototypeTimeline.time=0;prototypeTimeline.Play();prototypeTimeline.Evaluate();
            }
        }

        public void EvaluatePrototype(double time,bool spraying)
        {
            if(!prototypeRunning||PrototypeComplete)return;
            contentProvider.Evaluate(time);prototypePaint.RenderMasks();
            if(time>=3.0)AdvanceTo(IntroPhase.Composition);
            if(spraying!=sprayRunning)
            {
                sprayRunning=spraying;
                if(spraying)menu.audioFeedback.StartIntroSpray(menu.options[0]);
                else menu.audioFeedback.StopSpray();
            }
        }
        public void SetPrototypeCameraPush(float progress)
        {
            float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress));
            cameraController.SetIntroPose(new Vector3(.035f,.012f,.18f),new Vector3(-.32f,1.1f,.10f),-1.15f,p);
            if(p>.001f)AdvanceTo(IntroPhase.CameraPush);
        }
        // Deterministic inspection in Play Mode, including after the final review hold.
        public void PreviewPrototypeAt(double time)
        {
            if(!Application.isPlaying||!prototypeRunning)return;
            if(PrototypeComplete)
            {
                prototypePaint.End();contentProvider.Begin();prototypePaint.Begin(contentProvider.Surface);
                PrototypeComplete=false;
            }
            prototypeTimeline.Pause();prototypeTimeline.time=System.Math.Max(0,System.Math.Min(time,prototypeTimeline.duration));
            prototypeTimeline.Evaluate();contentProvider.contentCamera.Render();
            menu.audioFeedback.StopSpray();sprayRunning=false;
        }
        void LateUpdate()
        {
            if(!prototypeRunning||PrototypeComplete||prototypeTimeline.time<prototypeTimeline.duration-.025)return;
            // Freeze a review image once; no live cameras or RenderTextures during the hold.
            contentProvider.contentCamera.Render();
            prototypePaint.Freeze();contentProvider.End();
            PrototypeComplete=true;prototypeTimeline.Pause();menu.audioFeedback.StopSpray();sprayRunning=false;
        }
        void EndPrototype()
        {
            if(!prototypeRunning)return;prototypeRunning=false;
            if(prototypeTimeline)prototypeTimeline.Stop();
            if(prototypePaint)prototypePaint.End();if(contentProvider)contentProvider.End();
            if(cameraData)cameraData.renderPostProcessing=previousPostProcessing;
            cameraController.ClearIntroPose();
            if(menu&&menu.audioFeedback)
            {
                menu.audioFeedback.StopSpray();
                if(musicWasPlaying&&menu.audioFeedback.music)menu.audioFeedback.music.UnPause();
            }
            sprayRunning=false;
        }

        // Future sequence adapters report milestones, never directly unlock the menu.
        public bool AdvanceTo(IntroPhase next)
        {
            if(!begun||finishing||completed||next<=Phase||next>=IntroPhase.MenuReady)return false;
            SetPhase(next);return true;
        }
        [ContextMenu("DEBUG / Complete Intro")]
        public void CompleteIntroSafely()
        {
            if(!Application.isPlaying||completed||finishing||!isActiveAndEnabled)return;
            finishing=true;
            if(!menu.Prepare()){finishing=false;return;}
            SetPhase(IntroPhase.Handoff);
            EndPrototype();
            // Disabled, Skip and normal completion share precisely the same final state.
            menu.audioFeedback.StopSpray();
            if(menu.spray.mist)menu.spray.mist.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            cameraController.ResetToHero();
            menu.EnterMenu();
            cameraController.SetReactiveEnabled(true);
            completed=true;finishing=false;
            SetPhase(IntroPhase.MenuReady);
            MenuReady?.Invoke();
        }
        [ContextMenu("DEBUG / Skip Intro")]
        public void SkipIntro()
        {
            if(completed||finishing||!Application.isPlaying)return;
            WasSkipped=true;CompleteIntroSafely();
        }
        void SetPhase(IntroPhase value)
        {
            Phase=value;PhaseChanged?.Invoke(value);
        }
    }
}
