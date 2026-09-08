using UnityEngine;
namespace FlowState.Menu
{
    public sealed class MenuSpray : MonoBehaviour
    {
        public ParticleSystem mist;
        public Transform nozzle;
        public GraffitiMenuPainter painter;
        float until;
        public void Fire(MenuOptionData option)
        {
            var main=mist.main;main.startColor=option.accentColor;
            var emission=mist.emission;emission.rateOverTime=160*option.pressure;
            until=Time.unscaledTime+option.sprayDuration;mist.Play();
        }
        void LateUpdate()
        {
            if(Time.unscaledTime>=until){ if(mist.isEmitting)mist.Stop(true,ParticleSystemStopBehavior.StopEmitting);return; }
            transform.position=nozzle.position;
            Vector3 delta=painter.SprayTarget-transform.position;
            if(delta.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(delta);
            var main=mist.main; main.startSpeed=delta.magnitude*4;main.startLifetime=.27f;
        }
        void OnDisable(){mist.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
}
