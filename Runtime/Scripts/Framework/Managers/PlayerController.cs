using UnityEngine;

namespace Majingari.Framework.World {
    public abstract class PlayerController : MonoBehaviour {
        protected static bool GetPawn<P>(out P pawn) where P : PlayerPawn {
            pawn = GameModeManager.playerReference.GetPawn() as P;
            return pawn != null;
        }

        protected static bool GetState<S>(out S state) where S : PlayerState {
            state = GameModeManager.playerReference.GetState() as S;
            return state != null;
        }
    }
}