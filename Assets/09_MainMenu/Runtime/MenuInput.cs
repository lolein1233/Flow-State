using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlowState.Menu
{
    public enum AmbientLookSource { None, MousePosition, RightStick }

    // Binding overrides belong only to this menu's private copy, never to gameplay.
    public sealed class MenuInput : MonoBehaviour
    {
        public InputActionAsset actions;
        public Camera viewportCamera;
        [Min(0)] public float mouseIdleDelay=.8f;
        public event Action<int> Navigate;
        public event Action Confirm, Back, Skip;
        public Vector2 AmbientInput {get;private set;}
        public bool NavigationEnabled {get;private set;}
        InputActionAsset instance;
        InputActionMap map;
        InputAction navigate,submit,cancel,scroll,click,ambient,skip;
        int held;
        float nextRepeat,lastMouseMove;
        Vector2 previousMouse;
        bool hasMouseSample,mouseSource,suppressUntilRelease,focused=true;
        public Vector2 RawMousePosition {get;private set;}
        public Vector2 ViewportMousePosition {get;private set;}
        public Vector2 NormalizedMousePosition {get;private set;}
        public bool UsesLegacyViewportCoordinates {get;private set;}
        public AmbientLookSource ActiveLookSource {get;private set;}
        public float MouseInactiveSeconds=>mouseSource?Mathf.Max(0,Time.unscaledTime-lastMouseMove):0;
        void OnEnable()
        {
            if(!actions){Debug.LogError("Menu requires the project UI action asset.",this);enabled=false;return;}
            instance=Instantiate(actions);map=instance.FindActionMap("UI",true);
            navigate=map.FindAction("Navigate",true);submit=map.FindAction("Submit",true);
            cancel=map.FindAction("Cancel",true);scroll=map.FindAction("ScrollWheel",true);click=map.FindAction("Click",true);
            for(int i=0;i<navigate.bindings.Count;i++)
                if((navigate.bindings[i].effectivePath??"").IndexOf("rightStick",StringComparison.OrdinalIgnoreCase)>=0)
                    navigate.ApplyBindingOverride(i,string.Empty);
            ambient=map.AddAction("MenuAmbientLook",InputActionType.Value,"<Gamepad>/rightStick");
            skip=map.AddAction("MenuSkipIntro",InputActionType.Button);
            skip.AddBinding("<Keyboard>/escape");skip.AddBinding("<Keyboard>/enter");
            skip.AddBinding("<Keyboard>/space");skip.AddBinding("<Gamepad>/buttonSouth");
            map.Enable();
        }
        public void SetNavigationEnabled(bool value)
        {
            if(value&&!NavigationEnabled)suppressUntilRelease=true;
            NavigationEnabled=value;held=0;
        }
        void Update()
        {
            if(map==null)return;
            ReadAmbient();
            if(!focused)return;
            if(!NavigationEnabled)
            {
                if(skip.WasPressedThisFrame()){suppressUntilRelease=true;Skip?.Invoke();}
                return; // Skip can never become Submit in this frame.
            }
            if(suppressUntilRelease)
            {
                held=0;
                if(!skip.IsPressed()&&!submit.IsPressed()&&!cancel.IsPressed()&&!click.IsPressed()
                    &&navigate.ReadValue<Vector2>().sqrMagnitude<.01f)suppressUntilRelease=false;
                return;
            }
            Vector2 nav=navigate.ReadValue<Vector2>();
            float axis=Mathf.Abs(nav.x)>=Mathf.Abs(nav.y)?nav.x:nav.y;
            int direction=Mathf.Abs(axis)>.55f?(axis>0?1:-1):0;
            if(direction!=0&&(direction!=held||Time.unscaledTime>=nextRepeat))
            {
                Navigate?.Invoke(direction);nextRepeat=Time.unscaledTime+(direction!=held?.34f:.17f);
            }
            held=direction;
            float wheel=scroll.ReadValue<Vector2>().y;
            if(Mathf.Abs(wheel)>.1f)Navigate?.Invoke(wheel>0?1:-1);
            if(submit.WasPressedThisFrame()||click.WasPressedThisFrame())Confirm?.Invoke();
            if(cancel.WasPressedThisFrame())Back?.Invoke();
        }
        void ReadAmbient()
        {
            if(!focused){AmbientInput=Vector2.zero;return;}
            Vector2 stick=ambient.ReadValue<Vector2>();
            var device=Mouse.current;
            Vector2 rawMouse=device!=null?device.position.ReadValue():Vector2.zero;
            Vector2 mouse=rawMouse;
            UsesLegacyViewportCoordinates=false;
#if UNITY_EDITOR
            // Native RawInput/FastMouse reports desktop-space coordinates in the Editor.
            // That breaks on docked Game Views and multi-monitor layouts because pixelRect is
            // expressed in Game View/render coordinates. Legacy Input.mousePosition is already
            // remapped by Unity into the active Game View resolution (and the project uses Both).
            if(device!=null&&device.native)
            {
                Vector3 gameViewMouse=Input.mousePosition;
                if(float.IsFinite(gameViewMouse.x)&&float.IsFinite(gameViewMouse.y))
                {mouse=new Vector2(gameViewMouse.x,gameViewMouse.y);UsesLegacyViewportCoordinates=true;}
            }
#endif
            RawMousePosition=rawMouse;ViewportMousePosition=mouse;
            bool firstSample=!hasMouseSample;
            bool moved=hasMouseSample&&(mouse-previousMouse).sqrMagnitude>.25f;
            previousMouse=mouse;hasMouseSample=true;
            Rect viewport=viewportCamera?viewportCamera.pixelRect:new Rect(0,0,Screen.width,Screen.height);
            bool mouseInside=device!=null&&viewport.Contains(mouse);
            if(stick.sqrMagnitude>.01f){mouseSource=false;ActiveLookSource=AmbientLookSource.RightStick;}
            if(moved&&mouseInside){lastMouseMove=Time.unscaledTime;mouseSource=true;ActiveLookSource=AmbientLookSource.MousePosition;}
            // A valid first sample must work immediately after focus returns.
            if(firstSample&&device!=null&&device.wasUpdatedThisFrame&&mouseInside)
            {mouseSource=true;lastMouseMove=Time.unscaledTime;ActiveLookSource=AmbientLookSource.MousePosition;}
            if(!mouseSource)
            {
                NormalizedMousePosition=Vector2.zero;
                AmbientInput=Vector2.ClampMagnitude(stick,1);return;
            }
            if(!mouseInside)
            {NormalizedMousePosition=AmbientInput=Vector2.zero;return;}
            NormalizedMousePosition=new Vector2(Mathf.Clamp((mouse.x-viewport.x)/Mathf.Max(1,viewport.width)*2-1,-1,1),
                Mathf.Clamp((mouse.y-viewport.y)/Mathf.Max(1,viewport.height)*2-1,-1,1));
            // Position, rather than motion, remains authoritative while the cursor rests off-centre.
            AmbientInput=NormalizedMousePosition;
            if(AmbientInput.sqrMagnitude<.0004f&&Time.unscaledTime-lastMouseMove>mouseIdleDelay)
                ActiveLookSource=AmbientLookSource.None;
        }
        void OnApplicationFocus(bool value)
        {
            focused=value;AmbientInput=NormalizedMousePosition=RawMousePosition=Vector2.zero;
            ViewportMousePosition=Vector2.zero;UsesLegacyViewportCoordinates=false;
            hasMouseSample=false;mouseSource=false;ActiveLookSource=AmbientLookSource.None;
            if(value)suppressUntilRelease=true;
        }
        void OnDisable()
        {
            map?.Disable();map=null;if(instance)Destroy(instance);
            instance=null;held=0;AmbientInput=NormalizedMousePosition=RawMousePosition=Vector2.zero;
            ViewportMousePosition=Vector2.zero;UsesLegacyViewportCoordinates=false;
            hasMouseSample=false;mouseSource=false;ActiveLookSource=AmbientLookSource.None;
        }
    }
}
