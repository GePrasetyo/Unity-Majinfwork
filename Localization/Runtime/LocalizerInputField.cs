using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Localization;

namespace Majinfwork.Localization {
    [RequireComponent(typeof(TMP_InputField))]
    public class LocalizerInputField : MonoBehaviour {
        [SerializeField] public TMP_InputField target;
        [SerializeField] public LocalizedString placeholder;
        [SerializeField] public LocalizedTmpFont font;

        private void OnValidate() {
            target = GetComponent<TMP_InputField>();
            Assert.IsNotNull(target);
        }
        private void OnEnable() {
            placeholder.StringChanged += PlaceholderChangedHandler;
            font.AssetChanged += FontChangedHandler;
        }

        private void FontChangedHandler(TMP_FontAsset value) {
            target.fontAsset = value;
        }

        private void PlaceholderChangedHandler(string value) {
            ((TMP_Text)target.placeholder).text = value;
        }

        private void OnDisable() {
            placeholder.StringChanged -= PlaceholderChangedHandler;
            font.AssetChanged -= FontChangedHandler;
        }
    }
}