using UnityEngine;

namespace CyberDefense.Entities
{
    public sealed class CentralHub : NetworkEntity
    {
        [SerializeField] private float storedData;

        public float StoredData => storedData;

        public void DepositData(float amount)
        {
            storedData += amount;
            RestoreEnergy(amount * 0.25f);
        }

        public override void ReceiveDamage(float amount)
        {
            base.ReceiveDamage(amount * 0.35f);
        }
    }
}
