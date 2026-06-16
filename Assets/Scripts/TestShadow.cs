using Reign.SRP;
using UnityEngine;

[ExecuteInEditMode]
public class TestShadow : MonoBehaviour
{
	public RenderTexture shadowTexture;

	private void Update()
	{
		if (ReignRP.singleton != null) shadowTexture = ReignRP.singleton.shadowTexture;
	}
}
