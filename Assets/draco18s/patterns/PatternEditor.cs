﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MathS = System.MathF; //fuck off System.MathF

namespace Assets.draco18s
{
	public class PatternEditor : MonoBehaviour
	{
		public static PatternEditor instance;
		public GameObject target;
		public GameObject gunThings;
		public GameObject bulletThings;
		public TMP_Dropdown reloadDropdown;
		public TMP_Dropdown attributeDropdown;
		public TMP_Dropdown loopDropdown;
		public Slider capField;
		public Slider reloadField;
		public Slider angleField;
		public Slider lifetimeField;
		public Button subButton;
		public Button supButton;
		public Slider slider;
		public Scrollbar keyFrameSlider;
		public RectTransform timeHandle;
		public RectTransform secondsVis;
		public RectTransform lineRectTransform;
		public UIMeshLine line;
		public int currentKeyframe = 0;

		public GameObject keyframePrefab;
		private IHasPattern targetPatternObj;
		private PatternData targetPattern;
		private Timeline targetTimelineData;
		private bool changed = true;
		private Canvas canvas;

		private bool isEditingGun = true;

		readonly string[] loopNames = {
			WrapMode.ClampForever.ToString(), WrapMode.Loop.ToString(), WrapMode.PingPong.ToString(), WrapMode.Clamp.ToString()
		};

		[UsedImplicitly]
		void Start()
		{
			instance = this;
			lineRectTransform = (RectTransform)line.transform;
			canvas = GetComponent<Canvas>();
			reloadDropdown.AddOptions(new List<string>(){ "None","SingleShot","Overheat","ClipSize","Shotgun","Spread" });
			reloadDropdown.onValueChanged.AddListener(UpdateGunType);
			loopDropdown.AddOptions(loopNames.ToList());
			loopDropdown.onValueChanged.AddListener(UpdateLoopType);
			/*ChangeTarget(target);*/
			slider.onValueChanged.AddListener(UpdateValue);
			keyFrameSlider.onValueChanged.AddListener(UpdateKeyframe);
			attributeDropdown.onValueChanged.AddListener(UpdateAttribute);
			//capField.onValueChanged.AddListener(UpdateCapacity);
			reloadField.interactable = false;
			lifetimeField.onValueChanged.AddListener(UpdateLife);
			angleField.onValueChanged.AddListener(UpdateStartAngle);
			line.ClearPoints();
			canvas = GetComponent<Canvas>();
			canvas.enabled = false;
		}

		public void AccessSubsystem()
		{
			PatternData hasPattern = targetPattern.childPattern;
			if (hasPattern == null) return;

			subButton.interactable = (hasPattern.childPattern != null);
			supButton.interactable = true;
			isEditingGun = !isEditingGun;
			ChangeTarget(hasPattern);
		}

		public void AccessRootSystem()
		{
			ChangeTarget(target);
		}

		public void ChangeTarget(GameObject gg)
		{
			if (gg == null)
			{
				target = null;
				targetPattern = null;
				targetPatternObj = null;
				canvas.enabled = false;
				return;
			}
			IHasPattern hasTime = gg.GetComponent<IHasPattern>();
			if(hasTime ==  null) return;

			isEditingGun = true;
			target = gg;
			targetPatternObj = hasTime;
			ChangeTarget(hasTime.Pattern);
			supButton.interactable = false;
			canvas.enabled = true;
		}

		public void ChangeTarget(PatternData tl)
		{
			targetPattern = tl;
			targetTimelineData = tl.timeline;
			List<PatternDataKey> list = GetAllowedValues(isEditingGun);
			attributeDropdown.ClearOptions();
			attributeDropdown.AddOptions(list.Select(x => x.ToString()).ToList());
			attributeDropdown.SetValueWithoutNotify(0);
			reloadDropdown.SetValueWithoutNotify((int)tl.ReloadType);
			subButton.interactable = targetPattern.childPattern != null;
			//capField.SetValueWithoutNotify(tl.Pattern.MaxCapacity * 10);
			//bool isGun = tl is GunBarrel && tl.ReloadType != GunType.None;
			//bool isBullet = tl is Bullet;

			lifetimeField.SetValueWithoutNotify(tl.Lifetime);

			angleField.SetValueWithoutNotify(tl.StartAngle / 5f);
			reloadDropdown.interactable = isEditingGun;
			gunThings.SetActive(isEditingGun);
			bulletThings.SetActive(!isEditingGun);
			loopDropdown.interactable = isEditingGun;
			
			string val = attributeDropdown.options[attributeDropdown.value].text;
			Enum.TryParse(val, out PatternDataKey datType);

			foreach (PatternDataKey dat in list)
			{
				if (!targetTimelineData.data.ContainsKey(dat))
				{
					AnimationCurve c = GetNewAnimationCurve(isEditingGun);
					c.AddKey(0, dat == PatternDataKey.Rotation ? 0 : 1);
					targetTimelineData.data.Add(dat, c);
				}
			}
			

			AnimationCurve curv = targetTimelineData.data[datType];
			int w = Array.IndexOf(loopNames, curv.postWrapMode.ToString());
			loopDropdown.SetValueWithoutNotify(w);

			UpdateAttribute(0);

			changed = true;
		}

		private AnimationCurve GetNewAnimationCurve(bool b)
		{
			if (b)
				return new AnimationCurve()
				{
					preWrapMode = WrapMode.Loop,
					postWrapMode = WrapMode.Loop
				};
			else
				return new AnimationCurve()
				{
					preWrapMode = WrapMode.ClampForever,
					postWrapMode = WrapMode.ClampForever
				};
		}

		[UsedImplicitly]
		void Update()
		{
			if (Input.GetButtonDown("Cancel"))
			{
				targetPatternObj = null;
				canvas.enabled = false;
				target = null;
				targetPattern = null;
			}
			if (target == null || targetPatternObj == null) return;

			float t = targetPatternObj.CurrentTime;
			float h1 = lineRectTransform.rect.height;

			timeHandle.anchoredPosition = new Vector2(0, -t * h1 / targetPattern.Lifetime);

			if (!changed)
			{
				return;
			}

			changed = false;

			string val = attributeDropdown.options[attributeDropdown.value].text;
			Enum.TryParse(val, out PatternDataKey datType);
			AnimationCurve curv = targetTimelineData.data[datType];
			
			line.ClearPoints();
			for (int i = 0; i <= 20; i++)
			{
				float f = curv.Evaluate(i / 2f) * scalar;
				line.AddPoint(new LinePoint(GetPoint(i, f)));
			}
		}

		private Vector3 GetPoint(int i, float f)
		{
			Rect rect = lineRectTransform.rect;
			float range = (slider.maxValue - slider.minValue);

			float offset = slider.minValue / range * rect.width;
			float x = (f / range) * rect.width - rect.width/2 - offset;
			
			float y = i * rect.height / 20f;
			return new Vector3(x, -y);
		}

		/*private void UpdateCapacity(float t)
		{
			targetTimeline.Pattern.MaxCapacity = t/10;
			reloadField.SetValueWithoutNotify(reloadField.maxValue - capField.value + 1);
		}*/

		private void UpdateStartAngle(float t)
		{
			targetPattern.StartAngle = t * 5;
		}

		private void UpdateLife(float t)
		{
			targetPattern.Lifetime = t;
			
			secondsVis.anchorMin = new Vector2(secondsVis.anchorMin.x, -1 * ((10 / t) - 1));
			secondsVis.sizeDelta = new Vector2(secondsVis.sizeDelta.x, 0);
		}

		private void UpdateLoopType(int i)
		{
			WrapMode t = (WrapMode)i;
			string val = attributeDropdown.options[attributeDropdown.value].text;
			Enum.TryParse(val, out PatternDataKey datType);
			AnimationCurve curv = targetTimelineData.data[datType];
			Enum.TryParse(loopNames[i], out WrapMode mode);
			curv.postWrapMode = mode;
			curv.preWrapMode = mode;
		}

		private void UpdateGunType(int i)
		{
			GunType t = (GunType)i;
			targetPattern.ReloadType = t;
			switch (t)
			{
				case GunType.None:
					capField.interactable = false;
					reloadField.interactable = false;
					break;
				case GunType.SingleShot:
					capField.SetValueWithoutNotify(1);
					capField.interactable = false;
					reloadField.interactable = true;
					break;
				default:
					capField.interactable = true;
					reloadField.interactable = true;
					break;
			}
		}

		private float scalar = 1;

		private void UpdateAttribute(int i)
		{
			string val = attributeDropdown.options[attributeDropdown.value].text;
			Enum.TryParse(val, out PatternDataKey datType);
			AnimationCurve curv = targetTimelineData.data[datType];
			float v = curv.Evaluate(currentKeyframe);
			Vector3 minMax = GetAllowedRange(datType, isEditingGun);
			scalar = minMax.z;
			slider.minValue = minMax.x * scalar;
			slider.maxValue = minMax.y * scalar;

			slider.value = v * scalar;
			slider.GetComponentInChildren<TextMeshProUGUI>().text = (v).ToString("0.##");
			ClearAndResetKeyframes();
		}

		private void UpdateKeyframe(float v)
		{
			currentKeyframe = Mathf.FloorToInt(v*10);
			string val = attributeDropdown.options[attributeDropdown.value].text;
			Enum.TryParse(val, out PatternDataKey datType);
			AnimationCurve curv = targetTimelineData.data[datType];
			slider.SetValueWithoutNotify(curv.Evaluate(currentKeyframe) * scalar);
			slider.GetComponentInChildren<TextMeshProUGUI>().text = (curv.Evaluate(currentKeyframe)).ToString("0.##");
			changed = true;
		}

		private void UpdateValue(float v)
		{
			UpdateValue(v, -1);
		}


		private void UpdateValue(float v, int kf)
		{
			string val = attributeDropdown.options[attributeDropdown.value].text;
			Enum.TryParse(val, out PatternDataKey datType);
			AnimationCurve curv = targetTimelineData.data[datType];

			if (kf < 0)
			{
				kf = currentKeyframe;
				if (currentKeyframe == 0 && curv.postWrapMode == WrapMode.Loop)
				{
					UpdateValue(v, 10);
				}

				if (currentKeyframe == 10 && curv.postWrapMode == WrapMode.Loop)
				{
					UpdateValue(v, 0);
				}
			}

			v /= scalar;
			slider.GetComponentInChildren<TextMeshProUGUI>().text = v.ToString("0.##");
			Keyframe[] keys = curv.keys;

			if (keys.All(k => Mathf.FloorToInt(k.time) != kf))
			{
				Keyframe key = new Keyframe(kf, v);
				curv.AddKey(key);
				AddKeyframe(kf);
				//int i = Array.IndexOf(curv.keys, kf);
				//AnimationUtility.SetKeyLeftTangentMode(curv, i, AnimationUtility.TangentMode.Linear);
				//AnimationUtility.SetKeyRightTangentMode(curv, i, AnimationUtility.TangentMode.Linear);
			}
			else
			{
				Keyframe key = curv.keys.First(k => Mathf.FloorToInt(k.time) == kf);
				int k = Array.IndexOf(curv.keys, key);
				curv.RemoveKey(k);
				key = new Keyframe(kf, v);
				curv.AddKey(key);
			}

			int w = Array.IndexOf(loopNames, curv.postWrapMode.ToString());
			loopDropdown.SetValueWithoutNotify(w);
			changed = true;
		}

		private void ClearAndResetKeyframes()
		{
			foreach(GameObject g in keysframes)
				Destroy(g);
			string val = attributeDropdown.options[attributeDropdown.value].text;
			Enum.TryParse(val, out PatternDataKey datType);
			AnimationCurve curv = targetTimelineData.data[datType];
			foreach (Keyframe k in curv.keys)
			{
				AddKeyframe(Mathf.RoundToInt(k.time));
			}
			changed = true;
		}

		private List<GameObject> keysframes = new List<GameObject>();
		private void AddKeyframe(int pt)
		{
			string val = attributeDropdown.options[attributeDropdown.value].text;
			Enum.TryParse(val, out PatternDataKey datType);
			AnimationCurve curv = targetTimelineData.data[datType];

			if (pt == 0 || pt == 10) return;
			GameObject go = Instantiate(keyframePrefab, transform, false);
			keysframes.Add(go);
			float ay = lineRectTransform.position.y;
			float yy = pt/10f * lineRectTransform.rect.height;
			go.transform.position = new Vector3(go.transform.position.x, ay - yy, 0);
			//RectTransform r = ((RectTransform)keyFrameSlider.transform);
			//((RectTransform)go.transform).anchoredPosition = new Vector3(-20, GetPoint(pt*2, 1).y + r.anchoredPosition.y + r.rect.height, 0);
			go.GetComponent<Button>().onClick.AddListener(() =>
			{
				Keyframe key = curv.keys.First(k => Mathf.FloorToInt(k.time) == pt);
				int k = Array.IndexOf(curv.keys, key);
				curv.RemoveKey(k);
				changed = true;
				slider.SetValueWithoutNotify(curv.Evaluate(currentKeyframe) * scalar);
				keysframes.Remove(go);
				Destroy(go);
			});
		}

		public void ResetAnim()
		{
			targetPatternObj.SetTime(0);
		}

		List<PatternDataKey> GetAllowedValues(bool forGun)
		{
			if(forGun)
				return new List<PatternDataKey>() { PatternDataKey.FireShot, PatternDataKey.Rotation };
			else
				return new List<PatternDataKey>() {
					PatternDataKey.Rotation,
					PatternDataKey.Size,
					PatternDataKey.Speed,
				};
		}
		Vector3 GetAllowedRange(PatternDataKey dataKey, bool forGun)
		{
			if(forGun)
				switch (dataKey)
				{
					case PatternDataKey.FireShot:
						return new Vector3(0, 1, 1);
					case PatternDataKey.Rotation:
						return new Vector3(-2, 2, 4);
				}
			else
				switch (dataKey)
				{
					case PatternDataKey.Speed:
					case PatternDataKey.Size:
						return new Vector3(0, 10, 4);
					case PatternDataKey.Rotation:
						return new Vector3(-2, 2, 10);
				}
			return Vector3.zero;
		}
	}
}
