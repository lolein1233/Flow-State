using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace FlowState.Menu
{
    // A dedicated script file is required for Timeline subasset serialization across reloads.
    [System.Serializable]
    public sealed class IntroPaintClip : PlayableAsset,ITimelineClipAsset
    {
        [Range(0,2)] public int fragment;
        public IntroPaintStage stage;
        public ClipCaps clipCaps=>ClipCaps.None;
        public override Playable CreatePlayable(PlayableGraph graph,GameObject owner)=>Playable.Create(graph);
    }
}
