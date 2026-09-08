using UnityEngine;
namespace FlowState.Menu
{
    public sealed class MenuCameraFeedback : MonoBehaviour
    {
        public Camera menuCamera;
        [Range(0,1)] public float motionScale=1;
        Vector3 origin;
        Quaternion rotation;
        float kick;
        void Awake() { origin=transform.localPosition; rotation=transform.localRotation; }
        public void Impulse(float amount) { kick=Mathf.Clamp(kick+amount,-1,1); }
        void LateUpdate()
        {
            kick=Mathf.Lerp(kick,0,1-Mathf.Exp(-9*Time.unscaledDeltaTime));
            float t=Time.unscaledTime;
            transform.localPosition=origin+new Vector3(kick*.055f+Mathf.Sin(t*.5f)*.008f,Mathf.Sin(t*.7f)*.009f,0)*motionScale;
            transform.localRotation=rotation*Quaternion.Euler(0,0,(kick*.32f+Mathf.Sin(t*.45f)*.06f)*motionScale);
            // Preserve the entire designed 16:9 composition on narrow windows.
            float a=Mathf.Max(.3f,menuCamera.aspect);
            menuCamera.fieldOfView=2*Mathf.Atan(Mathf.Tan(38*Mathf.Deg2Rad*.5f)*Mathf.Max(1,16f/9/a))*Mathf.Rad2Deg;
        }
    }
}
