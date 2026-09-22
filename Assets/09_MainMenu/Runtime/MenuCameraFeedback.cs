using UnityEngine;
namespace FlowState.Menu
{
    // Preserves serialized references and Impulse API; never writes a Transform.
    public sealed class MenuCameraFeedback : MonoBehaviour
    {
        public Camera menuCamera;
        [Range(0,1)] public float motionScale=1;
        float kick;
        public float Sample(float deltaTime){kick*=Mathf.Exp(-9*deltaTime);return kick;}
        public void Impulse(float amount){kick=Mathf.Clamp(kick+amount,-1,1);}
        public void ResetFeedback(){kick=0;}
        void OnDisable(){ResetFeedback();}
    }
}
