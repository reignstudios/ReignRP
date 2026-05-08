using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCamera : MonoBehaviour
{
	internal new Transform transform;
	private float rot;

	public Vector3 center;
	public float distance = 8;
	public float speed = 1;

	private void Start()
	{
		transform = GetComponent<Transform>();
	}

	private void Update()
	{
		if (Reign.SRP.ReignRenderPipeline.xrActive) return;

		transform.position = center + (new Vector3(Mathf.Cos(rot), .25f, Mathf.Sin(rot)) * distance);
		transform.LookAt(center);
		rot += speed * Time.deltaTime;
	}
}
