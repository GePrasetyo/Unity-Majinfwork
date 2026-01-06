using System;
using UnityEngine;

namespace Majinfwork.World {
    [Serializable]
    [CreateAssetMenu(fileName = "Default Game Mode Config", menuName = "MFramework/Config Object/Game Mode Config")]
    public class GameModeManager : ScriptableObject {
        [SerializeField] private GameState gameState;
        [SerializeField] private HUDManager hudManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerState playerState;
        [SerializeField] private PlayerPawn playerPawn;
        [SerializeField] private PlayerInput playerInput;
        [SerializeReference, ClassReference] private CameraHandler cameraHandler;

        internal static PlayerDependency playerReference;

        internal void OnActive() {
            InitiateGameManager();
            InstantiatePlayer();
        }

        internal void OnDeactive() {
            cameraHandler.Deconstruct();
            playerReference = null;
        }

        internal void InitiateGameManager() {
            Instantiate(gameState);
            Instantiate(hudManager);
        }

        internal void InstantiatePlayer() {
            cameraHandler.Construct();
            playerReference = PlayerDependencyFactory.Create(playerState, playerPawn, playerInput);
        }

        internal abstract class PlayerDependency {
            public abstract PlayerState GetState();
            public abstract PlayerPawn GetPawn();
            public abstract PlayerInput GetInput();
        }

        internal class PlayerConstructorDependency<S, P, I> : PlayerDependency where S : PlayerState where P : PlayerPawn where I : PlayerInput {
            public S state;
            public P pawn;
            public I input;

            public override PlayerState GetState() => state;
            public override PlayerPawn GetPawn() => pawn;
            public override PlayerInput GetInput() => input;


            public PlayerConstructorDependency(S _state, P _pawn, I _input) {
                state = Instantiate(_state);

                var playerStart = FindFirstObjectByType<PlayerStart>();
                Vector3 spawnPost = playerStart == null ? Vector3.zero : playerStart.transform.position;
                Quaternion quaternion = playerStart == null ? Quaternion.identity : playerStart.transform.rotation;
                pawn = Instantiate(_pawn, spawnPost, quaternion);
                input = Instantiate(_input);
            }
        }

        internal static class PlayerDependencyFactory {
            public static PlayerDependency Create(PlayerState stateInstance, PlayerPawn pawnInstance, PlayerInput inputInstance) {
                Type stateType = stateInstance.GetType();
                Type pawnType = pawnInstance.GetType(); 
                Type inputType = inputInstance.GetType();

                var constructor = typeof(PlayerConstructorDependency<,,>)
                    .MakeGenericType(stateType, pawnType, inputType)
                    .GetConstructor(new[] { stateType, pawnType, inputType });

                return (PlayerDependency)constructor.Invoke(new object[] { stateInstance, pawnInstance, inputInstance });
            }
        }
    }
}