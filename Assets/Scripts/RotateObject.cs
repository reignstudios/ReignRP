using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
	internal new Transform transform;
	public Vector3 rotSpeed;
	private Vector3 rot;

	private void Start()
	{
		transform = GetComponent<Transform>();
	}

	private void Update()
	{
		transform.rotation = Quaternion.Euler(rot);
		rot += rotSpeed * Time.deltaTime;
	}
}
