using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Reign.SRP
{
	static class OpenGLUtils
	{
		[DllImport("libGLESv3")]
		public static extern void glDepthRangef(float near, float far);// Mobile

		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		[DllImport("Opengl32.dll")]
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		[DllImport("System/Library/Frameworks/OpenGL.framework/OpenGL")]
		#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
		[DllImport("libGL.so.1")]
		#endif
		public static extern void glDepthRange(double near, double far);// PC
	}
}
