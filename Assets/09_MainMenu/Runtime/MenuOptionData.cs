using UnityEngine;
using UnityEngine.Events;
using FlowState.Rendering;

namespace FlowState.Menu
{
    public enum MenuAction { Play, Gallery, Settings, Quit, Event }
    public enum WallPersistence { ResetOnEntry, Session, BetweenSessions }

    [CreateAssetMenu(menuName = "FLOW STATE/Main Menu/Option")]
    public sealed class MenuOptionData : ScriptableObject
    {
        public string optionId;
        public string displayName;
        public MenuAction action;
        public string scenePath = "Assets/02_Escenas/Game.unity";
        public FlowEmotion visualState;
        public FlowPaletteProfile palette;
        public Texture2D wordMask;
        public Color paintColor = new Color(.08f,.07f,.09f);
        public Color accentColor = new Color(1f,.25f,.1f);
        [Range(0,1)] public float grain = .22f;
        [Range(0,1)] public float drips = .4f;
        [Range(0,1)] public float halftone = .25f;
        [Range(.2f,1.5f)] public float turnDuration = .64f;
        [Range(.1f,1)] public float sprayDuration = .42f;
        [Range(0,2)] public float cameraImpulse = .6f;
        [Range(0,1)] public float pressure = .7f;
        public AudioClip rattle, clack, spray;
        [Range(0,1)] public float volume = .65f;
        [Range(.5f,2)] public float pitch = 1;
        [Range(.1f,2)] public float transitionDuration = .85f;
        public UnityEvent onConfirmed;
    }
}
