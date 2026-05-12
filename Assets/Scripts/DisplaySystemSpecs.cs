using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplaySystemSpecs : MonoBehaviour
{
	private float time;
	private int frame;
	private string fps;

	private string osInfo, platformInfo;

	private void Start()
	{
		osInfo = $"OS '{SystemInfo.operatingSystem}'";
		platformInfo = $"GPU:'{SystemInfo.graphicsDeviceName}' API:{SystemInfo.graphicsDeviceType} Rez:({Screen.width}, {Screen.height})";
	}

	private void Update()
	{
		frame++;
		time += Time.deltaTime;
		if (time >= 1)
		{
			time = 0;
			fps = $"FPS: {frame}";
			frame = 0;
		}
	}

	private void OnGUI()
	{
		#if !UNITY_EDITOR && !UNITY_STANDALONE
		if (Reign.SRP.ReignRP.xrActive) return;
		#endif
		
		var rect = new Rect(10, 10, 100, 20);
		GUI.Label(rect, fps);

		rect.width = 500;
		rect.y += 20;
		GUI.Label(rect, osInfo);

		rect.y += 20;
		rect.height *= 2;
		GUI.Label(rect, platformInfo);
	}
}
