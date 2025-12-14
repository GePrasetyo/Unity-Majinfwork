using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Localization;

namespace Majinfwork.Localization {
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizerValueText : MonoBehaviour {
        [SerializeField] public TMP_Text target;
        [SerializeField] public LocalizedString text;
        [SerializeField] public LocalizedTmpFont font;

        private Dictionary<string, string> valueByKey = new();
        private object[] arguments;

        private void OnValidate() {
            target = GetComponent<TMP_Text>();
            Assert.IsNotNull(target);
        }

        private void OnEnable() {
            text.StringChanged += TextChangedHandler;
            font.AssetChanged += FontChangedHandler;
        }

        private void FontChangedHandler(TMP_FontAsset value) {
            target.font = value;
        }

        private void TextChangedHandler(string value) {
            target.text = value;
        }

        private void OnDisable() {
            text.StringChanged -= TextChangedHandler;
            font.AssetChanged -= FontChangedHandler;
        }

        public void SetArgument(string key, string value) {
            if (arguments == null) {
                arguments = new object[] { valueByKey };
                text.Arguments = arguments;
            }
            valueByKey[key] = value;
            text.RefreshString();
        }
    }
}