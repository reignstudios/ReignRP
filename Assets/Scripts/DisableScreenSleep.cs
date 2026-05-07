using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableScreenSleep : MonoBehaviour
{
	private void Start()
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		#endif
	}
}
