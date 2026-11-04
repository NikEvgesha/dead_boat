using UnityEngine;

namespace DeadBoat.Online
{
    // Local bounded history in logical coordinates. No per-animal network transforms.
    public sealed class CompanionTrail
    {
        private readonly Vector3[] points = new Vector3[128];
        private int head, count;
        public int Count => count;
        public void Reset(Vector3 position) { head = 0; count = 1; points[0] = position; }
        public void Observe(Vector3 position)
        {
            if (count == 0 || (position - points[head]).sqrMagnitude > 100) { Reset(position); return; }
            if ((position - points[head]).sqrMagnitude < .04f) return;
            head = (head + 1) % points.Length; points[head] = position;
            count = Mathf.Min(count + 1, points.Length);
        }
        public Vector3 Sample(float distance, Vector3 forward)
        {
            if (count == 0) return Vector3.zero;
            Vector3 latest = points[head];
            for (int i = 1; i < count; i++)
            {
                Vector3 next = points[(head - i + points.Length) % points.Length];
                float segment = Vector3.Distance(latest, next);
                if (segment >= distance && segment > 0) return Vector3.Lerp(latest, next, distance / segment);
                distance -= segment; latest = next;
            }
            forward.y = 0;
            return latest - (forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward) * distance;
        }
    }
}
