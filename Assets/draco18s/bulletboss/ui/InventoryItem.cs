using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.draco18s.bulletboss.ui
{
	public class InventoryItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		private Transform scrollTransform;
		private RectTransform rectTransform;
		private bool isDragging = false;
		private bool isAttached = false;
		public UpgradeRuntime upgradeTypeData;

		[UsedImplicitly]
		void Start()
	    {
			rectTransform = GetComponent<RectTransform>();
			scrollTransform = transform.parent;
			if (upgradeTypeData == null)
			{
				Destroy(gameObject);
				return;
			}

			Image img = GetComponent<Image>();
		    img.sprite = upgradeTypeData.image;
	    }

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;
			
			if (isAttached)
			{
				transform.parent?.GetComponent<IInventoryDropTarget>()?.Clear(this);
			}
			isDragging = true;
			transform.SetParent(Inventory.instance.transform, false);
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
				GraphicRaycaster raycaster = GetComponentInParent<GraphicRaycaster>();
				raycaster.Raycast(dat, results);
				results.RemoveAll(r => r.gameObject == gameObject);
				if (results.Count > 0)
				{
					target = results[0].gameObject.GetComponent<IInventoryDropTarget>();
					bool didAttach = target?.Attach(this) ?? false;
					if (didAttach)
					{
						isAttached = true;
						return;
					}
					transform.SetParent(scrollTransform);
				}
				else if(isAttached)
				{
					Inventory.instance.Add(this);
					isAttached = false;
				}
			}

			if (!isAttached)
			{
				transform.SetParent(scrollTransform);
			}
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