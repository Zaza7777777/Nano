using UnityEngine;

namespace CyberDefense.Visuals
{
    public sealed class CircuitBoardBackground : MonoBehaviour
    {
        [SerializeField] private Vector2 worldSize = new Vector2(44f, 28f);
        private Material material;
        private Vector2 offset;

        private void Awake()
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Quad);
            board.name = "Subtle Scrolling Circuit Board";
            board.transform.SetParent(transform, false);
            board.transform.localPosition = new Vector3(0f, 0f, 4f);
            board.transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);

            Renderer renderer = board.GetComponent<Renderer>();
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            material = new Material(shader);
            material.mainTexture = BuildTexture();
            renderer.material = material;
            Destroy(board.GetComponent<Collider>());
        }

        private void Update()
        {
            if (material == null)
            {
                return;
            }

            offset += new Vector2(0.006f, 0.003f) * Time.deltaTime;
            material.mainTextureOffset = offset;
            material.mainTextureScale = new Vector2(3.5f, 2.2f);
        }

        private Texture2D BuildTexture()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            Color baseColor = new Color(0.005f, 0.008f, 0.012f, 1f);
            Color traceColor = new Color(0.03f, 0.16f, 0.18f, 1f);
            Color lightColor = new Color(0.05f, 0.5f, 0.62f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool trace = x % 64 == 8 && y > 20 && y < 230 || y % 64 == 36 && x > 18 && x < 240;
                    bool branch = x % 64 > 8 && x % 64 < 36 && y % 64 == 12;
                    bool node = Mathf.Abs((x % 64) - 8) < 3 && Mathf.Abs((y % 64) - 36) < 3;
                    Color color = baseColor;
                    if (trace || branch)
                    {
                        color = traceColor;
                    }

                    if (node)
                    {
                        color = lightColor;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
