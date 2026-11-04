using UnityEngine;

namespace FlowState.Playground
{
    // Property blocks keep every shared material untouched.
    public sealed class DiegeticSignal : MonoBehaviour
    {
        public Renderer[] surfaces;
        public Light indicator;
        public Color idle = new Color(0.85f, 0.44f, 0.14f);
        public Color active = new Color(0.22f, 0.78f, 0.66f);
        public GameObject readyMarker;
        bool on, focused;
        float pulse;
        MaterialPropertyBlock block;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        void Awake() { Apply(); }
        public void SetActive(bool value) { on = value; Apply(); }
        public void SetFocus(bool value) { if (focused == value) return; focused = value; Apply(); }
        public void Pulse() { pulse = 0.32f; Apply(); }
        void Update() { if (pulse <= 0f) return; pulse = Mathf.Max(0f, pulse - Time.deltaTime); Apply(); }
        void Apply()
        {
            if (block == null) block = new MaterialPropertyBlock();
            Color color = on ? active : idle;
            float strength = pulse > 0f ? 1.8f : focused ? 1.15f : 0.45f;
            block.SetColor(BaseColor, color);
            block.SetColor(Emission, color * strength);
            if (surfaces != null) foreach (var surface in surfaces) if (surface != null) surface.SetPropertyBlock(block);
            if (indicator != null) { indicator.color = color; indicator.intensity = strength * 1.4f; }
            if (readyMarker != null) readyMarker.SetActive(on);
        }
    }
}
