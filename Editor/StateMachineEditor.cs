using UnityEditor;

namespace Majingari.Framework.World {
    public class StateMachineEditor : Editor {

        [MenuItem("Majingari Framework/Level State Machine")]
        public static void CreateLevelStateMachine() {
            string resourcesPath = "Assets/Resources";

            if (!AssetDatabase.IsValidFolder(resourcesPath)) {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"{resourcesPath}/LevelStateMachine.controller");
        }

        [MenuItem("Majingari Framework/Game State Machine")]
        public static void GameStateMachine() {
            string resourcesPath = "Assets/Resources";

            if (!AssetDatabase.IsValidFolder(resourcesPath)) {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"{resourcesPath}/GameStateMachine.controller");
        }
    }
}