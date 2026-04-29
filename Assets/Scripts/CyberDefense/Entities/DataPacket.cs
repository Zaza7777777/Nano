namespace CyberDefense.Entities
{
    public sealed class DataPacket : NetworkEntity
    {
        private NetworkEntity reservedBy;

        public bool IsReserved => reservedBy != null;

        public bool TryReserve(NetworkEntity reserver)
        {
            if (IsReserved)
            {
                return false;
            }

            reservedBy = reserver;
            return true;
        }

        public bool IsReservedBy(NetworkEntity reserver)
        {
            return reservedBy == reserver;
        }
    }
}
