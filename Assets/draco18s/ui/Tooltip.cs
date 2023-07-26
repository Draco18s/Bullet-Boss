using UnityEngine;
using UnityEngine.UI;

namespace Assets.draco18s.ui {
	public sealed class Tooltip : MonoBehaviour {
		public static Tooltip instance {
			get {
				/*if(_instance == null) {
					GameObject go = new GameObject();
					go.name = "TooltipInstance";
					_instance = go.AddComponent<Tooltip>();
				}*/
				return _instance;
			}
		}
		private static Tooltip _instance;

		void Awake() {
			_instance = this;
			transform.SetSiblingIndex(transform.parent.childCount - 1);
		}

		public static void HideTooltip() {
			instance.gameObject.SetActive(false);
		}

		public static void ShowTooltip(Vector3 p, string v) {
			ShowTooltip(p, v, 1);
		}
		public static void ShowTooltip(Vector3 pos, string v, float ratio) {
			ShowTooltip(pos, v, ratio, 1);
		}
		public static void ShowTooltip(Vector3 pos, string v, float ratio, float scale) {
			ShowTooltip(pos, v, ratio, scale, true);
		}

		private static void ShowTooltip(Vector3 pos, string v, float ratio, float scale, bool allowMoveDown) {
			if(v.Length == 0) return;
			//if (GuiManager.instance.selectionImg.GetComponent<Image>().enabled) return;
			instance.gameObject.SetActive(true);
			((RectTransform)instance.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160);
			((RectTransform)instance.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);
			instance.transform.position = pos;
			Text textArea = instance.transform.Find("Text").GetComponent<Text>();
			textArea.text = v;
			//width + 7.5
			//height + 6
			bool fits = false;
			if(textArea.preferredWidth < 610) {
				((RectTransform)textArea.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textArea.preferredWidth);
				((RectTransform)textArea.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textArea.preferredHeight);
				((RectTransform)instance.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (textArea.preferredWidth / 2) + 22);
				((RectTransform)instance.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (textArea.preferredHeight / 2) + 16);
				fits = true;
			}
			/*if(t.preferredHeight < 232) {
				((RectTransform)instance.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (t.preferredHeight / 4) + 7.5f);
				fits = true;
			}*/
			float w = 64;// t.preferredWidth;
			if(!fits) {
				float ph = 4;
				float h = 68;
				float r = 1;
				do {
					r = (h * ratio) / w;
					w += 32 * Mathf.CeilToInt(r);
					((RectTransform)textArea.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
					ph = h;
					h = textArea.preferredHeight;
				} while(h * ratio > w && h != ph);
				if(h == ph) {
					w = textArea.preferredWidth + 22;
					//w -= 32 * Mathf.CeilToInt(r);
					((RectTransform)textArea.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
				}

				((RectTransform)textArea.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
				((RectTransform)textArea.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
				h = textArea.preferredHeight;
				((RectTransform)instance.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (w / 2) + 8);
				((RectTransform)instance.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (h / 2) + 15f);
			}
			float wid = ((RectTransform)instance.transform).rect.width;
			float hig = ((RectTransform)instance.transform).rect.height;
			if(instance.transform.position.x + wid * scale > Screen.width) {
				if(allowMoveDown) {
					//shift the tooltip down. No check for off-screen
					if(instance.transform.position.y - hig * 1.5f < 35) {
						instance.transform.position = new Vector3(Screen.width - 5 - wid, instance.transform.position.y + ((RectTransform)instance.transform).rect.height, 0);
					}
					else {
						instance.transform.position = new Vector3(Screen.width - 5 - wid, instance.transform.position.y - ((RectTransform)instance.transform).rect.height, 0);
					}
				}
				else {
					//instance.transform.position += new Vector3(wid * scale / 2, 0, 0);
					instance.transform.position = new Vector3(Screen.width - 5, instance.transform.position.y, 0);
				}
			}
			else {
				instance.transform.position += new Vector3(0, 0, 0);
			}
			instance.transform.localScale = new Vector3(scale, scale, scale);
		}
	}
}