using UnityEngine;

namespace CyberDefense.Entities
{
    [CreateAssetMenu(menuName = "Cyber Defense/Entity Config")]
    public sealed class CyberEntityConfig : ScriptableObject
    {
        public float maxHealth = 100f;
        public float maxEnergy = 100f;
        public float speed = 3f;
        public float perceptionRadius = 8f;
        public float interactionRadius = 0.7f;
        public Color color = Color.white;
    }
}
