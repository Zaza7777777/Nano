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
        private SpriteRenderer cargoPod;
        private SpriteRenderer glitchOutlineA;
        private SpriteRenderer glitchOutlineB;
        private Transform barFill;
        private LineRenderer scannerRing;
        private LineRenderer hubShieldRing;
        private TextMesh label;
        private ParticleSystem primaryParticles;
        private ParticleSystem secondaryParticles;
        private ParticleSystem repairSparks;
        private float repairPulse;
        private float collaborationScale = 1f;
        private float baseScale;
        private Material lineMaterial;

        public static bool LabelsVisible { get; set; }

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
            UpdateLabel();
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
                    BuildCargoPod();
                    break;
                case EntityKind.RepairDrone:
                    body.sprite = ProceduralVisualAssets.Gear;
                    body.color = new Color(0.86f, 0.96f, 1f, 1f);
                    glow.color = new Color(0.45f, 0.95f, 1f, 0.25f);
                    repairSparks = ParticleVisualFactory.CreateSparks(transform);
                    ParticleVisualFactory.SetEmission(repairSparks, 0f);
                    scannerRing = BuildRing("Medic Scanner Ring", 0.9f, 32, new Color(0.12f, 1f, 0.45f, 0.82f), 0.025f, 22);
                    break;
                case EntityKind.Malware:
                    body.sprite = ProceduralVisualAssets.Spark;
                    body.color = new Color(1f, 0.12f, 0.15f, 1f);
                    glow.color = new Color(1f, 0.05f, 0.08f, 0.28f);
                    BuildGlitchOutlines();
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
                    hubShieldRing = BuildRing("Hub Health Shield", 1.28f, 48, new Color(0.1f, 0.65f, 1f, 0.92f), 0.045f, 24);
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
                UpdateCargoPod(overloaded);
                transform.localScale = Vector3.one * (baseScale * (0.95f + Mathf.Sin(Time.time * 11f) * 0.04f));
                ParticleVisualFactory.SetEmission(secondaryParticles, overloaded ? 22f : 10f);
            }
            else if (entity.Kind == EntityKind.Malware)
            {
                MalwareBug malware = entity as MalwareBug;
                bool attacking = malware != null && malware.IsThreateningTarget;
                float aggression = entity.Simulation != null && entity.Simulation.Hub != null
                    ? 1f - Mathf.Clamp01(Vector2.Distance(entity.Position2D, entity.Simulation.Hub.Position2D) / 14f)
                    : 0.35f;
                float flicker = attacking ? Mathf.PingPong(Time.time * 14f + transform.GetInstanceID(), 1f) : aggression;
                body.color = Color.Lerp(new Color(0.78f, 0.03f, 0.08f), new Color(1f, 0.58f, 0.08f), flicker);
                UpdateGlitchOutlines(attacking);
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
                UpdateScannerRing();
                repairPulse = Mathf.Max(0f, repairPulse - Time.deltaTime);
                ParticleVisualFactory.SetEmission(repairSparks, repairPulse > 0f ? 45f * collaborationScale : 0f);
                ParticleSystem.MainModule main = repairSparks.main;
                main.startSize = 0.13f * collaborationScale;
            }
            else if (entity.Kind == EntityKind.CentralHub)
            {
                UpdateHubShield();
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

        private void BuildCargoPod()
        {
            cargoPod = NewRenderer("Harvest Cargo Pod", 21);
            cargoPod.sprite = ProceduralVisualAssets.Disc;
            cargoPod.material = ProceduralVisualAssets.SpriteMaterial;
            cargoPod.transform.localScale = Vector3.one * 0.42f;
            cargoPod.color = new Color(1f, 0.92f, 0.18f, 0.9f);
        }

        private void UpdateCargoPod(bool hasPayload)
        {
            if (cargoPod == null)
            {
                return;
            }

            float pulse = 0.85f + Mathf.Sin(Time.time * (hasPayload ? 12f : 5f)) * (hasPayload ? 0.22f : 0.08f);
            cargoPod.transform.localScale = Vector3.one * (hasPayload ? 0.55f * pulse : 0.34f * pulse);
            cargoPod.color = hasPayload
                ? new Color(1f, 0.96f, 0.25f, 1f)
                : new Color(0.9f, 0.64f, 0.08f, 0.65f);
        }

        private void BuildGlitchOutlines()
        {
            glitchOutlineA = NewRenderer("Glitch Outline A", 8);
            glitchOutlineB = NewRenderer("Glitch Outline B", 8);
            glitchOutlineA.sprite = ProceduralVisualAssets.Spark;
            glitchOutlineB.sprite = ProceduralVisualAssets.Spark;
            glitchOutlineA.material = ProceduralVisualAssets.SpriteMaterial;
            glitchOutlineB.material = ProceduralVisualAssets.SpriteMaterial;
            glitchOutlineA.transform.localScale = Vector3.one * 1.24f;
            glitchOutlineB.transform.localScale = Vector3.one * 1.34f;
        }

        private void UpdateGlitchOutlines(bool attacking)
        {
            if (glitchOutlineA == null || glitchOutlineB == null)
            {
                return;
            }

            float jitter = attacking ? 0.12f : 0.045f;
            glitchOutlineA.transform.localPosition = Random.insideUnitCircle * jitter;
            glitchOutlineB.transform.localPosition = Random.insideUnitCircle * jitter;
            Color hot = attacking ? new Color(1f, 0.55f, 0f, 0.75f) : new Color(1f, 0f, 0.08f, 0.35f);
            glitchOutlineA.color = hot;
            glitchOutlineB.color = new Color(hot.r, hot.g * 0.35f, hot.b, hot.a * 0.6f);
        }

        private void UpdateScannerRing()
        {
            if (scannerRing == null)
            {
                return;
            }

            scannerRing.transform.Rotate(Vector3.forward, Time.deltaTime * 120f);
            Color color = repairPulse > 0f
                ? new Color(0.08f, 1f, 0.34f, 1f)
                : new Color(0.12f, 0.85f, 0.46f, 0.52f);
            scannerRing.startColor = color;
            scannerRing.endColor = color;
            scannerRing.startWidth = repairPulse > 0f ? 0.04f : 0.025f;
            scannerRing.endWidth = scannerRing.startWidth;
        }

        private void UpdateHubShield()
        {
            if (hubShieldRing == null)
            {
                return;
            }

            float health = entity.HealthRatio;
            Color color = Color.Lerp(new Color(1f, 0.08f, 0.04f, 0.92f), new Color(0.08f, 0.55f, 1f, 0.92f), health);
            hubShieldRing.startColor = color;
            hubShieldRing.endColor = color;
            hubShieldRing.positionCount = Mathf.Max(8, Mathf.RoundToInt(Mathf.Lerp(14f, 48f, health)));
            SetRingPositions(hubShieldRing, 1.28f, health < 0.98f ? 0.18f : 0f);
            float scalePulse = 1f + Mathf.Sin(Time.time * 3.5f) * 0.04f;
            hubShieldRing.transform.localScale = Vector3.one * scalePulse;
        }

        private void UpdateLabel()
        {
            if (label == null)
            {
                label = BuildLabel();
            }

            label.gameObject.SetActive(LabelsVisible);
            if (!LabelsVisible)
            {
                return;
            }

            label.text = LabelText();
            label.color = LabelColor();
            label.transform.rotation = Quaternion.identity;
        }

        private TextMesh BuildLabel()
        {
            GameObject labelObject = new GameObject("Presentation Role Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.22f;
            text.fontSize = 56;
            text.text = LabelText();
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 250;
            labelObject.SetActive(false);
            return text;
        }

        private string LabelText()
        {
            if (entity.Kind == EntityKind.RepairDrone && repairPulse > 0f)
            {
                return "REPAIRING...";
            }

            if (entity.Kind == EntityKind.Sentinel)
            {
                DataSentinel sentinel = entity as DataSentinel;
                return sentinel != null && sentinel.HasPayload ? "SENTINEL: CARGO" : "SENTINEL";
            }

            if (entity.Kind == EntityKind.Malware)
            {
                MalwareBug malware = entity as MalwareBug;
                return malware != null && malware.IsThreateningTarget ? "MALWARE: ATTACK" : "MALWARE";
            }

            if (entity.Kind == EntityKind.RepairDrone)
            {
                return "REPAIR DRONE";
            }

            if (entity.Kind == EntityKind.CentralHub)
            {
                return "CENTRAL HUB";
            }

            if (entity.Kind == EntityKind.CorruptedNode)
            {
                CorruptedNode node = entity as CorruptedNode;
                if (node != null && node.IsRepaired)
                {
                    return "STABLE NODE";
                }

                return repairPulse > 0f ? "STABILIZING..." : "CORRUPTED NODE";
            }

            return entity.Kind.ToString().ToUpperInvariant();
        }

        private Color LabelColor()
        {
            switch (entity.Kind)
            {
                case EntityKind.Malware:
                    return new Color(1f, 0.42f, 0.08f, 1f);
                case EntityKind.RepairDrone:
                    return new Color(0.25f, 1f, 0.48f, 1f);
                case EntityKind.CentralHub:
                    return new Color(0.25f, 0.72f, 1f, 1f);
                case EntityKind.Sentinel:
                    return new Color(1f, 0.9f, 0.18f, 1f);
                default:
                    return Color.white;
            }
        }

        private LineRenderer BuildRing(string name, float radius, int segments, Color color, float width, int sortingOrder)
        {
            GameObject ringObject = new GameObject(name);
            ringObject.transform.SetParent(transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = segments;
            ring.material = GetLineMaterial();
            ring.startColor = color;
            ring.endColor = color;
            ring.startWidth = width;
            ring.endWidth = width;
            ring.sortingOrder = sortingOrder;
            ring.alignment = LineAlignment.View;
            SetRingPositions(ring, radius, 0f);
            return ring;
        }

        private void SetRingPositions(LineRenderer ring, float radius, float gapEveryRadians)
        {
            int count = ring.positionCount;
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0f : (float)i / count;
                float angle = t * Mathf.PI * 2f;
                float gap = gapEveryRadians > 0f && Mathf.Sin(angle * 5f + Time.time * 2f) > 0.72f ? 0.18f : 0f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (radius + gap));
            }
        }

        private Material GetLineMaterial()
        {
            if (lineMaterial != null)
            {
                return lineMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return lineMaterial;
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
