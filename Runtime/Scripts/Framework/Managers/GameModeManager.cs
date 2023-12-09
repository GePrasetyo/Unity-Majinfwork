using System;
using UnityEngine;

namespace Majingari.Framework.World {
    [Serializable]
    [CreateAssetMenu(fileName = "Default Game Mode Config", menuName = "Config Object/Game Mode Config")]
    public class GameModeManager : ScriptableObject {
        [SerializeField] private GameState _gameState;
        [SerializeField] private HUDManager _hudManager;
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerState _playerState;
        [SerializeField] private PlayerPawn _playerPawn;
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
            Instantiate(_gameState);
            Instantiate(_hudManager);
            Instantiate(_inputManager);
        }

        internal void InstantiatePlayer() {
            cameraHandler.Construct();
            playerReference = PlayerDependencyFactory.Create(_playerState, _playerPawn);
            Instantiate(_playerController);
        }

        internal abstract class PlayerDependency {
            public abstract PlayerState GetState();
            public abstract PlayerPawn GetPawn();
        }

        internal class PlayerConstructorDependency<S, P> : PlayerDependency where S : PlayerState where P : PlayerPawn {
            public S state;
            public P pawn;

            public override PlayerState GetState() => state;
            public override PlayerPawn GetPawn() => pawn;

            public PlayerConstructorDependency(S _state, P _pawn) {
                state = Instantiate(_state);

                var playerStart = FindObjectOfType<PlayerStart>();
                Vector3 spawnPost = playerStart == null ? Vector3.zero : playerStart.transform.position;
                Quaternion quaternion = playerStart == null ? Quaternion.identity : playerStart.transform.rotation;
                pawn = Instantiate(_pawn, spawnPost, quaternion);
            }
        }

        internal static class PlayerDependencyFactory {
            public static PlayerDependency Create(PlayerState stateInstance, PlayerPawn pawnInstance) {
                Type stateType = stateInstance.GetType();
                Type pawnType = pawnInstance.GetType(); 

                var constructor = typeof(PlayerConstructorDependency<,>)
                    .MakeGenericType(stateType, pawnType)
                    .GetConstructor(new[] { stateType, pawnType });

                return (PlayerDependency)constructor.Invoke(new object[] { stateInstance, pawnInstance });
            }
        }
    }
}