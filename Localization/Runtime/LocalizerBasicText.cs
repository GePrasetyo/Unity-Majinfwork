using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Majinfwork.Localization {
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizerBasicText : MonoBehaviour {
        [SerializeField] public TMP_Text target;
        [SerializeField] public LocalizedString text;
        [SerializeField] public LocalizedTmpFont font;

        private void OnValidate() {
            target = GetComponent<TMP_Text>();
            Assert.IsNotNull(target);
        }

        private void OnEnable() {
            text.StringChanged += TextChangedHandler;
            font.AssetChanged += FontChangedHandler;
            ForceUpdateText();
            ForceUpdateFont();
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

        public void SetTarget(TMP_Text target) {
            this.target = target;
        }

        public void SetText(LocalizedString text, bool forceUpdate = true) {
            if (enabled) {
                this.text.StringChanged -= TextChangedHandler;
                this.text = text;
                if (forceUpdate) {
                    ForceUpdateText();
                }
                this.text.StringChanged += TextChangedHandler;
            }
            else {
                this.text = text;
            }
        }

        public async void ForceUpdateText() {
            if (text.TableEntryReference.ReferenceType != TableEntryReference.Type.Empty) {
                target.text = await text.GetLocalizedStringAsync().Task;
            }
        }

        public async void ForceUpdateFont() {
            if (font.TableEntryReference.ReferenceType != TableEntryReference.Type.Empty) {
                target.font = await font.LoadAssetAsync().Task;
            }
        }
    }
}