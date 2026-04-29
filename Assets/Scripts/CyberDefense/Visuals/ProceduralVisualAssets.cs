using UnityEngine;

namespace CyberDefense.Visuals
{
    public static class ProceduralVisualAssets
    {
        private static Sprite disc;
        private static Sprite gear;
        private static Sprite spark;
        private static Sprite socket;
        private static Material spriteMaterial;
        private static Material additiveMaterial;

        public static Material SpriteMaterial => spriteMaterial != null ? spriteMaterial : spriteMaterial = new Material(Shader.Find("Sprites/Default"));
        public static Material AdditiveMaterial => additiveMaterial != null ? additiveMaterial : additiveMaterial = BuildAdditiveMaterial();
        public static Sprite Disc => disc != null ? disc : disc = BuildDisc();
        public static Sprite Gear => gear != null ? gear : gear = BuildGear();
        public static Sprite Spark => spark != null ? spark : spark = BuildSpark();
        public static Sprite Socket => socket != null ? socket : socket = BuildSocket();

        public static Color StatusColor(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (ratio > 0.55f)
            {
                return Color.Lerp(new Color(1f, 0.86f, 0.18f), new Color(0.2f, 1f, 0.55f), Mathf.InverseLerp(0.55f, 1f, ratio));
            }

            return Color.Lerp(new Color(1f, 0.1f, 0.08f), new Color(1f, 0.86f, 0.18f), Mathf.InverseLerp(0f, 0.55f, ratio));
        }

        private static Material BuildAdditiveMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.SetColor("_Color", Color.white);
            return material;
        }

        private static Sprite BuildDisc()
        {
            const int size = 64;
            Texture2D texture = NewTexture(size);
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

            return FinalizeSprite(texture);
        }

        private static Sprite BuildGear()
        {
            const int size = 96;
            Texture2D texture = NewTexture(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y) - center;
                    float radius = point.magnitude / (size * 0.5f);
                    float angle = Mathf.Atan2(point.y, point.x);
                    float teeth = Mathf.Abs(Mathf.Sin(angle * 6f));
                    float outer = 0.58f + teeth * 0.16f;
                    bool ring = radius < outer && radius > 0.23f;
                    bool hub = radius < 0.16f;
                    float alpha = ring || hub ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            return FinalizeSprite(texture);
        }

        private static Sprite BuildSpark()
        {
            const int size = 96;
            Texture2D texture = NewTexture(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2[] points =
            {
                new Vector2(0f, 0.48f),
                new Vector2(0.13f, 0.08f),
                new Vector2(0.42f, 0.18f),
                new Vector2(0.18f, -0.05f),
                new Vector2(0.34f, -0.46f),
                new Vector2(0f, -0.2f),
                new Vector2(-0.32f, -0.46f),
                new Vector2(-0.17f, -0.04f),
                new Vector2(-0.43f, 0.18f),
                new Vector2(-0.12f, 0.08f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = (new Vector2(x, y) - center) / size;
                    bool inside = IsInsidePolygon(point, points);
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            return FinalizeSprite(texture);
        }

        private static Sprite BuildSocket()
        {
            const int size = 96;
            Texture2D texture = NewTexture(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y) - center;
                    float radius = point.magnitude / (size * 0.5f);
                    float angle = Mathf.Atan2(point.y, point.x);
                    bool ring = radius < 0.62f && radius > 0.47f;
                    bool slot = Mathf.Abs(point.x) < 5f && Mathf.Abs(point.y) < 22f || Mathf.Abs(point.y) < 5f && Mathf.Abs(point.x) < 22f;
                    bool arcs = radius > 0.7f && radius < 0.82f && Mathf.Sin(angle * 5f) > 0.45f;
                    texture.SetPixel(x, y, ring || slot || arcs ? Color.white : Color.clear);
                }
            }

            return FinalizeSprite(texture);
        }

        private static Texture2D NewTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static Sprite FinalizeSprite(Texture2D texture)
        {
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        }

        private static bool IsInsidePolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
