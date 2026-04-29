using System.Collections.Generic;
using UnityEngine;

namespace CyberDefense.Spatial
{
    public sealed class QuadTree<T> where T : class, IQuadTreeEntity
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

        private readonly int capacity;
        private readonly int maxDepth;
        private Node root;
        private readonly Dictionary<T, Rect> itemBounds = new Dictionary<T, Rect>();

        public Rect Bounds => root.Bounds;
        public int Count => itemBounds.Count;

        public QuadTree(Rect bounds, int capacity = 6, int maxDepth = 7)
        {
            this.capacity = Mathf.Max(1, capacity);
            this.maxDepth = Mathf.Max(1, maxDepth);
            root = new Node(bounds, 0);
        }

        public bool Insert(T item)
        {
            if (item == null || !item.IsSpatiallyActive)
            {
                return false;
            }

            Rect bounds = GetEntityBounds(item);
            EnsureRootContains(bounds);

            if (!Insert(root, item, bounds))
            {
                return false;
            }

            itemBounds[item] = bounds;
            return true;
        }

        public bool Remove(T item)
        {
            if (item == null || !itemBounds.Remove(item))
            {
                return false;
            }

            bool removed = Remove(root, item);
            Collapse(root);
            return removed;
        }

        public bool Update(T item)
        {
            if (item == null)
            {
                return false;
            }

            if (!item.IsSpatiallyActive)
            {
                return Remove(item);
            }

            Rect newBounds = GetEntityBounds(item);
            if (itemBounds.TryGetValue(item, out Rect oldBounds) && oldBounds == newBounds && root.Bounds.Contains(newBounds.min) && root.Bounds.Contains(newBounds.max))
            {
                return true;
            }

            Remove(item);
            return Insert(item);
        }

        public List<T> Query(Vector2 center, float radius)
        {
            List<T> results = new List<T>();
            Query(center, radius, results);
            return results;
        }

        public void Query(Vector2 center, float radius, List<T> results)
        {
            float diameter = radius * 2f;
            Rect range = new Rect(center.x - radius, center.y - radius, diameter, diameter);
            float radiusSqr = radius * radius;
            QueryCircle(root, center, radiusSqr, range, results);
        }

        public List<T> Query(Rect area)
        {
            List<T> results = new List<T>();
            Query(area, results);
            return results;
        }

        public void Query(Rect area, List<T> results)
        {
            QueryRect(root, area, results);
        }

        public void Clear()
        {
            root = new Node(root.Bounds, 0);
            itemBounds.Clear();
        }

        public void GetDebugRects(List<Rect> rects)
        {
            rects.Clear();
            CollectDebugRects(root, rects);
        }

        public void DebugDraw(Color color)
        {
            Color previous = Gizmos.color;
            Gizmos.color = color;
            DebugDraw(root);
            Gizmos.color = previous;
        }

        private static Rect GetEntityBounds(T item)
        {
            Vector2 position = item.Position2D;
            float radius = Mathf.Max(0.05f, item.SpatialRadius);
            return new Rect(position.x - radius, position.y - radius, radius * 2f, radius * 2f);
        }

        private static bool FullyContains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
        }

        private static bool IntersectsCircle(Rect rect, Vector2 center, float radiusSqr)
        {
            float x = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
            float y = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
            return ((Vector2)new Vector2(x, y) - center).sqrMagnitude <= radiusSqr;
        }

        private void EnsureRootContains(Rect bounds)
        {
            while (!FullyContains(root.Bounds, bounds))
            {
                Rect old = root.Bounds;
                float size = Mathf.Max(old.width, old.height) * 2f;
                Vector2 center = old.center;
                if (bounds.center.x < old.center.x)
                {
                    center.x -= old.width * 0.5f;
                }
                else
                {
                    center.x += old.width * 0.5f;
                }

                if (bounds.center.y < old.center.y)
                {
                    center.y -= old.height * 0.5f;
                }
                else
                {
                    center.y += old.height * 0.5f;
                }

                Node newRoot = new Node(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), 0);
                Insert(newRoot, root);
                root = newRoot;
            }
        }

        private void Insert(Node parent, Node existingNode)
        {
            List<T> items = new List<T>();
            Collect(existingNode, items);
            foreach (T item in items)
            {
                Insert(parent, item, GetEntityBounds(item));
            }
        }

        private bool Insert(Node node, T item, Rect bounds)
        {
            if (!FullyContains(node.Bounds, bounds))
            {
                return false;
            }

            if (!node.IsLeaf)
            {
                int childIndex = GetContainingChildIndex(node, bounds);
                if (childIndex >= 0)
                {
                    return Insert(node.Children[childIndex], item, bounds);
                }
            }

            node.Items.Add(item);

            if (node.Items.Count > capacity && node.Depth < maxDepth)
            {
                Split(node);
            }

            return true;
        }

        private bool Remove(Node node, T item)
        {
            if (node.Items.Remove(item))
            {
                return true;
            }

            if (node.IsLeaf)
            {
                return false;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                if (Remove(node.Children[i], item))
                {
                    return true;
                }
            }

            return false;
        }

        private void Split(Node node)
        {
            if (!node.IsLeaf)
            {
                return;
            }

            node.Children = new Node[4];
            Vector2 min = node.Bounds.min;
            Vector2 half = node.Bounds.size * 0.5f;
            int depth = node.Depth + 1;
            node.Children[0] = new Node(new Rect(min.x, min.y + half.y, half.x, half.y), depth);
            node.Children[1] = new Node(new Rect(min.x + half.x, min.y + half.y, half.x, half.y), depth);
            node.Children[2] = new Node(new Rect(min.x, min.y, half.x, half.y), depth);
            node.Children[3] = new Node(new Rect(min.x + half.x, min.y, half.x, half.y), depth);

            for (int i = node.Items.Count - 1; i >= 0; i--)
            {
                T item = node.Items[i];
                Rect bounds = GetEntityBounds(item);
                int childIndex = GetContainingChildIndex(node, bounds);
                if (childIndex < 0)
                {
                    continue;
                }

                node.Items.RemoveAt(i);
                Insert(node.Children[childIndex], item, bounds);
            }
        }

        private int GetContainingChildIndex(Node node, Rect bounds)
        {
            if (node.IsLeaf)
            {
                return -1;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                if (FullyContains(node.Children[i].Bounds, bounds))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Collapse(Node node)
        {
            if (node.IsLeaf)
            {
                return;
            }

            int total = node.Items.Count;
            for (int i = 0; i < node.Children.Length; i++)
            {
                Collapse(node.Children[i]);
                total += CountItems(node.Children[i]);
            }

            if (total > capacity)
            {
                return;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                Collect(node.Children[i], node.Items);
            }

            node.Children = null;
        }

        private int CountItems(Node node)
        {
            int total = node.Items.Count;
            if (node.IsLeaf)
            {
                return total;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                total += CountItems(node.Children[i]);
            }

            return total;
        }

        private void Collect(Node node, List<T> items)
        {
            items.AddRange(node.Items);
            if (node.IsLeaf)
            {
                return;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                Collect(node.Children[i], items);
            }
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
                if (item != null && item.IsSpatiallyActive && (item.Position2D - center).sqrMagnitude <= radiusSqr)
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

        private void QueryRect(Node node, Rect area, List<T> results)
        {
            if (!node.Bounds.Overlaps(area))
            {
                return;
            }

            for (int i = 0; i < node.Items.Count; i++)
            {
                T item = node.Items[i];
                if (item != null && item.IsSpatiallyActive && area.Contains(item.Position2D))
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
                QueryRect(node.Children[i], area, results);
            }
        }

        private void DebugDraw(Node node)
        {
            Vector3 center = new Vector3(node.Bounds.center.x, node.Bounds.center.y, 0f);
            Vector3 size = new Vector3(node.Bounds.size.x, node.Bounds.size.y, 0.01f);
            Gizmos.DrawWireCube(center, size);

            if (node.IsLeaf)
            {
                return;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                DebugDraw(node.Children[i]);
            }
        }

        private void CollectDebugRects(Node node, List<Rect> rects)
        {
            rects.Add(node.Bounds);
            if (node.IsLeaf)
            {
                return;
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                CollectDebugRects(node.Children[i], rects);
            }
        }
    }
}
