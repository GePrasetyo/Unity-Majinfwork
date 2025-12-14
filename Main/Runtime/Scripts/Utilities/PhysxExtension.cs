using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Majinfwork {
    public static class PhysxExtension {
        public static Collider[] colliderHitAllocation = new Collider[128];
        public static RaycastHit[] hitAllocation = new RaycastHit[256];
        public delegate bool DestructableObstruction(out bool isDestroyable);

        /// <summary>
        /// Physics Overlap Sphere
        /// </summary>
        public static void OverlapSphere(ref List<Collider> colliders, Vector3 origin, float radius, int layerMask) {
            var foundCount = Physics.OverlapSphereNonAlloc(origin, radius, colliderHitAllocation, layerMask);
            foundCount = Mathf.Min(foundCount, colliderHitAllocation.Length);

            for (int i = 0; i < foundCount; i++) {
                colliders.Add(colliderHitAllocation[i]);
            }
        }

        public static void OverlapBox(ref List<Collider> colliders, Vector3 origin, Vector3 halfExtent, Quaternion orientation, int layerMask) {
            var foundCount = Physics.OverlapBoxNonAlloc(origin, halfExtent, colliderHitAllocation, orientation, layerMask);
            foundCount = Mathf.Min(foundCount, colliderHitAllocation.Length);

            for (int i = 0; i < foundCount; i++) {
                colliders.Add(colliderHitAllocation[i]);
            }
        }

        public static bool IsObstructed(Vector3 origin, Vector3 target, LayerMask layerObstruction, float threshold = 0.2f) {
            var vector = target - origin;
            var distanceToTarget = vector.magnitude;

            if (Physics.RaycastNonAlloc(origin, vector / distanceToTarget, hitAllocation, distanceToTarget + 0.5f, layerObstruction, QueryTriggerInteraction.Ignore) > 0) {
                Assert.IsTrue(hitAllocation[0].point != null, "Somehow hit point from Raycast Allocation is null");
                var distanceToHit = (origin - hitAllocation[0].point).magnitude;

                if (distanceToHit > distanceToTarget) {
                    return false;
                }

                return distanceToTarget - distanceToHit >= threshold;
            }
            else {
                return false;
            }
        }

        public static bool IsObstructed(Vector3 origin, Vector3 target, LayerMask layerObstruction, LayerMask destructabkeLayer, float threshold = 0.2f) {
            var vector = target - origin;
            var distanceToTarget = vector.magnitude;

            if (Physics.RaycastNonAlloc(origin, vector / distanceToTarget, hitAllocation, distanceToTarget + 0.5f, layerObstruction, QueryTriggerInteraction.Ignore) > 0) {
                Assert.IsTrue(hitAllocation[0].point != null, "Somehow hit point from Raycast Allocation is null");
                var distanceToHit = (origin - hitAllocation[0].point).magnitude;

                if (distanceToHit > distanceToTarget) {
                    return false;
                }

                if ((destructabkeLayer.value & 1 << hitAllocation[0].collider.gameObject.layer) != 0) {
                    if (hitAllocation[0].collider.TryGetComponent(out IDestroyable obj)) {
                        return !obj.IsDestroyable;
                    }
                }

                return distanceToTarget - distanceToHit >= threshold;
            }
            else {
                return false;
            }
        }
    }

    public interface IDestroyable {
        public bool IsDestroyable { get; }
    }
}