using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace FlowState.Menu
{
    [TrackColor(.7f,.18f,.18f),TrackClipType(typeof(IntroPaintClip)),TrackBindingType(typeof(FlowStateIntroDirector))]
    public sealed class IntroPaintTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph,GameObject go,int inputCount)
        {
            var p=ScriptPlayable<IntroPaintMixer>.Create(graph,inputCount);
            p.GetBehaviour().track=this;p.GetBehaviour().director=graph.GetResolver() as PlayableDirector;return p;
        }
    }
    public sealed class IntroPaintMixer : PlayableBehaviour
    {
        public IntroPaintTrack track;
        public PlayableDirector director;
        public override void ProcessFrame(Playable playable,FrameData info,object playerData)
        {
            var intro=playerData as FlowStateIntroDirector;
            if(!intro||!intro.prototypePaint||!intro.prototypePaint.IsLive)return;
            double time=director.time;bool spraying=false;
            intro.prototypePaint.ResetProgress();
            intro.SetPrototypeCameraPush(0);
            foreach(var clip in track.GetClips())
            {
                var cue=clip.asset as IntroPaintClip;if(!cue)continue;
                float progress=Mathf.Clamp01((float)((time-clip.start)/System.Math.Max(.001,clip.duration)));
                if(cue.stage==IntroPaintStage.CameraPush)intro.SetPrototypeCameraPush(progress);
                else
                {
                    intro.prototypePaint.SetProgress(cue.fragment,cue.stage,progress);
                    spraying|=time>=clip.start&&time<clip.end;
                }
            }
            intro.EvaluatePrototype(time,spraying);
        }
    }
}
