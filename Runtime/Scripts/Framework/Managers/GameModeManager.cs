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

        public void InitiateGameManager() {
            Instantiate(_gameState);
            Instantiate(_hudManager);
            Instantiate(_inputManager);
        }

        public void InstantiatePlayer() {
            var _pc = Instantiate(_playerController);
            _pc.Construct(_playerState, _playerPawn);
        }
    }

}