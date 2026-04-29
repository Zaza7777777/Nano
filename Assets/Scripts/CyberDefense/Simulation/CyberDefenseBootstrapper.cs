using UnityEngine;

namespace CyberDefense.Simulation
{
    public static class CyberDefenseBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSimulationExists()
        {
            if (Object.FindFirstObjectByType<CyberDefenseSimulation>() != null)
            {
                return;
            }

            GameObject simulation = new GameObject("Cyber-Security Network Defense Simulation");
            simulation.AddComponent<CyberDefenseSimulation>();
            simulation.AddComponent<QuadTreeDebugToggle>();

            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 12.5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.03f, 0.035f, 0.05f);
        }
    }
}
