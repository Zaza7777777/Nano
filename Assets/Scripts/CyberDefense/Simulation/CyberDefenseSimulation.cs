using System.Collections.Generic;
using CyberDefense.Entities;
using CyberDefense.Spatial;
using CyberDefense.Visuals;
using UnityEngine;

namespace CyberDefense.Simulation
{
    public sealed class CyberDefenseSimulation : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private Vector2 worldSize = new Vector2(36f, 22f);
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool drawQuadTree = true;
        [SerializeField] private Color quadTreeColor = new Color(0.1f, 0.85f, 1f, 0.75f);

        [Header("Population")]
        [SerializeField] private int sentinelCount = 16;
        [SerializeField] private int malwareCount = 9;
        [SerializeField] private int droneCount = 5;
        [SerializeField] private int dataPacketCount = 28;
        [SerializeField] private int firewallCount = 7;
        [SerializeField] private int corruptedNodeCount = 4;

        [Header("Optional Scriptable Configs")]
        [SerializeField] private CyberEntityConfig sentinelConfig;
        [SerializeField] private CyberEntityConfig malwareConfig;
        [SerializeField] private CyberEntityConfig droneConfig;
        [SerializeField] private CyberEntityConfig packetConfig;
        [SerializeField] private CyberEntityConfig hubConfig;
        [SerializeField] private CyberEntityConfig firewallConfig;
        [SerializeField] private CyberEntityConfig corruptedNodeConfig;

        private readonly List<NetworkEntity> queryScratch = new List<NetworkEntity>();
        private readonly List<NetworkEntity> allEntities = new List<NetworkEntity>();
        private readonly List<Rect> quadTreeRects = new List<Rect>();
        private QuadTree<NetworkEntity> quadTree;
        private Sprite runtimeSprite;
        private float overloadTimer;
        private float quadTreeOverlayAlpha = 1f;
        private float quadTreeTargetAlpha = 1f;
        private Rect activeQueryArea;
        private float activeQueryTimer;

        public CentralHub Hub { get; private set; }
        public bool SystemOverload { get; private set; }
        public QuadTree<NetworkEntity> ActiveQuadTree => quadTree;
        public Rect WorldBounds => new Rect(-worldSize.x * 0.5f, -worldSize.y * 0.5f, worldSize.x, worldSize.y);

        private void Awake()
        {
            quadTree = new QuadTree<NetworkEntity>(WorldBounds, 5, 7);
            runtimeSprite = BuildSprite();
            BuildRuntimeConfigs();
            EnsureBackground();
            EnsureQuadTreeRenderer();
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                PopulateWorld();
            }
        }

        private void Update()
        {
            UpdateSystemOverload();
            quadTreeOverlayAlpha = Mathf.MoveTowards(quadTreeOverlayAlpha, quadTreeTargetAlpha, Time.deltaTime * 1.8f);
            activeQueryTimer = Mathf.Max(0f, activeQueryTimer - Time.deltaTime);
        }

        public void ToggleQuadTreeDraw()
        {
            drawQuadTree = !drawQuadTree;
            quadTreeTargetAlpha = drawQuadTree ? 1f : 0f;
        }

        public void Register(NetworkEntity entity)
        {
            if (entity == null || allEntities.Contains(entity))
            {
                return;
            }

            allEntities.Add(entity);
            quadTree.Insert(entity);
        }

        public void Unregister(NetworkEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            allEntities.Remove(entity);
            quadTree.Remove(entity);
        }

        public void UpdateSpatial(NetworkEntity entity)
        {
            quadTree.Update(entity);
        }

        public void Query(Vector2 center, float radius, List<NetworkEntity> results)
        {
            results.Clear();
            float diameter = radius * 2f;
            activeQueryArea = new Rect(center.x - radius, center.y - radius, diameter, diameter);
            activeQueryTimer = 0.12f;
            quadTree.Query(center, radius, results);
        }

        public int CountSentinelsNear(Vector2 center, float radius)
        {
            queryScratch.Clear();
            quadTree.Query(center, radius, queryScratch);
            int count = 0;
            for (int i = 0; i < queryScratch.Count; i++)
            {
                if (queryScratch[i].Kind == EntityKind.Sentinel)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountRepairDronesNear(Vector2 center, float radius)
        {
            queryScratch.Clear();
            quadTree.Query(center, radius, queryScratch);
            int count = 0;
            for (int i = 0; i < queryScratch.Count; i++)
            {
                if (queryScratch[i].Kind == EntityKind.RepairDrone)
                {
                    count++;
                }
            }

            return count;
        }

        public Vector3 ClampToWorld(Vector3 position)
        {
            Rect bounds = WorldBounds;
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
            position.y = Mathf.Clamp(position.y, bounds.yMin, bounds.yMax);
            position.z = 0f;
            return position;
        }

        public Vector2 GetPatrolPoint(float t)
        {
            float x = Mathf.Sin(t * 1.7f) * worldSize.x * 0.35f;
            float y = Mathf.Cos(t * 1.1f) * worldSize.y * 0.35f;
            return new Vector2(x, y);
        }

        public void SpawnDataPacket(Vector2 position)
        {
            CreateEntity<DataPacket>("Data Packet", EntityKind.DataPacket, packetConfig, position, 0.34f);
        }

        public void SpawnCorruptedNode(Vector2 position)
        {
            if (Random.value < 0.55f)
            {
                CreateEntity<CorruptedNode>("Corrupted Node", EntityKind.CorruptedNode, corruptedNodeConfig, position, 0.65f);
            }
        }

        private void PopulateWorld()
        {
            Hub = CreateEntity<CentralHub>("Central Hub", EntityKind.CentralHub, hubConfig, Vector2.zero, 1.35f);

            for (int i = 0; i < sentinelCount; i++)
            {
                CreateEntity<DataSentinel>("Data Sentinel", EntityKind.Sentinel, sentinelConfig, RandomPointNear(Vector2.zero, 6f), 0.45f);
            }

            for (int i = 0; i < malwareCount; i++)
            {
                Vector2 side = Random.value > 0.5f ? Vector2.right : Vector2.left;
                CreateEntity<MalwareBug>("Malware Bug", EntityKind.Malware, malwareConfig, RandomPointNear(side * worldSize.x * 0.34f, 5f), 0.42f);
            }

            for (int i = 0; i < droneCount; i++)
            {
                CreateEntity<RepairDrone>("Repair Drone", EntityKind.RepairDrone, droneConfig, RandomPointNear(Vector2.zero, 8f), 0.4f);
            }

            for (int i = 0; i < dataPacketCount; i++)
            {
                SpawnDataPacket(RandomPoint());
            }

            for (int i = 0; i < firewallCount; i++)
            {
                CreateEntity<Firewall>("Firewall", EntityKind.Firewall, firewallConfig, RandomPoint(), 0.8f);
            }

            for (int i = 0; i < corruptedNodeCount; i++)
            {
                CreateEntity<CorruptedNode>("Corrupted Node", EntityKind.CorruptedNode, corruptedNodeConfig, RandomPoint(), 0.65f);
            }
        }

        private T CreateEntity<T>(string label, EntityKind kind, CyberEntityConfig config, Vector2 position, float scale) where T : NetworkEntity
        {
            GameObject instance = new GameObject(label);
            instance.transform.SetParent(transform);
            instance.transform.position = ClampToWorld(position);
            instance.transform.localScale = Vector3.one * scale;

            SpriteRenderer spriteRenderer = instance.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = runtimeSprite;
            spriteRenderer.sortingOrder = GetSortingOrder(kind);

            T entity = instance.AddComponent<T>();
            entity.Initialize(this, kind, config);
            Register(entity);
            return entity;
        }

        private int GetSortingOrder(EntityKind kind)
        {
            switch (kind)
            {
                case EntityKind.CentralHub:
                case EntityKind.Firewall:
                    return 0;
                case EntityKind.DataPacket:
                case EntityKind.CorruptedNode:
                    return 1;
                default:
                    return 2;
            }
        }

        private void UpdateSystemOverload()
        {
            overloadTimer -= Time.deltaTime;
            if (overloadTimer <= 0f)
            {
                int malware = CountEntities(EntityKind.Malware);
                int sentinels = Mathf.Max(1, CountEntities(EntityKind.Sentinel));
                SystemOverload = malware > sentinels * 0.55f || CountUnrepairedNodes() >= 7;
                overloadTimer = 1f;
            }
        }

        private int CountEntities(EntityKind kind)
        {
            int count = 0;
            for (int i = allEntities.Count - 1; i >= 0; i--)
            {
                if (allEntities[i] == null)
                {
                    allEntities.RemoveAt(i);
                    continue;
                }

                if (allEntities[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountUnrepairedNodes()
        {
            int count = 0;
            for (int i = allEntities.Count - 1; i >= 0; i--)
            {
                CorruptedNode node = allEntities[i] as CorruptedNode;
                if (node != null && !node.IsRepaired)
                {
                    count++;
                }
            }

            return count;
        }

        private Vector2 RandomPoint()
        {
            Rect bounds = WorldBounds;
            return new Vector2(Random.Range(bounds.xMin, bounds.xMax), Random.Range(bounds.yMin, bounds.yMax));
        }

        private Vector2 RandomPointNear(Vector2 center, float radius)
        {
            return ClampToWorld(center + Random.insideUnitCircle * radius);
        }

        private Sprite BuildSprite()
        {
            Texture2D texture = Texture2D.whiteTexture;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
        }

        private void BuildRuntimeConfigs()
        {
            if (sentinelConfig == null)
            {
                sentinelConfig = Config("Sentinel Config", new Color(0.15f, 0.9f, 1f), 100f, 100f, 3.2f, 8.5f, 0.75f);
            }

            if (malwareConfig == null)
            {
                malwareConfig = Config("Malware Config", new Color(1f, 0.18f, 0.25f), 80f, 80f, 2.8f, 9.5f, 0.65f);
            }

            if (droneConfig == null)
            {
                droneConfig = Config("Repair Drone Config", new Color(0.35f, 1f, 0.38f), 90f, 120f, 3.5f, 10f, 0.7f);
            }

            if (packetConfig == null)
            {
                packetConfig = Config("Data Packet Config", new Color(1f, 0.85f, 0.15f), 1f, 1f, 0f, 0f, 0.4f);
            }

            if (hubConfig == null)
            {
                hubConfig = Config("Hub Config", new Color(0.95f, 0.95f, 1f), 600f, 500f, 0f, 12f, 1.25f);
            }

            if (firewallConfig == null)
            {
                firewallConfig = Config("Firewall Config", new Color(0.2f, 0.45f, 1f), 250f, 1f, 0f, 0f, 0.8f);
            }

            if (corruptedNodeConfig == null)
            {
                corruptedNodeConfig = Config("Corrupted Node Config", new Color(0.75f, 0.12f, 1f), 120f, 1f, 0f, 0f, 0.8f);
            }
        }

        private CyberEntityConfig Config(string name, Color color, float health, float energy, float speed, float perception, float interaction)
        {
            CyberEntityConfig config = ScriptableObject.CreateInstance<CyberEntityConfig>();
            config.name = name;
            config.color = color;
            config.maxHealth = health;
            config.maxEnergy = energy;
            config.speed = speed;
            config.perceptionRadius = perception;
            config.interactionRadius = interaction;
            return config;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.18f, 0.22f, 0.24f, 0.45f);
            Rect bounds = WorldBounds;
            Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.size.x, bounds.size.y, 0.01f));

            if (quadTree != null && quadTreeOverlayAlpha > 0.01f)
            {
                quadTree.GetDebugRects(quadTreeRects);
                for (int i = 0; i < quadTreeRects.Count; i++)
                {
                    Rect rect = quadTreeRects[i];
                    bool queried = activeQueryTimer > 0f && rect.Overlaps(activeQueryArea);
                    Color color = queried
                        ? new Color(0.2f, 0.74f, 1f, 0.82f * quadTreeOverlayAlpha)
                        : new Color(1f, 1f, 1f, 0.22f * quadTreeOverlayAlpha);
                    Gizmos.color = color;
                    Gizmos.DrawWireCube(rect.center, new Vector3(rect.size.x, rect.size.y, 0.01f));
                }
            }
        }

        private void EnsureBackground()
        {
            if (GetComponent<CircuitBoardBackground>() == null)
            {
                gameObject.AddComponent<CircuitBoardBackground>();
            }
        }

        private void EnsureQuadTreeRenderer()
        {
            if (GetComponent<QuadTreeRenderer>() == null)
            {
                gameObject.AddComponent<QuadTreeRenderer>();
            }
        }
    }
}
