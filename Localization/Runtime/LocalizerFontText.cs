using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Localization;

namespace Majinfwork.Localization {
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizerFontText : MonoBehaviour {
        [SerializeField] public TMP_Text target;
        [SerializeField] public LocalizedTmpFont font;

        private void OnValidate() {
            target = GetComponent<TMP_Text>();
            Assert.IsNotNull(target);
        }
        private void OnEnable() {
            font.AssetChanged += FontChangedHandler;
        }

        private void FontChangedHandler(TMP_FontAsset value) {
            target.font = value;
        }

        private void OnDisable() {
            font.AssetChanged -= FontChangedHandler;
        }
    }
}