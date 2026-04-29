using System.Collections.Generic;
using UnityEngine;

namespace CyberDefense.Entities
{
    public sealed class DataSentinel : NetworkEntity
    {
        private enum SentinelState
        {
            Gather,
            ReturnToHub,
            RepairNode,
            Flee,
            Hide,
            Recharge
        }

        private readonly List<NetworkEntity> nearby = new List<NetworkEntity>();
        private SentinelState state;
        private DataPacket carriedPacket;
        private DataPacket targetPacket;
        private CorruptedNode repairTarget;
        private Firewall hidingSpot;

        public bool HasPayload => carriedPacket != null;

        private void Update()
        {
            if (Simulation == null)
            {
                return;
            }

            DrainEnergy(Time.deltaTime * (Simulation.SystemOverload ? 2.6f : 1f));
            SenseAndChooseState();
            Act();
            ApplyMovement();
        }

        private void SenseAndChooseState()
        {
            nearby.Clear();
            Simulation.Query(Position2D, GetPerceptionRadius(), nearby);

            NetworkEntity closestMalware = FindClosest(EntityKind.Malware);
            if (closestMalware != null && Health < 65f)
            {
                hidingSpot = FindClosest(EntityKind.Firewall) as Firewall;
                state = hidingSpot != null ? SentinelState.Hide : SentinelState.Flee;
                return;
            }

            if (Energy <= 18f)
            {
                state = SentinelState.Recharge;
                return;
            }

            repairTarget = FindClosestCorruptedNode();
            if (repairTarget != null && (Simulation.SystemOverload || repairTarget.Corruption > 40f))
            {
                state = SentinelState.RepairNode;
                return;
            }

            state = carriedPacket != null ? SentinelState.ReturnToHub : SentinelState.Gather;
        }

        private void Act()
        {
            switch (state)
            {
                case SentinelState.Gather:
                    GatherData();
                    break;
                case SentinelState.ReturnToHub:
                    ReturnToHub();
                    break;
                case SentinelState.RepairNode:
                    RepairNode();
                    break;
                case SentinelState.Flee:
                    FleeFromMalware();
                    break;
                case SentinelState.Hide:
                    HideBehindFirewall();
                    break;
                case SentinelState.Recharge:
                    RechargeAtHub();
                    break;
            }
        }

        private void GatherData()
        {
            if (targetPacket == null || !targetPacket.isActiveAndEnabled || (targetPacket.IsReserved && !targetPacket.IsReservedBy(this)))
            {
                targetPacket = FindClosestFreePacket();
                if (targetPacket != null)
                {
                    targetPacket.TryReserve(this);
                }
            }

            if (targetPacket == null)
            {
                Wander(0.7f);
                return;
            }

            MoveToward(targetPacket.Position2D);
            if (Vector2.Distance(Position2D, targetPacket.Position2D) <= GetInteractionRadius())
            {
                carriedPacket = targetPacket;
                Simulation.Unregister(carriedPacket);
                carriedPacket.gameObject.SetActive(false);
                targetPacket = null;
            }
        }

        private void ReturnToHub()
        {
            CentralHub hub = Simulation.Hub;
            if (hub == null)
            {
                Wander();
                return;
            }

            MoveToward(hub.Position2D);
            if (Vector2.Distance(Position2D, hub.Position2D) <= GetInteractionRadius() + 0.5f)
            {
                hub.DepositData(10f);
                RestoreEnergy(18f);
                ShowInteractionBeam(hub, new Color(1f, 0.82f, 0.18f, 0.95f));
                Destroy(carriedPacket.gameObject);
                carriedPacket = null;
            }
        }

        private void RepairNode()
        {
            if (repairTarget == null)
            {
                Wander();
                return;
            }

            MoveToward(repairTarget.Position2D, 0.85f);
            if (Vector2.Distance(Position2D, repairTarget.Position2D) <= GetInteractionRadius() + 0.6f)
            {
                int helpers = Mathf.Max(1, Simulation.CountSentinelsNear(repairTarget.Position2D, 1.8f));
                float cooperativeBoost = 1f + (helpers - 1) * 0.7f;
                repairTarget.Repair(Time.deltaTime * 16f * cooperativeBoost);
                PulseRepairVisual(Mathf.Min(3f, helpers));
                ShowInteractionBeam(repairTarget, new Color(0.45f, 1f, 0.75f, 0.85f));
                RestoreEnergy(Time.deltaTime * 2f);
                Velocity *= 0.2f;
            }
        }

        private void FleeFromMalware()
        {
            NetworkEntity malware = FindClosest(EntityKind.Malware);
            if (malware == null)
            {
                Wander();
                return;
            }

            MoveAwayFrom(malware.Position2D, 1.25f);
        }

        private void HideBehindFirewall()
        {
            if (hidingSpot == null)
            {
                FleeFromMalware();
                return;
            }

            Vector2 threatDirection = Vector2.zero;
            NetworkEntity malware = FindClosest(EntityKind.Malware);
            if (malware != null)
            {
                threatDirection = (hidingSpot.Position2D - malware.Position2D).normalized;
            }

            Vector2 hidePosition = hidingSpot.Position2D + threatDirection * 0.9f;
            MoveToward(hidePosition, 1.15f);
            if (Vector2.Distance(Position2D, hidePosition) <= 0.35f)
            {
                Health = Mathf.Min(Config != null ? Config.maxHealth : 100f, Health + Time.deltaTime * 8f);
                Velocity *= 0.1f;
            }
        }

        private void RechargeAtHub()
        {
            CentralHub hub = Simulation.Hub;
            if (hub == null)
            {
                Wander();
                return;
            }

            MoveToward(hub.Position2D, 0.9f);
            if (Vector2.Distance(Position2D, hub.Position2D) <= GetInteractionRadius() + 0.8f)
            {
                RestoreEnergy(Time.deltaTime * 26f);
                Velocity *= 0.1f;
            }
        }

        private DataPacket FindClosestFreePacket()
        {
            DataPacket closest = null;
            float closestSqr = float.MaxValue;
            for (int i = 0; i < nearby.Count; i++)
            {
                DataPacket packet = nearby[i] as DataPacket;
                if (packet == null || packet.IsReserved)
                {
                    continue;
                }

                float sqr = (packet.Position2D - Position2D).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = packet;
                }
            }

            return closest;
        }

        private NetworkEntity FindClosest(EntityKind kind)
        {
            NetworkEntity closest = null;
            float closestSqr = float.MaxValue;
            for (int i = 0; i < nearby.Count; i++)
            {
                NetworkEntity entity = nearby[i];
                if (entity == null || entity == this || entity.Kind != kind)
                {
                    continue;
                }

                float sqr = (entity.Position2D - Position2D).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = entity;
                }
            }

            return closest;
        }

        private CorruptedNode FindClosestCorruptedNode()
        {
            CorruptedNode closest = null;
            float closestSqr = float.MaxValue;
            for (int i = 0; i < nearby.Count; i++)
            {
                CorruptedNode node = nearby[i] as CorruptedNode;
                if (node == null || node.IsRepaired)
                {
                    continue;
                }

                float sqr = (node.Position2D - Position2D).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = node;
                }
            }

            return closest;
        }
    }
}
