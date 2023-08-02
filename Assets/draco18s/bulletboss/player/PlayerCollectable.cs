using Assets.draco18s;
using Assets.draco18s.training;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.draco18s.bulletboss
{
	public class PlayerCollectable : MonoBehaviour
	{
		public Sprite image;
		public Vector2 previousPosition1;
		public Vector2 previousPosition2;
		public Vector2 previousPosition3;
		public float speed = 3 / 5f;

		[UsedImplicitly]
		void Start()
		{
			//GetComponent<SpriteRenderer>().sprite = image;
		}

		[UsedImplicitly]
		void FixedUpdate()
		{
			previousPosition1 = previousPosition2;
			previousPosition2 = previousPosition3;
			previousPosition3 = transform.localPosition;
			float dt = Time.fixedDeltaTime;

			transform.Translate(new Vector3(0, -1 * dt * speed, dt), Space.Self);

			if (transform.localPosition.y < -5)
			{
				Destroy(gameObject);
			}
		}

		[UsedImplicitly]
		void OnTriggerEnter2D(Collider2D other)
		{
			if (other.gameObject.layer == LayerMask.NameToLayer("AIPlayer"))
			{
				PlayerAgent ag = other.GetComponentInParent<PlayerAgent>();
				ag.AddReward(0.01f);
				Destroy(gameObject);
			}
		}
	}
}