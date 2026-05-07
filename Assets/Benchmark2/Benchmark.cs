using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Benchmark : MonoBehaviour
{
	public Transform cameraTransform;

	public int instancesVolumeSize = 10;
	public float prefabSize = .1f;
	public GameObject prefab;

	private float fpsStep;
	private int fpsCount;
	private string fps;

	private void Start()
	{
		Application.targetFrameRate = 90;
		QualitySettings.vSyncCount = 0;

		for (int x = 0; x < instancesVolumeSize; x++)
		{
			for (int y = 0; y < instancesVolumeSize; y++)
			{
				for (int z = 0; z < instancesVolumeSize; z++)
				{
					var obj = Instantiate(prefab);
					obj.transform.parent = transform;

					float offset = instancesVolumeSize / 2f;
					obj.transform.position = new Vector3(x - offset, y - offset, z - offset) * prefabSize * 2f;
					obj.transform.localScale = new Vector3(prefabSize, prefabSize, prefabSize);
					obj.isStatic = true;
				}
			}
		}

		cameraTransform.position = new Vector3(0, 0, -(instancesVolumeSize * prefabSize * 2));
	}

	private void Update()
	{
		fpsStep += Time.deltaTime;
		if (fpsStep >= 1f)
		{
			fpsStep %= 1f;
			fps = $"FPS: {fpsCount}";
			fpsCount = 0;
		}
		fpsCount++;
	}

	private void OnGUI()
	{
		GUI.TextArea(new Rect(10, 10, 128, 32), fps);
	}
}
