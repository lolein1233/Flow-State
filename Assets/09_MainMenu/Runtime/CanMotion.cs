using UnityEngine;

namespace FlowState.Menu
{
    public sealed class CanMotion : MonoBehaviour
    {
        public Transform can;
        public Transform nozzle;
        public AnimationCurve turnCurve = new AnimationCurve(new Keyframe(0,0),new Keyframe(.12f,-.08f),new Keyframe(.62f,1.08f),new Keyframe(.82f,.975f),new Keyframe(1,1));
        [Range(0,1)] public float motionScale=1;
        Vector3 origin, nozzleOrigin;
        Quaternion rest;
        float yaw, from, target, elapsed, duration, sprayKick;
        bool turning;
        public float Progress => turning?Mathf.Clamp01(elapsed/duration):1;
        public bool IsTurning => turning;
        void Awake() { origin=can.localPosition; rest=can.localRotation; if(nozzle) nozzleOrigin=nozzle.localPosition; }
        public void Turn(int direction,float seconds)
        {
            from=yaw; target=yaw+90*direction; elapsed=0; duration=Mathf.Max(.15f,seconds); turning=true;
        }
        public void Spray() { sprayKick=1; }
        void Update()
        {
            float dt=Time.unscaledDeltaTime;
            if(turning)
            {
                elapsed+=dt;
                yaw=Mathf.LerpUnclamped(from,target,turnCurve.Evaluate(Progress));
                if(elapsed>=duration) { yaw=target%360; turning=false; }
            }
            sprayKick=Mathf.MoveTowards(sprayKick,0,dt*5);
            float t=Time.unscaledTime;
            float impulse=turning?Mathf.Sin(Progress*Mathf.PI):0;
            can.localRotation=rest*Quaternion.Euler(Mathf.Sin(t*1.1f)*.65f*motionScale+sprayKick*3, yaw, (Mathf.Sin(t*.75f)*1.1f+impulse*4)*motionScale);
            can.localPosition=origin+new Vector3(Mathf.Sin(t*.62f)*.018f,Mathf.Sin(t*1.3f)*.025f+impulse*.09f,-sprayKick*.06f)*motionScale;
            if(nozzle) nozzle.localPosition=nozzleOrigin+Vector3.down*(sprayKick*.025f);
        }
    }
}
