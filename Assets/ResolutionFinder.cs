using UnityEngine;
using System.Collections;

public class ResolutionFinder : MonoBehaviour {
	// Desktop resolution
	public static int desktopWidth;
	public static int desktopHeight;

	// Awake
	void Awake() {
		Screen.fullScreen = false;

		var resolutions = Screen.resolutions;

		foreach(var res in resolutions) {
			Debug.Log("Supported resolution: " + res.width + "x" + res.height);
		}
		
		desktopWidth = Screen.currentResolution.width;
		desktopHeight = Screen.currentResolution.height;
		Debug.Log("Current resolution: " + desktopWidth + "x" + desktopHeight);
	}
}
