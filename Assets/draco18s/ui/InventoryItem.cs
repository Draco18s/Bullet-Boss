using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.draco18s.ui{
	public class InventoryItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		private Transform scrollTransform;
		private RectTransform rectTransform;
		private bool isDragging = false;
		private bool isAttached = false;
		public UpgradeScriptable upgradeTypeData;

		[UsedImplicitly]
		void Start()
	    {
			rectTransform = GetComponent<RectTransform>();
			scrollTransform = transform.parent;
			if(upgradeTypeData == null)
				Destroy(gameObject);
			GetComponent<Image>().sprite = upgradeTypeData.image;
	    }

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;

			//if (!IsActive() || !IsInteractable())
			//	return;
			if (isAttached)
			{
				transform.parent.GetComponent<IInventoryDropTarget>().Clear(this);
			}
			isDragging = true;
			transform.parent = Inventory.instance.transform;
		}

		public virtual void OnPointerUp(PointerEventData eventData)
		{
			if (!isDragging || eventData.button != PointerEventData.InputButton.Left) return;
			isDragging = false;
			Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			Collider2D hit = Physics2D.OverlapPoint(mousePosition, LayerMask.GetMask(new[] { "UI" }));
			IInventoryDropTarget target = hit?.GetComponent<IInventoryDropTarget>();
			if (target != null && this.upgradeTypeData.type == target.slotType)
			{
				if (target.Attach(this))
				{
					isAttached = true;
					return;
				}
				transform.parent = scrollTransform;
				Inventory.instance.Remove(this);
			}
			else
			{
				PointerEventData dat = new PointerEventData(EventSystem.current);
				dat.position = Input.mousePosition;

				List<RaycastResult> results = new List<RaycastResult>();
				GraphicRaycaster raycaster = GetTopLevelRaycaster(transform);
				raycaster.Raycast(dat, results);
				if (results.Count > 0)
				{
					target = results[0].gameObject.GetComponent<IInventoryDropTarget>();
					bool didAttach = target?.Attach(this) ?? false;
					if (didAttach)
					{
						isAttached = true;
						return;
					}
					transform.parent = scrollTransform;
					Inventory.instance.Remove(this);
				}
				else
				{
					transform.parent = scrollTransform;
					if (isAttached)
					{
						Inventory.instance.Add(this);
						isAttached = false;
					}
				}
			}
		}

		public GraphicRaycaster GetTopLevelRaycaster(Transform t, GraphicRaycaster lastFound=null)
		{
			if (t == null) return lastFound;
			GraphicRaycaster canvas = t.GetComponentInParent<GraphicRaycaster>();
			return GetTopLevelRaycaster(t.parent, canvas);
		}

		[UsedImplicitly]
		void Update()
	    {
			if (isDragging)
			{
				rectTransform.position = Input.mousePosition;
			}
		}
	}
}