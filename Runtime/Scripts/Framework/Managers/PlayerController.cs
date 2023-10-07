using UnityEngine;

namespace Majingari.Framework.World {
    public class PlayerController : MonoBehaviour {
        protected PlayerPawn playerPawn;
        protected PlayerState playerState;

        public void Construct(PlayerState _state, PlayerPawn _pawn) {
            playerState = Instantiate(_state);
            playerPawn = Instantiate(_pawn);
        }
    }
}