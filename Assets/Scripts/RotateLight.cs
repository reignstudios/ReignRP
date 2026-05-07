using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateLight : MonoBehaviour
{
	internal new Transform transform;
	public Vector3 rotSpeed, radius = new Vector3(1, 1, 1);
	private Vector3 rot;
	private Vector3 center;

	private new Light light;

	private void Start()
	{
		transform = GetComponent<Transform>();
		light = GetComponent<Light>();
		center = transform.position;
		light.color = new Color(Random.value + .5f, Random.value + .5f, Random.value + .5f);
	}

	private void Update()
	{
		transform.position = center + new Vector3(Mathf.Sin(rot.x) * radius.x, Mathf.Sin(rot.y) * radius.y, Mathf.Sin(rot.z) * radius.z);
		rot += rotSpeed * Time.deltaTime;
	}
}
