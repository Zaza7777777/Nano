using System.Collections.Generic;
using UnityEngine;

namespace MicroscopicCircuitDefense
{
    public interface IQuadTreeItem
    {
        Vector2 Position { get; }
        float Radius { get; }
        bool IsActive { get; }
    }

    public sealed class QuadTree<T> where T : class, IQuadTreeItem
    {
        private sealed class Node
        {
            public readonly Rect Bounds;
            public readonly int Depth;
            public readonly List<T> Items = new List<T>();
            public Node[] Children;

            public bool IsLeaf => Children == null;

            public Node(Rect bounds, int depth)
            {
                Bounds = bounds;
                Depth = depth;
            }
        }

        public static bool DrawGizmos = true;

        private readonly int capacity;
        private readonly int maxDepth;
        private Node root;

        public QuadTree(Rect bounds, int capacity = 6, int maxDepth = 7)
        {
            this.capacity = Mathf.Max(1, capacity);
            this.maxDepth = Mathf.Max(1, maxDepth);
            root = new Node(bounds, 0);
        }

        public void Clear(Rect bounds)
        {
            root = new Node(bounds, 0);
        }

        public bool Insert(T item)
        {
            if (item == null || !item.IsActive)
            {
                return false;
            }

            Rect itemBounds = GetItemBounds(item);
            if (!Contains(root.Bounds, itemBounds))
            {
                return false;
            }

            return Insert(root, item, itemBounds);
        }

        public void Query(Vector2 center, float radius, List<T> results)
        {
            float diameter = radius * 2f;
            Rect area = new Rect(center.x - radius, center.y - radius, diameter, diameter);
            QueryCircle(root, center, radius * radius, area, results);
        }

        public void Query(Rect bounds, List<T> results)
        {
            QueryBounds(root, bounds, results);
        }

        public void DrawDebugGizmos()
        {
            if (!DrawGizmos)
            {
                return;
            }

            Color previous = Gizmos.color;
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            DrawNode(root);
            Gizmos.color = previous;
        }

        private static Rect GetItemBounds(T item)
        {
            float radius = Mathf.Max(0.05f, item.Radius);
            Vector2 position = item.Position;
            return new Rect(position.x - radius, position.y - radius, radius * 2f, radius * 2f);
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
        }

        private static bool IntersectsCircle(Rect rect, Vector2 center, float radiusSqr)
        {
            float closestX = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
            float closestY = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
            return (new Vector2(closestX, closestY) - center).sqrMagnitude <= radiusSqr;
        }

        private bool Insert(Node node, T item, Rect itemBounds)
        {
            if (!node.IsLeaf)
            {
                int childIndex = FindContainingChild(node, itemBounds);
                if (childIndex >= 0)
                {
                    return Insert(node.Children[childIndex], item, itemBounds);
                }
            }

            node.Items.Add(item);

            if (node.Items.Count > capacity && node.Depth < maxDepth)
            {
                Split(node);
            }

            return true;
        }

        private void Split(Node node)
        {
            if (!node.IsLeaf)
            {
                return;
            }

            Vector2 min = node.Bounds.min;
            Vector2 half = node.Bounds.size * 0.5f;
            int childDepth = node.Depth + 1;
            node.Children = new[]
            {
                new Node(new Rect(min.x, min.y + half.y, half.x, half.y), childDepth),
                new Node(new Rect(min.x + half.x, min.y + half.y, half.x, half.y), childDepth),
                new Node(new Rect(min.x, min.y, half.x, half.y), childDepth),
                new Node(new Rect(min.x + half.x, min.y, half.x, half.y), childDepth)
            };

            for (int i = node.Items.Count - 1; i >= 0; i--)
            {
                T item = node.Items[i];
                int childIndex = FindContainingChild(node, GetItemBounds(item));
                if (childIndex < 0)
                {
                    continue;
                }

                node.Items.RemoveAt(i);
                Insert(node.Children[childIndex], item, GetItemBounds(item));
            }
        }

        private int FindContainingChild(Node node, Rect itemBounds)
        {
            if (node.IsLeaf)
            {
                return -1;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                if (Contains(node.Children[i].Bounds, itemBounds))
                {
                    return i;
                }
            }

            return -1;
        }

        private void QueryCircle(Node node, Vector2 center, float radiusSqr, Rect area, List<T> results)
        {
            if (!node.Bounds.Overlaps(area) || !IntersectsCircle(node.Bounds, center, radiusSqr))
            {
                return;
            }

            for (int i = 0; i < node.Items.Count; i++)
            {
                T item = node.Items[i];
                if (item != null && item.IsActive && (item.Position - center).sqrMagnitude <= radiusSqr)
                {
                    results.Add(item);
                }
            }

            if (node.IsLeaf)
            {
                return;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                QueryCircle(node.Children[i], center, radiusSqr, area, results);
            }
        }

        private void QueryBounds(Node node, Rect bounds, List<T> results)
        {
            if (!node.Bounds.Overlaps(bounds))
            {
                return;
            }

            for (int i = 0; i < node.Items.Count; i++)
            {
                T item = node.Items[i];
                if (item != null && item.IsActive && bounds.Contains(item.Position))
                {
                    results.Add(item);
                }
            }

            if (node.IsLeaf)
            {
                return;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                QueryBounds(node.Children[i], bounds, results);
            }
        }

        private void DrawNode(Node node)
        {
            Gizmos.DrawWireCube(node.Bounds.center, new Vector3(node.Bounds.width, node.Bounds.height, 0.01f));
            if (node.IsLeaf)
            {
                return;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                DrawNode(node.Children[i]);
            }
        }
    }

    public enum NetworkActorKind
    {
        Sentinel,
        Malware,
        GreenFlocker
    }

    public enum NetworkActorState
    {
        Work,
        Cluster,
        Hunt,
        Flock,
        Recharge
    }

    public sealed class NetworkActor : MonoBehaviour, IQuadTreeItem
    {
        [SerializeField] private NetworkActorKind kind;
        [SerializeField] private float radius = 0.35f;
        [SerializeField] private float speed = 3f;
        [SerializeField] private float perceptionRadius = 5.5f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float attackDamagePerSecond = 18f;

        private readonly List<NetworkActor> nearby = new List<NetworkActor>();
        private NetworkDefenseSimulation simulation;
        private SpriteRenderer spriteRenderer;
        private Vector2 velocity;
        private float processProgress;
        private float energy;
        private float health = 100f;

        public NetworkActorKind Kind => kind;
        public NetworkActorState State { get; private set; }
        public Vector2 Position => transform.position;
        public float Radius => radius;
        public bool IsActive => isActiveAndEnabled && health > 0f;
        public float Energy01 => Mathf.Clamp01(energy / maxEnergy);

        public void Initialize(NetworkDefenseSimulation owner, NetworkActorKind actorKind, Sprite sprite)
        {
            simulation = owner;
            kind = actorKind;
            energy = maxEnergy * Random.Range(0.55f, 1f);
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 10;
            spriteRenderer.color = BaseColor();
        }

        public void TickAI(float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            energy = Mathf.Max(0f, energy - EnergyDrainRate() * deltaTime);

            nearby.Clear();
            simulation.QueryActors(Position, perceptionRadius, nearby);

            if (energy <= maxEnergy * 0.18f)
            {
                State = NetworkActorState.Recharge;
            }
            else if (kind == NetworkActorKind.Sentinel)
            {
                State = CountKind(NetworkActorKind.Sentinel, 2.2f) >= 2 ? NetworkActorState.Work : NetworkActorState.Cluster;
            }
            else if (kind == NetworkActorKind.Malware)
            {
                State = NetworkActorState.Hunt;
            }
            else
            {
                State = NetworkActorState.Flock;
            }

            switch (State)
            {
                case NetworkActorState.Recharge:
                    SeekRecharge(deltaTime);
                    break;
                case NetworkActorState.Work:
                    ProcessData(deltaTime);
                    break;
                case NetworkActorState.Cluster:
                    ClusterWithSentinels(deltaTime);
                    break;
                case NetworkActorState.Hunt:
                    PackHunt(deltaTime);
                    break;
                case NetworkActorState.Flock:
                    Flock(deltaTime);
                    break;
            }
        }

        public void Move(float deltaTime)
        {
            transform.position += (Vector3)(velocity * deltaTime);
            transform.position = simulation.ClampToWorld(transform.position);
            velocity = Vector2.Lerp(velocity, Vector2.zero, deltaTime * 0.6f);
            UpdateVisuals();
        }

        public void ReceiveDamage(float damage)
        {
            health -= damage;
            if (health <= 0f)
            {
                simulation.SpawnDigitalBurst(transform.position, BaseColor());
                gameObject.SetActive(false);
            }
        }

        private void ProcessData(float deltaTime)
        {
            int collaborators = Mathf.Max(1, CountKind(NetworkActorKind.Sentinel, 2.2f));
            processProgress += deltaTime * collaborators;
            velocity = Vector2.Lerp(velocity, Vector2.zero, deltaTime * 3f);

            float orbitAngle = Time.time * (1.5f + collaborators * 0.15f) + GetInstanceID();
            velocity += new Vector2(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle)) * 0.15f;
        }

        private void ClusterWithSentinels(float deltaTime)
        {
            Vector2 center = AveragePosition(NetworkActorKind.Sentinel);
            if (center == Position)
            {
                center = simulation.WorldCenter;
            }

            SteerToward(center, speed * 0.85f, deltaTime);
        }

        private void PackHunt(float deltaTime)
        {
            NetworkActor target = FindClosest(NetworkActorKind.Sentinel);
            Vector2 packCenter = AveragePosition(NetworkActorKind.Malware);
            Vector2 desired = Vector2.zero;

            if (target != null)
            {
                desired += (target.Position - Position).normalized * 1.5f;
                if (Vector2.Distance(Position, target.Position) <= radius + target.radius + 0.25f)
                {
                    target.ReceiveDamage(attackDamagePerSecond * deltaTime);
                    simulation.EmitAttackSpark(Position, target.Position);
                }
            }

            if (packCenter != Position)
            {
                desired += (packCenter - Position).normalized * 0.55f;
            }

            if (desired.sqrMagnitude < 0.01f)
            {
                desired = Random.insideUnitCircle.normalized;
            }

            velocity = Vector2.Lerp(velocity, desired.normalized * speed * 1.08f, deltaTime * 4f);
        }

        private void Flock(float deltaTime)
        {
            Vector2 separation = Vector2.zero;
            Vector2 alignment = Vector2.zero;
            Vector2 cohesion = Vector2.zero;
            int count = 0;

            for (int i = 0; i < nearby.Count; i++)
            {
                NetworkActor other = nearby[i];
                if (other == this || other.Kind != NetworkActorKind.GreenFlocker)
                {
                    continue;
                }

                Vector2 offset = Position - other.Position;
                float distance = Mathf.Max(0.15f, offset.magnitude);
                separation += offset.normalized / distance;
                alignment += other.velocity;
                cohesion += other.Position;
                count++;
            }

            Vector2 desired = Vector2.zero;
            if (count > 0)
            {
                alignment /= count;
                cohesion = (cohesion / count - Position).normalized;
                desired += separation * 1.7f;
                desired += alignment.normalized * 0.75f;
                desired += cohesion * 0.85f;
            }

            desired += (simulation.GetPatrolPoint(Time.time * 0.25f) - Position).normalized * 0.65f;
            velocity = Vector2.Lerp(velocity, desired.normalized * speed, deltaTime * 3.2f);
        }

        private void SeekRecharge(float deltaTime)
        {
            Vector2 station = simulation.GetClosestRechargeStation(Position);
            SteerToward(station, speed * 1.15f, deltaTime);
            if (Vector2.Distance(Position, station) < 0.8f)
            {
                energy = Mathf.Min(maxEnergy, energy + maxEnergy * 0.42f * deltaTime);
                velocity *= 0.45f;
            }
        }

        private void SteerToward(Vector2 target, float targetSpeed, float deltaTime)
        {
            Vector2 desired = target - Position;
            if (desired.sqrMagnitude < 0.01f)
            {
                return;
            }

            velocity = Vector2.Lerp(velocity, desired.normalized * targetSpeed, deltaTime * 3.5f);
        }

        private NetworkActor FindClosest(NetworkActorKind targetKind)
        {
            NetworkActor closest = null;
            float closestSqr = float.MaxValue;
            for (int i = 0; i < nearby.Count; i++)
            {
                NetworkActor actor = nearby[i];
                if (actor == this || actor.Kind != targetKind || !actor.IsActive)
                {
                    continue;
                }

                float sqr = (actor.Position - Position).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = actor;
                }
            }

            return closest;
        }

        private int CountKind(NetworkActorKind targetKind, float range)
        {
            int count = 0;
            float rangeSqr = range * range;
            for (int i = 0; i < nearby.Count; i++)
            {
                NetworkActor actor = nearby[i];
                if (actor.Kind == targetKind && (actor.Position - Position).sqrMagnitude <= rangeSqr)
                {
                    count++;
                }
            }

            return count;
        }

        private Vector2 AveragePosition(NetworkActorKind targetKind)
        {
            Vector2 total = Vector2.zero;
            int count = 0;
            for (int i = 0; i < nearby.Count; i++)
            {
                NetworkActor actor = nearby[i];
                if (actor.Kind != targetKind || !actor.IsActive)
                {
                    continue;
                }

                total += actor.Position;
                count++;
            }

            return count == 0 ? Position : total / count;
        }

        private float EnergyDrainRate()
        {
            switch (kind)
            {
                case NetworkActorKind.Malware:
                    return 4.5f;
                case NetworkActorKind.GreenFlocker:
                    return 3f;
                default:
                    return 2.2f;
            }
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Color status = Color.Lerp(Color.red, Color.green, Energy01);
            Color baseColor = BaseColor();
            spriteRenderer.color = Color.Lerp(status, baseColor, 0.55f);

            float pulse = kind == NetworkActorKind.Malware ? Mathf.Sin(Time.time * 12f + GetInstanceID()) * 0.12f : 0f;
            transform.localScale = Vector3.one * (0.75f + Radius + pulse);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }

        private Color BaseColor()
        {
            switch (kind)
            {
                case NetworkActorKind.Malware:
                    return new Color(1f, 0.12f, 0.16f, 1f);
                case NetworkActorKind.GreenFlocker:
                    return new Color(0.22f, 1f, 0.42f, 1f);
                default:
                    return new Color(1f, 0.86f, 0.08f, 1f);
            }
        }
    }

    public sealed class RechargeStation : MonoBehaviour
    {
        public Vector2 Position => transform.position;
    }

    public sealed class NetworkDefenseSimulation : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private Vector2 worldSize = new Vector2(34f, 20f);
        [SerializeField] private float fixedLogicStep = 0.08f;

        [Header("Population")]
        [SerializeField] private int sentinelCount = 18;
        [SerializeField] private int malwareCount = 8;
        [SerializeField] private int greenFlockerCount = 14;
        [SerializeField] private int rechargeStationCount = 3;

        private readonly List<NetworkActor> actors = new List<NetworkActor>();
        private readonly List<RechargeStation> stations = new List<RechargeStation>();
        private QuadTree<NetworkActor> tree;
        private Sprite discSprite;
        private Sprite jaggedSprite;
        private ParticleSystem sparkTemplate;
        private float logicAccumulator;

        public Vector2 WorldCenter => Vector2.zero;
        private Rect WorldBounds => new Rect(-worldSize.x * 0.5f, -worldSize.y * 0.5f, worldSize.x, worldSize.y);

        private void Awake()
        {
            tree = new QuadTree<NetworkActor>(WorldBounds);
            discSprite = BuildDiscSprite();
            jaggedSprite = BuildJaggedSprite();
            sparkTemplate = BuildSparkTemplate();
            SpawnWorld();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                QuadTree<NetworkActor>.DrawGizmos = !QuadTree<NetworkActor>.DrawGizmos;
            }

            RebuildTree();

            logicAccumulator += Time.deltaTime;
            while (logicAccumulator >= fixedLogicStep)
            {
                for (int i = 0; i < actors.Count; i++)
                {
                    actors[i].TickAI(fixedLogicStep);
                }

                logicAccumulator -= fixedLogicStep;
            }

            for (int i = 0; i < actors.Count; i++)
            {
                actors[i].Move(Time.deltaTime);
            }
        }

        public void QueryActors(Vector2 center, float radius, List<NetworkActor> results)
        {
            results.Clear();
            tree.Query(center, radius, results);
        }

        public Vector3 ClampToWorld(Vector3 position)
        {
            Rect bounds = WorldBounds;
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
            position.y = Mathf.Clamp(position.y, bounds.yMin, bounds.yMax);
            position.z = 0f;
            return position;
        }

        public Vector2 GetClosestRechargeStation(Vector2 from)
        {
            RechargeStation closest = null;
            float closestSqr = float.MaxValue;
            for (int i = 0; i < stations.Count; i++)
            {
                float sqr = (stations[i].Position - from).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = stations[i];
                }
            }

            return closest != null ? closest.Position : Vector2.zero;
        }

        public Vector2 GetPatrolPoint(float t)
        {
            return new Vector2(Mathf.Sin(t * 1.7f) * worldSize.x * 0.35f, Mathf.Cos(t * 1.2f) * worldSize.y * 0.35f);
        }

        public void EmitAttackSpark(Vector2 start, Vector2 end)
        {
            ParticleSystem sparks = Instantiate(sparkTemplate, (start + end) * 0.5f, Quaternion.identity);
            sparks.gameObject.SetActive(true);
            sparks.Emit(14);
            Destroy(sparks.gameObject, 0.9f);
        }

        public void SpawnDigitalBurst(Vector2 position, Color color)
        {
            ParticleSystem burst = Instantiate(sparkTemplate, position, Quaternion.identity);
            ParticleSystem.MainModule main = burst.main;
            main.startColor = color;
            burst.gameObject.SetActive(true);
            burst.Emit(26);
            Destroy(burst.gameObject, 1f);
        }

        private void RebuildTree()
        {
            tree.Clear(WorldBounds);
            for (int i = actors.Count - 1; i >= 0; i--)
            {
                if (actors[i] == null || !actors[i].IsActive)
                {
                    continue;
                }

                tree.Insert(actors[i]);
            }
        }

        private void SpawnWorld()
        {
            SpawnStations();
            for (int i = 0; i < sentinelCount; i++)
            {
                SpawnActor(NetworkActorKind.Sentinel, Random.insideUnitCircle * 4f);
            }

            for (int i = 0; i < malwareCount; i++)
            {
                Vector2 edge = Random.value > 0.5f ? Vector2.right : Vector2.left;
                SpawnActor(NetworkActorKind.Malware, edge * worldSize.x * 0.38f + Random.insideUnitCircle * 3f);
            }

            for (int i = 0; i < greenFlockerCount; i++)
            {
                SpawnActor(NetworkActorKind.GreenFlocker, RandomPoint());
            }
        }

        private void SpawnActor(NetworkActorKind kind, Vector2 position)
        {
            GameObject actorObject = new GameObject(kind.ToString());
            actorObject.transform.SetParent(transform);
            actorObject.transform.position = ClampToWorld(position);
            NetworkActor actor = actorObject.AddComponent<NetworkActor>();
            actor.Initialize(this, kind, kind == NetworkActorKind.Malware ? jaggedSprite : discSprite);
            actors.Add(actor);
        }

        private void SpawnStations()
        {
            for (int i = 0; i < rechargeStationCount; i++)
            {
                float angle = i * Mathf.PI * 2f / rechargeStationCount;
                Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 7f;
                GameObject stationObject = new GameObject("Recharge Station");
                stationObject.transform.SetParent(transform);
                stationObject.transform.position = position;
                stationObject.transform.localScale = Vector3.one * 1.2f;
                SpriteRenderer renderer = stationObject.AddComponent<SpriteRenderer>();
                renderer.sprite = discSprite;
                renderer.color = new Color(0.18f, 0.62f, 1f, 0.85f);
                renderer.sortingOrder = 4;
                stations.Add(stationObject.AddComponent<RechargeStation>());
            }
        }

        private Vector2 RandomPoint()
        {
            Rect bounds = WorldBounds;
            return new Vector2(Random.Range(bounds.xMin, bounds.xMax), Random.Range(bounds.yMin, bounds.yMax));
        }

        private ParticleSystem BuildSparkTemplate()
        {
            GameObject template = new GameObject("Digital Spark Template");
            template.SetActive(false);
            template.transform.SetParent(transform);
            ParticleSystem particles = template.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.startLifetime = 0.28f;
            main.startSpeed = 2.2f;
            main.startSize = 0.09f;
            main.startColor = new Color(0f, 1f, 1f, 1f);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 30;
            return particles;
        }

        private Sprite BuildDiscSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float alpha = Mathf.SmoothStep(1f, 0f, distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite BuildJaggedSprite()
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = (new Vector2(x, y) - center) / size;
                    float angle = Mathf.Atan2(point.y, point.x);
                    float jag = 0.26f + Mathf.Abs(Mathf.Sin(angle * 5f)) * 0.22f;
                    bool inside = point.magnitude < jag && point.magnitude > 0.04f;
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void OnDrawGizmos()
        {
            if (tree != null)
            {
                tree.DrawDebugGizmos();
            }

            Gizmos.color = new Color(0f, 1f, 1f, 0.08f);
            Rect bounds = WorldBounds;
            Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.width, bounds.height, 0.01f));
        }
    }
}
