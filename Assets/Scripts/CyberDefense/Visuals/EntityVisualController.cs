using CyberDefense.Entities;
using UnityEngine;

namespace CyberDefense.Visuals
{
    [RequireComponent(typeof(NetworkEntity))]
    public sealed class EntityVisualController : MonoBehaviour
    {
        private NetworkEntity entity;
        private SpriteRenderer body;
        private SpriteRenderer glow;
        private Transform barFill;
        private ParticleSystem primaryParticles;
        private ParticleSystem secondaryParticles;
        private ParticleSystem repairSparks;
        private float repairPulse;
        private float collaborationScale = 1f;
        private float baseScale;

        public void Initialize(NetworkEntity target)
        {
            entity = target;
            baseScale = transform.localScale.x;
            BuildVisual();
        }

        public void PulseRepair(float intensity)
        {
            repairPulse = 0.18f;
            collaborationScale = Mathf.Clamp(intensity, 1f, 3.5f);
        }

        private void Awake()
        {
            entity = GetComponent<NetworkEntity>();
        }

        private void LateUpdate()
        {
            if (entity == null)
            {
                return;
            }

            float status = entity.Kind == EntityKind.CorruptedNode ? entity.IntegrityRatio : Mathf.Min(entity.HealthRatio, entity.EnergyRatio);
            Color statusColor = ProceduralVisualAssets.StatusColor(status);
            UpdateStatusBar(status, statusColor);
            UpdatePerKindVisuals(statusColor);
        }

        private void BuildVisual()
        {
            SpriteRenderer primitiveRenderer = GetComponent<SpriteRenderer>();
            if (primitiveRenderer != null)
            {
                primitiveRenderer.enabled = false;
            }

            body = NewRenderer("Procedural Body", 10);
            body.material = ProceduralVisualAssets.SpriteMaterial;

            glow = NewRenderer("Soft State Glow", 9);
            glow.sprite = ProceduralVisualAssets.Disc;
            glow.material = ProceduralVisualAssets.SpriteMaterial;
            glow.transform.localScale = Vector3.one * 1.9f;

            BuildStatusBar();
            ConfigureKind();
        }

        private void ConfigureKind()
        {
            switch (entity.Kind)
            {
                case EntityKind.Sentinel:
                    body.sprite = ProceduralVisualAssets.Disc;
                    body.color = new Color(1f, 0.86f, 0.1f, 0.85f);
                    glow.color = new Color(1f, 0.9f, 0.15f, 0.24f);
                    primaryParticles = ParticleVisualFactory.CreateSwirl(transform, new Color(1f, 0.86f, 0.1f, 0.95f));
                    secondaryParticles = ParticleVisualFactory.CreateGrime(transform);
                    break;
                case EntityKind.RepairDrone:
                    body.sprite = ProceduralVisualAssets.Gear;
                    body.color = new Color(0.86f, 0.96f, 1f, 1f);
                    glow.color = new Color(0.45f, 0.95f, 1f, 0.25f);
                    repairSparks = ParticleVisualFactory.CreateSparks(transform);
                    ParticleVisualFactory.SetEmission(repairSparks, 0f);
                    break;
                case EntityKind.Malware:
                    body.sprite = ProceduralVisualAssets.Spark;
                    body.color = new Color(1f, 0.12f, 0.15f, 1f);
                    glow.color = new Color(1f, 0.05f, 0.08f, 0.28f);
                    break;
                case EntityKind.CorruptedNode:
                    body.sprite = ProceduralVisualAssets.Socket;
                    body.color = new Color(0.22f, 1f, 0.38f, 1f);
                    glow.color = new Color(0.15f, 1f, 0.35f, 0.22f);
                    primaryParticles = ParticleVisualFactory.CreateArcs(transform);
                    break;
                case EntityKind.CentralHub:
                    body.sprite = ProceduralVisualAssets.Gear;
                    body.color = new Color(0.66f, 0.9f, 1f, 1f);
                    glow.color = new Color(0.22f, 0.65f, 1f, 0.25f);
                    break;
                case EntityKind.Firewall:
                    body.sprite = ProceduralVisualAssets.Socket;
                    body.color = new Color(0.1f, 0.35f, 1f, 0.82f);
                    glow.color = new Color(0.08f, 0.35f, 1f, 0.18f);
                    break;
                case EntityKind.DataPacket:
                    body.sprite = ProceduralVisualAssets.Disc;
                    body.color = new Color(1f, 0.75f, 0.12f, 1f);
                    glow.color = new Color(1f, 0.75f, 0.12f, 0.2f);
                    break;
            }
        }

        private void UpdatePerKindVisuals(Color statusColor)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 8f + transform.GetInstanceID()) * 0.08f;
            glow.color = new Color(statusColor.r, statusColor.g, statusColor.b, 0.16f + (1f - entity.HealthRatio) * 0.2f);
            glow.transform.localScale = Vector3.one * (1.65f + (1f - entity.HealthRatio) * 0.9f);

            if (entity.Kind == EntityKind.Sentinel)
            {
                DataSentinel sentinel = entity as DataSentinel;
                bool overloaded = sentinel != null && sentinel.HasPayload;
                body.color = overloaded ? new Color(0.95f, 0.44f, 0.05f, 0.92f) : new Color(1f, 0.88f, 0.08f, 0.9f);
                transform.localScale = Vector3.one * (baseScale * (0.95f + Mathf.Sin(Time.time * 11f) * 0.04f));
                ParticleVisualFactory.SetEmission(secondaryParticles, overloaded ? 22f : 10f);
            }
            else if (entity.Kind == EntityKind.Malware)
            {
                float aggression = entity.Simulation != null && entity.Simulation.Hub != null
                    ? 1f - Mathf.Clamp01(Vector2.Distance(entity.Position2D, entity.Simulation.Hub.Position2D) / 14f)
                    : 0.35f;
                body.color = Color.Lerp(new Color(0.8f, 0.04f, 0.08f), new Color(1f, 0.48f, 0.12f), aggression);
                transform.localScale = Vector3.one * (baseScale * (pulse + aggression * 0.25f));
            }
            else if (entity.Kind == EntityKind.CorruptedNode)
            {
                CorruptedNode node = entity as CorruptedNode;
                float corruption = node != null ? Mathf.Clamp01(node.Corruption / 100f) : 1f;
                body.color = Color.Lerp(new Color(0.52f, 0.78f, 0.9f), new Color(0.18f, 1f, 0.32f), corruption);
                ParticleVisualFactory.SetEmission(primaryParticles, corruption * 34f);
            }
            else if (entity.Kind == EntityKind.RepairDrone)
            {
                body.transform.Rotate(Vector3.forward, Time.deltaTime * 90f);
                repairPulse = Mathf.Max(0f, repairPulse - Time.deltaTime);
                ParticleVisualFactory.SetEmission(repairSparks, repairPulse > 0f ? 45f * collaborationScale : 0f);
                ParticleSystem.MainModule main = repairSparks.main;
                main.startSize = 0.13f * collaborationScale;
            }
        }

        private void BuildStatusBar()
        {
            GameObject back = new GameObject("AI State Bar Back");
            back.transform.SetParent(transform, false);
            back.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            back.transform.localScale = new Vector3(1f, 0.09f, 1f);

            SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
            backRenderer.sprite = ProceduralVisualAssets.Disc;
            backRenderer.color = new Color(0.02f, 0.03f, 0.04f, 0.82f);
            backRenderer.sortingOrder = 18;

            GameObject fill = new GameObject("AI State Bar Fill");
            fill.transform.SetParent(back.transform, false);
            fill.transform.localPosition = Vector3.zero;
            fill.transform.localScale = Vector3.one;
            SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = ProceduralVisualAssets.Disc;
            fillRenderer.color = Color.green;
            fillRenderer.sortingOrder = 19;
            barFill = fill.transform;
        }

        private void UpdateStatusBar(float ratio, Color color)
        {
            if (barFill == null)
            {
                return;
            }

            barFill.localScale = new Vector3(Mathf.Max(0.05f, ratio), 1f, 1f);
            barFill.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, 0f);
            SpriteRenderer renderer = barFill.GetComponent<SpriteRenderer>();
            renderer.color = color;
        }

        private SpriteRenderer NewRenderer(string name, int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
