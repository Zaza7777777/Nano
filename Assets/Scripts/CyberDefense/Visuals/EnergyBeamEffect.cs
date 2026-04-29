using UnityEngine;

namespace CyberDefense.Visuals
{
    public sealed class EnergyBeamEffect : MonoBehaviour
    {
        private LineRenderer line;
        private float lifetime;
        private float age;

        public static void Spawn(Vector3 start, Vector3 end, Color color, float width = 0.055f, float duration = 0.16f)
        {
            GameObject instance = new GameObject("Interaction Energy Beam");
            EnergyBeamEffect beam = instance.AddComponent<EnergyBeamEffect>();
            beam.Initialize(start, end, color, width, duration);
        }

        private void Initialize(Vector3 start, Vector3 end, Color color, float width, float duration)
        {
            lifetime = duration;
            line = gameObject.AddComponent<LineRenderer>();
            line.material = ProceduralVisualAssets.SpriteMaterial;
            line.positionCount = 3;
            line.useWorldSpace = true;
            line.sortingOrder = 30;
            line.startWidth = width;
            line.endWidth = width * 0.45f;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.25f);

            Vector3 midpoint = (start + end) * 0.5f + Vector3.forward * -0.05f;
            midpoint += (Vector3)(Random.insideUnitCircle * 0.18f);
            line.SetPosition(0, start);
            line.SetPosition(1, midpoint);
            line.SetPosition(2, end);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - age / lifetime);
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.3f, 0.9f, 1f), 1f) },
                new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha * 0.2f, 1f) });
            line.colorGradient = gradient;

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
