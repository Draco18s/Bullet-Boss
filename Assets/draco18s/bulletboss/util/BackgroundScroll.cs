using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
	public float size = 38.4f;
	public float speed = 0.333f;

	[UsedImplicitly]
    void Update()
    {
	    float dt = Time.deltaTime;
        transform.Translate(new Vector3(0,speed * dt,0));
        if (transform.localPosition.y * Math.Sign(speed) >= size)
        {
	        transform.localPosition = new Vector3(0, transform.localPosition.y - size * 2 * Math.Sign(speed), transform.localPosition.z);

        }
    }
}
