using CyberDefense.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace CyberDefense.Visuals
{
    public sealed class CyberDefenseStartScreen : MonoBehaviour
    {
        [SerializeField] private CyberDefenseSimulation simulation;

        private Canvas canvas;
        private Text promptText;
        private float pulse;

        private void Awake()
        {
            if (simulation == null)
            {
                simulation = GetComponent<CyberDefenseSimulation>();
            }

            BuildScreen();
            EnsureEventSystem();
        }

        private void Update()
        {
            pulse += Time.unscaledDeltaTime;
            if (promptText != null)
            {
                float alpha = 0.55f + Mathf.Sin(pulse * 3.2f) * 0.25f;
                promptText.color = new Color(0.65f, 1f, 1f, alpha);
            }

            if (WasStartPressed())
            {
                StartSimulation();
            }
        }

        private void StartSimulation()
        {
            if (simulation != null)
            {
                simulation.BeginSimulation();
            }

            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }

            Destroy(this);
        }

        private void BuildScreen()
        {
            GameObject canvasObject = new GameObject("Cyber Defense Start Screen");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            Image backdrop = CreateImage("Backdrop", canvasObject.transform, new Color(0.005f, 0.008f, 0.014f, 0.96f));
            Stretch(backdrop.rectTransform);

            Image grid = CreateImage("Diagnostic Grid", canvasObject.transform, new Color(0f, 0.75f, 0.9f, 0.08f));
            Stretch(grid.rectTransform);
            grid.sprite = BuildGridSprite();
            grid.type = Image.Type.Tiled;

            RectTransform panel = CreatePanel(canvasObject.transform);
            Text title = CreateText("NANO DEFENSE GRID", panel, 42, new Color(0.68f, 1f, 1f, 1f));
            title.rectTransform.anchoredPosition = new Vector2(0f, 112f);

            Text subtitle = CreateText("AUTONOMOUS MICRO-CIRCUIT SECURITY SIMULATION", panel, 16, new Color(0.92f, 0.96f, 1f, 0.82f));
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, 68f);

            Text diagnostics = CreateText("QUAD-TREE SPATIAL INDEX  //  SENTINEL WORKERS  //  MALWARE PACK AI  //  REPAIR DRONE FLOCKING", panel, 13, new Color(0.2f, 1f, 0.55f, 0.78f));
            diagnostics.rectTransform.anchoredPosition = new Vector2(0f, 28f);

            Button button = CreateButton(panel);
            button.onClick.AddListener(StartSimulation);

            promptText = CreateText("PRESS ENTER OR CLICK BOOT SIMULATION", panel, 13, new Color(0.65f, 1f, 1f, 0.75f));
            promptText.rectTransform.anchoredPosition = new Vector2(0f, -120f);
        }

        private RectTransform CreatePanel(Transform parent)
        {
            GameObject panelObject = new GameObject("Boot Panel");
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 330f);
            rect.anchoredPosition = Vector2.zero;

            Image frame = panelObject.AddComponent<Image>();
            frame.color = new Color(0.02f, 0.06f, 0.08f, 0.72f);
            frame.sprite = ProceduralVisualAssets.Disc;

            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.95f, 1f, 0.42f);
            outline.effectDistance = new Vector2(2f, -2f);
            return rect;
        }

        private Button CreateButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("Boot Simulation Button");
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(270f, 56f);
            rect.anchoredPosition = new Vector2(0f, -54f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.02f, 0.38f, 0.48f, 0.9f);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.02f, 0.38f, 0.48f, 0.9f);
            colors.highlightedColor = new Color(0.05f, 0.78f, 0.88f, 1f);
            colors.pressedColor = new Color(0.05f, 1f, 0.55f, 1f);
            button.colors = colors;

            Text text = CreateText("BOOT SIMULATION", rect, 20, Color.white);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.rectTransform.anchoredPosition = Vector2.zero;
            return button;
        }

        private Text CreateText(string value, Transform parent, int size, Color color)
        {
            GameObject textObject = new GameObject(value);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(720f, 42f);
            return text;
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Sprite BuildGridSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            Color clear = Color.clear;
            Color line = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool trace = x == 0 || y == 0 || x == 31 || y == 31;
                    texture.SetPixel(x, y, trace ? line : clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private bool WasStartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return);
#else
            return false;
#endif
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
