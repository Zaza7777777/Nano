using UnityEngine;

namespace CyberDefense.Simulation
{
    public sealed class QuadTreeDebugToggle : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.Q;
        [SerializeField] private CyberDefenseSimulation simulation;

        private void Awake()
        {
            if (simulation == null)
            {
                simulation = FindFirstObjectByType<CyberDefenseSimulation>();
            }
        }

        private void Update()
        {
            if (simulation != null && Input.GetKeyDown(toggleKey))
            {
                simulation.ToggleQuadTreeDraw();
            }
        }
    }
}
