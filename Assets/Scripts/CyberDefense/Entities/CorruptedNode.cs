using UnityEngine;

namespace CyberDefense.Entities
{
    public sealed class CorruptedNode : NetworkEntity
    {
        [SerializeField] private float corruption = 100f;
        private bool rewardSpawned;

        public float Corruption => corruption;
        public bool IsRepaired => corruption <= 0f;

        private void Start()
        {
            Integrity = 100f - corruption;
        }

        public void Repair(float amount)
        {
            corruption = Mathf.Max(0f, corruption - amount);
            Integrity = 100f - corruption;
            PulseRepairVisual(Mathf.Lerp(3f, 1f, Integrity / 100f));
            if (IsRepaired && !rewardSpawned)
            {
                rewardSpawned = true;
                Simulation?.SpawnDataPacket(Position2D + Random.insideUnitCircle * 1.2f);
            }
        }
    }
}
