using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlowState.Menu
{
    // A private instance of the project's UI map preserves binding IDs and ownership.
    public sealed class MenuInput : MonoBehaviour
    {
        public InputActionAsset actions;
        public event Action<int> Navigate;
        public event Action Confirm, Back;
        InputActionAsset instance;
        InputActionMap map;
        InputAction navigate, submit, cancel, scroll, click;
        int held;
        float nextRepeat;
        void OnEnable()
        {
            if (!actions) { Debug.LogError("Menu requires the project UI action asset.", this); enabled=false; return; }
            instance=Instantiate(actions);
            map=instance.FindActionMap("UI",true);
            navigate=map.FindAction("Navigate",true);
            submit=map.FindAction("Submit",true);
            cancel=map.FindAction("Cancel",true);
            scroll=map.FindAction("ScrollWheel",true);
            click=map.FindAction("Click",true);
            map.Enable();
        }
        void Update()
        {
            float x=navigate.ReadValue<Vector2>().x;
            int direction=Mathf.Abs(x)>.55f ? (x>0?1:-1):0;
            if(direction!=0 && (direction!=held || Time.unscaledTime>=nextRepeat))
            {
                Navigate?.Invoke(direction);
                nextRepeat=Time.unscaledTime+(direction!=held?.34f:.17f);
            }
            held=direction;
            if(scroll.ReadValue<Vector2>().y is float wheel && Mathf.Abs(wheel)>.1f) Navigate?.Invoke(wheel>0?1:-1);
            if(submit.WasPressedThisFrame() || click.WasPressedThisFrame()) Confirm?.Invoke();
            if(cancel.WasPressedThisFrame()) Back?.Invoke();
        }
        void OnDisable()
        {
            map?.Disable();
            if(instance) Destroy(instance);
            instance=null; held=0;
        }
    }
}
