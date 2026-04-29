using UnityEngine;

namespace CyberDefense.Visuals
{
    public static class ParticleVisualFactory
    {
        public static ParticleSystem CreateSwirl(Transform parent, Color color)
        {
            ParticleSystem particles = Create(parent, "Swirling Nano Dust", color, 0.45f, 8f, 0.18f);
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalZ = 1.8f;
            velocity.radial = -0.08f;
            return particles;
        }

        public static ParticleSystem CreateGrime(Transform parent)
        {
            ParticleSystem particles = Create(parent, "Lifted Grime Motes", new Color(0.55f, 0.42f, 0.16f, 0.55f), 0.35f, 2f, 0.08f);
            ParticleSystem.MainModule main = particles.main;
            main.startSpeed = 0.35f;
            return particles;
        }

        public static ParticleSystem CreateSparks(Transform parent)
        {
            ParticleSystem particles = Create(parent, "Repair Sparks", new Color(0.4f, 0.95f, 1f, 1f), 0.35f, 45f, 0.16f);
            ParticleSystem.MainModule main = particles.main;
            main.startSpeed = 1.2f;
            main.startLifetime = 0.22f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 34f;
            shape.radius = 0.08f;
            return particles;
        }

        public static ParticleSystem CreateArcs(Transform parent)
        {
            ParticleSystem particles = Create(parent, "Unstable Socket Arcs", new Color(0.28f, 1f, 0.42f, 0.9f), 0.5f, 18f, 0.22f);
            ParticleSystem.MainModule main = particles.main;
            main.startSpeed = 0.8f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            return particles;
        }

        public static ParticleSystem CreateDestructionBurst(Vector3 position, Color color)
        {
            GameObject instance = new GameObject("Micro Circuit Burst");
            instance.transform.position = position;
            ParticleSystem particles = Create(instance.transform, "Burst Particles", color, 0.7f, 0f, 0.16f);
            ParticleSystem.MainModule main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.startSpeed = 2.5f;
            main.startLifetime = 0.5f;
            main.startSize = 0.11f;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            particles.Emit(28);
            Object.Destroy(instance, 1.2f);
            return particles;
        }

        public static void SetEmission(ParticleSystem particles, float rate)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rate;
        }

        private static ParticleSystem Create(Transform parent, string name, Color color, float lifetime, float rate, float size)
        {
            GameObject instance = new GameObject(name);
            instance.transform.SetParent(parent, false);
            ParticleSystem particles = instance.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.startColor = color;
            main.startLifetime = lifetime;
            main.startSize = size;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.loop = true;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rate;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = ProceduralVisualAssets.SpriteMaterial;
            renderer.sortingOrder = 12;
            return particles;
        }
    }
}
