using UnityEngine;
using System.IO;

public class Launcher : MonoBehaviour {
	public Vector2 windowSize;
	public Font font;
	public GUIStyle textStyle;
	public ProgressBarStyle progressBarStyle;

	private bool windowedFullScreen;

	// Start
	void Start () {
		// Resolution
		Screen.SetResolution((int)windowSize.x, (int)windowSize.y, false);

		// Frame rate
		Application.targetFrameRate = 20;

		// Load settings
		windowedFullScreen = PlayerPrefs.GetInt("Launcher_GraphicsMode", 1) == 1;
	}
	
	// OnGUI
	void OnGUI() {
		// Set font
		if(GUI.skin.font != font)
			GUI.skin.font = font;
		
		var width = Screen.width * 0.9f;
		var height = 92;
		
		GUILayout.BeginArea(new Rect(Screen.width / 2 - width / 2, Screen.height / 2 - height / 2, width, height));
		GUILayout.BeginVertical();

		if(!Updater.instance.installed) {
			GUILayout.Label(Updater.instance.statusMessage, textStyle);
			
			DrawProgress();
		} else {
			DrawPlayInterface();
		}
		
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}
	
	// DrawPlayInterface
	void DrawPlayInterface() {
		GUI.skin.button.fontSize = 32;
		GUI.skin.button.padding = new RectOffset(15, 15, 15, 15);
		
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.BeginVertical();

		// Play button
		DrawPlayButton();

		// Options
		DrawOptions();
		
		GUILayout.EndVertical();
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

	// DrawPlayButton
	void DrawPlayButton() {
		if(GUILayout.Button("Play", GUILayout.Width(224))) {
			string args = "-adapter 0";
			
			if(windowedFullScreen) {
				args += " -popupwindow -screen-width " + ResolutionFinder.desktopWidth + " -screen-height " + ResolutionFinder.desktopHeight;
			}
			
			Launcher.StartProcess(Updater.instance.exePath, args, true);
			Application.Quit();
		}
	}

	// DrawOptions
	void DrawOptions() {
		// Windowed fullscreen
		bool newWindowedFullScreen = GUILayout.Toggle(windowedFullScreen, " Windowed fullscreen");
		
		if(newWindowedFullScreen != windowedFullScreen) {
			windowedFullScreen = newWindowedFullScreen;
			PlayerPrefs.SetInt("Launcher_GraphicsMode", windowedFullScreen ? 1 : 0);
		}
	}
	
	// DrawProgress
	void DrawProgress() {
		GUIHelper.ProgressBar(
			((int)(Updater.instance.progress * 100)) + " %",
			Updater.instance.progress,
			"",
			progressBarStyle
		);
		
		// Show remaining time
		if(Updater.instance.progress > 0f && !Updater.instance.isDone) {
			GUILayout.Space(12);
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			
			int secs = (int)((1 / (Updater.instance.progress) - 1) * (Time.time - Updater.instance.downloadStartTime));
			var t = System.TimeSpan.FromSeconds(secs);
			string remainingTime = t.Seconds + "s";
			if(t.Minutes > 0)
				remainingTime = t.Minutes + "m " + remainingTime;
			if(t.Hours > 0)
				remainingTime = t.Hours + "h " + remainingTime;

			GUILayout.Label("≈ " + remainingTime + " remaining", textStyle);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
	}

	// To start a process silently
	public static System.Diagnostics.Process StartProcess(
		string cmd,
		string args = "",
		bool show = true,
		bool shell = true
		) {
		var processStartInfo = new System.Diagnostics.ProcessStartInfo(cmd, args);
		var process = new System.Diagnostics.Process();
		
		if(!show) {
			processStartInfo.CreateNoWindow = true;
			processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
		}
		
		if(!shell) {
			processStartInfo.UseShellExecute = false;
		}
		
		process.StartInfo = processStartInfo;
		process.Start();
		
		return process;
	}
}
