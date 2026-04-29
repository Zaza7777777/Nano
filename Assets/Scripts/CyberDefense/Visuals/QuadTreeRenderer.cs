using System.Collections.Generic;
using CyberDefense.Entities;
using CyberDefense.Simulation;
using CyberDefense.Spatial;
using UnityEngine;

namespace CyberDefense.Visuals
{
    [ExecuteAlways]
    public sealed class QuadTreeRenderer : MonoBehaviour
    {
        [SerializeField] private CyberDefenseSimulation simulation;
        [SerializeField] private Color lineColor = Color.cyan;
        [SerializeField] private bool drawRuntimeLines = true;
        [SerializeField] private bool drawEditorGizmos = true;
        [SerializeField] private float runtimeLineWidth = 0.025f;

        private readonly List<Rect> debugRects = new List<Rect>();
        private readonly List<LineRenderer> runtimeLines = new List<LineRenderer>();
        private Material lineMaterial;

        private void OnEnable()
        {
            FindSimulationIfNeeded();
            BuildLineMaterial();
        }

        private void Update()
        {
            FindSimulationIfNeeded();
            UpdateRuntimeLines();
        }

        private void OnDrawGizmos()
        {
            if (!drawEditorGizmos || !TryGetRects())
            {
                return;
            }

            Color previous = Gizmos.color;
            Gizmos.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.3f);

            for (int i = 0; i < debugRects.Count; i++)
            {
                Rect rect = debugRects[i];
                Gizmos.DrawWireCube(
                    new Vector3(rect.center.x, rect.center.y, 0f),
                    new Vector3(rect.width, rect.height, 0.01f));
            }

            Gizmos.color = previous;
        }

        private void OnDisable()
        {
            HideUnusedLines(0);
        }

        private void UpdateRuntimeLines()
        {
            if (!drawRuntimeLines || !TryGetRects())
            {
                HideUnusedLines(0);
                return;
            }

            EnsureLineCount(debugRects.Count);

            for (int i = 0; i < debugRects.Count; i++)
            {
                LineRenderer line = runtimeLines[i];
                line.enabled = true;
                line.startColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.75f);
                line.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.75f);
                line.startWidth = runtimeLineWidth;
                line.endWidth = runtimeLineWidth;
                SetRectPositions(line, debugRects[i]);
            }

            HideUnusedLines(debugRects.Count);
        }

        private bool TryGetRects()
        {
            FindSimulationIfNeeded();
            if (simulation == null)
            {
                return false;
            }

            QuadTree<NetworkEntity> tree = simulation.ActiveQuadTree;
            if (tree == null)
            {
                return false;
            }

            tree.GetDebugRects(debugRects);
            return debugRects.Count > 0;
        }

        private void EnsureLineCount(int count)
        {
            BuildLineMaterial();
            while (runtimeLines.Count < count)
            {
                GameObject lineObject = new GameObject("Runtime QuadTree Cell");
                lineObject.transform.SetParent(transform, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.loop = true;
                line.positionCount = 4;
                line.material = lineMaterial;
                line.alignment = LineAlignment.View;
                line.textureMode = LineTextureMode.Stretch;
                line.sortingOrder = 100;
                runtimeLines.Add(line);
            }
        }

        private void HideUnusedLines(int activeCount)
        {
            for (int i = activeCount; i < runtimeLines.Count; i++)
            {
                if (runtimeLines[i] != null)
                {
                    runtimeLines[i].enabled = false;
                }
            }
        }

        private void SetRectPositions(LineRenderer line, Rect rect)
        {
            Vector3 bottomLeft = new Vector3(rect.xMin, rect.yMin, 0f);
            Vector3 topLeft = new Vector3(rect.xMin, rect.yMax, 0f);
            Vector3 topRight = new Vector3(rect.xMax, rect.yMax, 0f);
            Vector3 bottomRight = new Vector3(rect.xMax, rect.yMin, 0f);

            line.SetPosition(0, bottomLeft);
            line.SetPosition(1, topLeft);
            line.SetPosition(2, topRight);
            line.SetPosition(3, bottomRight);
        }

        private void FindSimulationIfNeeded()
        {
            if (simulation != null)
            {
                return;
            }

            simulation = FindFirstObjectByType<CyberDefenseSimulation>();
        }

        private void BuildLineMaterial()
        {
            if (lineMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }
}
