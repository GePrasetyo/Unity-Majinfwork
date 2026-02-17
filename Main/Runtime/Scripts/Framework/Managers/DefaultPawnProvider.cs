using System;
using UnityEngine;

namespace Majinfwork.World {
    [Serializable]
    public class DefaultPawnProvider : PawnProvider {
        [SerializeField] private PlayerPawn pawnPrefab;

        public override PlayerPawn GetPawn(PlayerState state, Vector3 position, Quaternion rotation) {
            return UnityEngine.Object.Instantiate(pawnPrefab, position, rotation);
        }

        public override void ReleasePawn(PlayerPawn pawn) {
            if (pawn != null)
                UnityEngine.Object.Destroy(pawn.gameObject);
        }
    }
}
