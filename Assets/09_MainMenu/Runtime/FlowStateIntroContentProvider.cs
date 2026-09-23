using UnityEngine;
namespace FlowState.Menu
{
    public sealed class FlowStateIntroContentProvider : MonoBehaviour
    {
        public Camera contentCamera;
        public GameObject diorama;
        public Transform animatedCan;
        public RenderTexture Surface {get;private set;}
        public bool IsLive=>Surface;
        Vector3 initialPosition;Quaternion initialRotation;
        public void Begin()
        {
            if(Surface)return;
            initialPosition=contentCamera.transform.localPosition;
            if(animatedCan)initialRotation=animatedCan.localRotation;
            Surface=PaintRenderSurface.Create(1024,576,"Intro shared world",24);
            diorama.SetActive(true);contentCamera.targetTexture=Surface;contentCamera.enabled=true;
        }
        public void Evaluate(double time)
        {
            if(!Surface)return;
            float t=(float)time;
            contentCamera.transform.localPosition=initialPosition+new Vector3(Mathf.Sin(t*.65f)*.32f,Mathf.Sin(t*.9f)*.055f,0);
            if(animatedCan)animatedCan.localRotation=initialRotation*Quaternion.Euler(0,t*22,Mathf.Sin(t*1.4f)*4);
        }
        public void End()
        {
            if(contentCamera){contentCamera.enabled=false;contentCamera.targetTexture=null;contentCamera.transform.localPosition=initialPosition;}
            if(animatedCan)animatedCan.localRotation=initialRotation;
            if(diorama)diorama.SetActive(false);
            var rt=Surface;PaintRenderSurface.Release(ref rt);Surface=null;
        }
        void OnDestroy(){if(Surface)End();}
    }
}
