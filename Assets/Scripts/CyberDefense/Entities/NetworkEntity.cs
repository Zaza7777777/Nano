using CyberDefense.Simulation;
using CyberDefense.Spatial;
using CyberDefense.Visuals;
using UnityEngine;

namespace CyberDefense.Entities
{
    public abstract class NetworkEntity : MonoBehaviour, IQuadTreeEntity
    {
        [SerializeField] private EntityKind kind;
        [SerializeField] private CyberEntityConfig config;
        [SerializeField] private float spatialRadius = 0.35f;

        protected Vector2 Velocity;

        public EntityKind Kind => kind;
        public CyberEntityConfig Config => config;
        public CyberDefenseSimulation Simulation { get; private set; }
        public float Health { get; protected set; }
        public float Energy { get; protected set; }
        public float Integrity { get; protected set; } = 100f;
        public float HealthRatio => Mathf.Clamp01(Health / (config != null ? config.maxHealth : 100f));
        public float EnergyRatio => Mathf.Clamp01(Energy / Mathf.Max(1f, config != null ? config.maxEnergy : 100f));
        public float IntegrityRatio => Mathf.Clamp01(Integrity / 100f);
        public Vector2 Position2D => transform.position;
        public float SpatialRadius => spatialRadius;
        public bool IsSpatiallyActive => isActiveAndEnabled;

        public void Initialize(CyberDefenseSimulation simulation, EntityKind entityKind, CyberEntityConfig entityConfig)
        {
            Simulation = simulation;
            kind = entityKind;
            config = entityConfig;
            Health = config != null ? config.maxHealth : 100f;
            Energy = config != null ? config.maxEnergy : 100f;
            ApplyColor(config != null ? config.color : Color.white);
            EnsureVisualController();
        }

        public virtual void ReceiveDamage(float amount)
        {
            Health -= amount;
            if (Health <= 0f)
            {
                Color burstColor = config != null ? config.color : Color.white;
                ParticleVisualFactory.CreateDestructionBurst(transform.position, burstColor);
                Simulation?.Unregister(this);
                Destroy(gameObject);
            }
        }

        public void ShowInteractionBeam(NetworkEntity target, Color color)
        {
            if (target != null)
            {
                EnergyBeamEffect.Spawn(transform.position, target.transform.position, color);
            }
        }

        public void PulseRepairVisual(float intensity)
        {
            EntityVisualController visual = GetComponent<EntityVisualController>();
            if (visual != null)
            {
                visual.PulseRepair(intensity);
            }
        }

        protected virtual void OnEnable()
        {
            if (Simulation != null)
            {
                Simulation.Register(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (Simulation != null)
            {
                Simulation.Unregister(this);
            }
        }

        protected void MoveToward(Vector2 target, float speedMultiplier = 1f)
        {
            Vector2 desired = target - Position2D;
            if (desired.sqrMagnitude > 0.001f)
            {
                Velocity = desired.normalized * GetSpeed() * speedMultiplier;
            }
        }

        protected void MoveAwayFrom(Vector2 threat, float speedMultiplier = 1f)
        {
            Vector2 desired = Position2D - threat;
            if (desired.sqrMagnitude > 0.001f)
            {
                Velocity = desired.normalized * GetSpeed() * speedMultiplier;
            }
        }

        protected void Wander(float strength = 1f)
        {
            Vector2 jitter = Random.insideUnitCircle.normalized * GetSpeed() * strength;
            Velocity = Vector2.Lerp(Velocity, jitter, Time.deltaTime * 1.5f);
        }

        protected void ApplyMovement()
        {
            transform.position += (Vector3)(Velocity * Time.deltaTime);
            if (Simulation != null)
            {
                transform.position = Simulation.ClampToWorld(transform.position);
                Simulation.UpdateSpatial(this);
            }
        }

        protected float GetSpeed()
        {
            return config != null ? config.speed : 2f;
        }

        protected float GetPerceptionRadius()
        {
            return config != null ? config.perceptionRadius : 6f;
        }

        protected float GetInteractionRadius()
        {
            return config != null ? config.interactionRadius : 0.7f;
        }

        protected void DrainEnergy(float amount)
        {
            Energy = Mathf.Max(0f, Energy - amount);
        }

        protected void RestoreEnergy(float amount)
        {
            float max = config != null ? config.maxEnergy : 100f;
            Energy = Mathf.Min(max, Energy + amount);
        }

        private void ApplyColor(Color color)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        private void EnsureVisualController()
        {
            EntityVisualController visual = GetComponent<EntityVisualController>();
            if (visual == null)
            {
                visual = gameObject.AddComponent<EntityVisualController>();
            }

            visual.Initialize(this);
        }
    }
}
