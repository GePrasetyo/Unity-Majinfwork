using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Majinfwork.World {
    internal class PSORecordingOverlay : MonoBehaviour {
        private bool visible;
        private bool recording;
        private GraphicsStateCollection recordingCollection;
        private int variantCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate() {
            if (!Debug.isDebugBuild) return;

            var go = new GameObject("PSORecordingOverlay");
            go.AddComponent<PSORecordingOverlay>();
            DontDestroyOnLoad(go);
        }

        private void Update() {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard[UnityEngine.InputSystem.Key.F11].wasPressedThisFrame)
                visible = !visible;
#else
            if (Input.GetKeyDown(KeyCode.F11))
                visible = !visible;
#endif
        }

        private void OnGUI() {
            if (!visible) return;

            float w = 220f, h = 90f;
            var rect = new Rect(Screen.width - w - 10, 10, w, h);
            GUI.Box(rect, "PSO Recorder");

            var inner = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, 25);

            if (!recording) {
                if (GUI.Button(inner, "Start Recording")) {
                    recordingCollection = new GraphicsStateCollection();
                    recordingCollection.BeginTrace();
                    recording = true;
                }
            }
            else {
                GUI.color = Color.red;
                GUI.Label(inner, "\u25CF REC");
                GUI.color = Color.white;

                inner.y += 25;
                GUI.Label(inner, $"Variants: {recordingCollection.variantCount}");

                inner.y += 25;
                if (GUI.Button(new Rect(inner.x, inner.y, inner.width, 22), "Stop & Save")) {
                    StopAndSave();
                }
            }
        }

        private void StopAndSave() {
            if (!recording || recordingCollection == null) return;

            recordingCollection.EndTrace();
            recording = false;

            var dir = System.IO.Path.Combine(Application.persistentDataPath, "RecordedPSO");
            System.IO.Directory.CreateDirectory(dir);
            var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var path = System.IO.Path.Combine(dir, $"pso_{timestamp}.shaderstates");

            recordingCollection.SaveToFile(path);
            Debug.Log($"[PSORecordingOverlay] Saved PSO collection ({recordingCollection.variantCount} variants) to: {path}");
            recordingCollection = null;
        }

        private void OnDestroy() {
            if (recording && recordingCollection != null) {
                recordingCollection.EndTrace();
                recording = false;
            }
        }
    }
}
