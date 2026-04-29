using System.Collections.Generic;
using UnityEngine;

namespace CyberDefense.Entities
{
    public sealed class RepairDrone : NetworkEntity
    {
        private readonly List<NetworkEntity> nearby = new List<NetworkEntity>();

        private void Update()
        {
            if (Simulation == null)
            {
                return;
            }

            nearby.Clear();
            Simulation.Query(Position2D, GetPerceptionRadius(), nearby);
            FlockAndPatrol();
            ApplyMovement();
        }

        private void FlockAndPatrol()
        {
            Vector2 separation = Vector2.zero;
            Vector2 alignment = Vector2.zero;
            Vector2 cohesion = Vector2.zero;
            Vector2 malwarePressure = Vector2.zero;
            CorruptedNode repairTarget = null;
            int droneCount = 0;
            int malwareCount = 0;

            for (int i = 0; i < nearby.Count; i++)
            {
                NetworkEntity entity = nearby[i];
                if (entity == null || entity == this)
                {
                    continue;
                }

                Vector2 offset = Position2D - entity.Position2D;
                if (entity.Kind == EntityKind.RepairDrone)
                {
                    float distance = Mathf.Max(0.2f, offset.magnitude);
                    separation += offset.normalized / distance;
                    alignment += entity.GetComponent<RepairDrone>().Velocity;
                    cohesion += entity.Position2D;
                    droneCount++;
                }
                else if (entity.Kind == EntityKind.Malware)
                {
                    malwarePressure += entity.Position2D;
                    malwareCount++;
                    if (distanceTo(entity) < 1.4f)
                    {
                        entity.ReceiveDamage(Time.deltaTime * 10f);
                    }
                }
                else if (entity.Kind == EntityKind.CorruptedNode && repairTarget == null)
                {
                    CorruptedNode node = entity as CorruptedNode;
                    if (node != null && !node.IsRepaired)
                    {
                        repairTarget = node;
                    }
                }
            }

            Vector2 desired = Vector2.zero;
            if (droneCount > 0)
            {
                alignment /= droneCount;
                cohesion = (cohesion / droneCount - Position2D).normalized;
                desired += separation * 1.6f + alignment.normalized * 0.7f + cohesion * 0.8f;
            }

            if (malwareCount > 0)
            {
                desired += ((malwarePressure / malwareCount) - Position2D).normalized * 2.1f;
            }
            else if (repairTarget != null)
            {
                desired += (repairTarget.Position2D - Position2D).normalized * 1.5f;
            }
            else
            {
                desired += (Simulation.GetPatrolPoint(Time.time * 0.35f) - Position2D).normalized;
            }

            Velocity = Vector2.Lerp(Velocity, desired.normalized * GetSpeed(), Time.deltaTime * 2.8f);

            if (repairTarget != null && distanceTo(repairTarget) <= 1.5f)
            {
                int collaborators = Mathf.Max(1, Simulation.CountRepairDronesNear(repairTarget.Position2D, 2f));
                repairTarget.Repair(Time.deltaTime * 8f * collaborators);
                PulseRepairVisual(collaborators);
                ShowInteractionBeam(repairTarget, new Color(0.42f, 0.96f, 1f, 0.95f));
                Velocity *= 0.35f;
            }
        }

        private float distanceTo(NetworkEntity entity)
        {
            return Vector2.Distance(Position2D, entity.Position2D);
        }
    }
}
