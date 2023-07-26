using Assets.draco18s;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	public class PlayerCollectable : MonoBehaviour
	{
		public Sprite image;
		public Vector2 previousPosition1;
		public Vector2 previousPosition2;
		public Vector2 previousPosition3;

		[UsedImplicitly]
		void Start()
		{
			GetComponent<SpriteRenderer>().sprite = image;
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			previousPosition1 = previousPosition2;
			previousPosition2 = previousPosition3;
			previousPosition3 = transform.localPosition;
			float dt = Time.fixedDeltaTime;

			transform.Translate(new Vector3(0, -1 * dt, dt), Space.Self);
		}
	}
}