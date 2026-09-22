using UnityEngine;

namespace FlowState.Menu
{
    [DisallowMultipleComponent]
    public sealed class FlowStateMenuCameraController : MonoBehaviour
    {
        [Header("Explicit rig references")]
        public Transform heroAnchor, responseRig;
        public Camera menuCamera;
        public MenuInput input;
        public MenuCameraFeedback feedback;
        public CanMotion canMotion;
        [Header("Ambient limits (local units / degrees)")]
        [Min(0)] public float maxHorizontal=.23f;
        [Min(0)] public float maxVertical=.13f;
        [Min(0)] public float yaw=2.8f, pitch=1.5f, roll=.25f;
        [Header("Spring and return")]
        [Min(1)] public float springStrength=145;
        [Min(0)] public float damping=18;
        [Min(.01f)] public float returnSpeed=4.5f;
        [Range(0,.4f)] public float deadzone=.025f;
        public AnimationCurve responseCurve=AnimationCurve.Linear(0,0,1,1);
        [Header("Accessibility")]
        [Range(0,1)] public float CameraMotionIntensity=1;
        [Range(0,1)] public float ParallaxIntensity=1;
        [Range(0,1)] public float IdleMotionIntensity=0;
        [Header("Can follow-through")]
        public bool canFollowEnabled=true;
        [Range(0,1)] public float canFollowStrength=.2f;
        [Header("Optics")]
        [Range(10,90)] public float baseFieldOfView=38;
        [Min(.1f)] public float referenceAspect=16f/9;
        public bool ReactiveEnabled {get;private set;}
        public Vector2 Response {get;private set;}
        public float CanFollowOffset=>follow;
        public float IntroPoseProgress {get;private set;}
        Vector2 target, velocity;
        float follow, followVelocity, followTarget;
        Vector3 introOffset,introAngles;
        float introFovOffset;

        void OnEnable()
        {
            if(canMotion){canMotion.RotationStarted+=OnTurnStarted;canMotion.RotationCompleted+=OnTurnCompleted;}
        }
        void OnDisable()
        {
            if(canMotion){canMotion.RotationStarted-=OnTurnStarted;canMotion.RotationCompleted-=OnTurnCompleted;}
        }
        void OnTurnStarted(int direction,float duration)
        {if(ReactiveEnabled&&canFollowEnabled)followTarget=Mathf.Sign(direction)*canFollowStrength;}
        void OnTurnCompleted(){followTarget=0;}
        public void SetReactiveEnabled(bool value)
        {
            ReactiveEnabled=value;
            if(value)ClearIntroPose();
            else ClearMotion();
        }
        public void SetIntroPose(Vector3 offset,Vector3 angles,float fovOffset,float progress)
        {introOffset=offset;introAngles=angles;introFovOffset=fovOffset;IntroPoseProgress=Mathf.Clamp01(progress);}
        public void ClearIntroPose()
        {introOffset=introAngles=Vector3.zero;introFovOffset=0;IntroPoseProgress=0;}
        public void ResetToHero()
        {
            ClearMotion();
            ClearIntroPose();
            if(feedback)feedback.ResetFeedback();
            ApplyPose(Vector3.zero,Vector3.zero,0);
        }
        void ClearMotion()
        {
            Response=Vector2.zero;target=velocity=Vector2.zero;
            follow=followVelocity=followTarget=0;
        }
        float Shape(float axis)
        {
            float magnitude=Mathf.Clamp01((Mathf.Abs(axis)-deadzone)/Mathf.Max(.001f,1-deadzone));
            return Mathf.Sign(axis)*Mathf.Clamp01(responseCurve.Evaluate(magnitude));
        }
        void LateUpdate()
        {
            if(!heroAnchor||!responseRig||!menuCamera)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            float intensity=ReactiveEnabled?CameraMotionIntensity*(feedback?feedback.motionScale:1):0;
            if(intensity<=0)
            {
                ClearMotion();if(feedback)feedback.ResetFeedback();
                ApplyPose(introOffset*IntroPoseProgress,introAngles*IntroPoseProgress,introFovOffset*IntroPoseProgress);return;
            }
            Vector2 raw=input?input.AmbientInput:Vector2.zero;
            Vector2 desired=new Vector2(Shape(raw.x),Shape(raw.y));
            target=desired.sqrMagnitude<.000001f?Vector2.MoveTowards(target,Vector2.zero,returnSpeed*dt):desired;
            if(!canFollowEnabled)follow=followVelocity=followTarget=0;
            Vector2 response=Response;
            // Bounded integration steps keep the spring stable during editor/frame stalls.
            int steps=Mathf.Max(1,Mathf.CeilToInt(dt/(1f/120)));
            float h=dt/steps,k=Mathf.Clamp(springStrength,1,400),drag=Mathf.Clamp(damping,0,80);
            for(int i=0;i<steps;i++)
            {
                velocity+=((target-response)*k-velocity*drag)*h;
                response+=velocity*h;
                if(Mathf.Abs(response.x)>1){response.x=Mathf.Clamp(response.x,-1,1);velocity.x=0;}
                if(Mathf.Abs(response.y)>1){response.y=Mathf.Clamp(response.y,-1,1);velocity.y=0;}
                followVelocity+=((followTarget-follow)*40-followVelocity*11)*h;
                follow=Mathf.Clamp(follow+followVelocity*h,-1,1);
            }
            Response=response;
            float kick=feedback&&feedback.isActiveAndEnabled?feedback.Sample(dt):0;
            float t=Time.unscaledTime;
            Vector3 offset=new Vector3(response.x*maxHorizontal+kick*.055f+follow*.025f,response.y*maxVertical,0);
            offset+=new Vector3(Mathf.Sin(t*.5f)*.008f,Mathf.Sin(t*.7f)*.009f,0)*IdleMotionIntensity;
            Vector3 angles=new Vector3(-response.y*pitch,response.x*yaw+follow*.3f,-response.x*roll+kick*.32f+follow*.08f);
            angles.z+=Mathf.Sin(t*.45f)*.06f*IdleMotionIntensity;
            ApplyPose(offset*intensity*ParallaxIntensity,angles*intensity,0);
        }
        void ApplyPose(Vector3 offset,Vector3 angles,float fovOffset)
        {
            if(!heroAnchor||!responseRig||!menuCamera)return;
            transform.SetPositionAndRotation(heroAnchor.position,heroAnchor.rotation);
            responseRig.localPosition=offset;responseRig.localRotation=Quaternion.Euler(angles);
            menuCamera.transform.localPosition=Vector3.zero;menuCamera.transform.localRotation=Quaternion.identity;
            float aspect=Mathf.Max(.3f,menuCamera.aspect);
            float corrected=2*Mathf.Atan(Mathf.Tan(baseFieldOfView*Mathf.Deg2Rad*.5f)*Mathf.Max(1,referenceAspect/aspect))*Mathf.Rad2Deg;
            menuCamera.fieldOfView=Mathf.Clamp(corrected+fovOffset,1,179);
        }
    }
}
