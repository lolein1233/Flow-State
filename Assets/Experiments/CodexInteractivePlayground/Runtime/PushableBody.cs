using UnityEngine;

namespace FlowState.Playground
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PushableBody : MonoBehaviour
    {
        [Min(0f)] public float acceleration = 24f;
        [Min(0.1f)] public float maximumSpeed = 1.6f;
        Rigidbody body;
        Vector3 requested;
        public int PushContacts { get; private set; }
        void Awake() { body = GetComponent<Rigidbody>(); }
        public void Push(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            requested += direction.normalized;
            PushContacts++;
        }
        void FixedUpdate()
        {
            if (body == null || body.isKinematic) { requested = Vector3.zero; return; }
            if (requested.sqrMagnitude > 0.001f)
            {
                Vector3 direction = Vector3.ClampMagnitude(requested, 1f);
                float along = Vector3.Dot(body.linearVelocity, direction.normalized);
                float stepAcceleration = Mathf.Min(acceleration, Mathf.Max(0f, maximumSpeed - along) / Time.fixedDeltaTime);
                body.AddForce(direction * stepAcceleration, ForceMode.Acceleration);
            }
            requested = Vector3.zero;
            Vector3 v = body.linearVelocity;
            Vector3 planar = Vector3.ClampMagnitude(new Vector3(v.x, 0f, v.z), maximumSpeed);
            body.linearVelocity = new Vector3(planar.x, Mathf.Clamp(v.y, -12f, 5f), planar.z);
        }
    }
}
