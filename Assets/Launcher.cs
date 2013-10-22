using UnityEngine;
using System.Collections;
using System.IO;

public class Launcher : MonoBehaviour {
	public int launcherVersion;
	private int gameVersion = 0;
	
	public Font font;
	public GUIStyle textStyle;
	//public GUIStyle playButtonStyle;
	public GUIStyle progressBarEmptyStyle;
	public GUIStyle progressBarFullStyle;
	public GUIStyle progressBarTextStyle;
	
	private string statusMessage = "Checking for updates...";
	
	private string launcherVersionURL = "http://battle-of-mages.com/download/launcher.ini";
	private string gameVersionURL = "http://battle-of-mages.com/download/game.ini";
	
	private WWW request;
	private float downloadStartTime;
	private string exePath;
	private bool installed = false;
	private float progress;
	private bool windowedFullScreen;
	private string curDir;
	
	// Desktop resolution
	private int maxWidth = 0;
	private int maxHeight = 0;
	
	void Awake() {
		Screen.fullScreen = false;
		
		Resolution[] resolutions = Screen.resolutions;
        foreach (Resolution res in resolutions) {
			/*if(res.width >= maxWidth || (res.width >= maxWidth && res.height >= maxHeight)) {
				maxWidth = res.width;
				maxHeight = res.height;
			}*/
			
			Debug.Log("Supported resolution: " + res.width + "x" + res.height);
		}
		
		maxWidth = Screen.currentResolution.width;
		maxHeight = Screen.currentResolution.height;
		Debug.Log("Current resolution: " + maxWidth + "x" + maxHeight);
	}
	
	// Use this for initialization
	void Start () {
		Application.targetFrameRate = 20;
		
		windowedFullScreen = PlayerPrefs.GetInt("Launcher_GraphicsMode", 1) == 1;
		
		// Clear download cache
		try {
			Directory.Delete("./Download", true);
		} catch(DirectoryNotFoundException) {
			Debug.Log("Download directory does not exist yet.");
		}
		
		if(!Directory.Exists("./GameClient"))
			Directory.CreateDirectory("./GameClient");
		if(!Directory.Exists("./Download"))
			Directory.CreateDirectory("./Download");
		
		curDir = Directory.GetCurrentDirectory().Replace("\\", "/");
		exePath = curDir + "/GameClient/BoM-LobbyClient.exe";
		
		StartCoroutine(LauncherDownload());
	}
	
	// LauncherDownload
	IEnumerator LauncherDownload() {
		statusMessage = "Downloading launcher updates...";
		
		request = new WWW(launcherVersionURL);
		downloadStartTime = Time.time;
		
		// Wait for download to finish
		yield return request;
		
		string launcherURL = "";
		int nextVersion = 0;
		
		if(request.error == null) {
			var lines = request.text.Split('\n');
			
			nextVersion = int.Parse(lines[0].Split('=')[1]);
			launcherURL = ParseURL(lines[1].Split('=')[1], nextVersion);
			
			Debug.Log ("Launcher version: " + nextVersion + " @ " + launcherURL);
			
			// Download launcher
			if(launcherVersion < nextVersion) {
				request = new WWW(launcherURL);
				downloadStartTime = Time.time;
				
				// Wait for download to finish
				yield return request;
				
				Debug.Log ("Finished downloading new launcher.");
				
				progress = 1f;
				yield return null;
				
				string launcherExeFile = "BoM-Launcher.exe";
				
				/*string launcherDataDir = "BoM-Launcher_Data";
				
				try {
					Directory.Delete(launcherDataDir, true);
				} catch(DirectoryNotFoundException) {
					Debug.Log(launcherDataDir + " has already been deleted.");
				} catch(System.Exception e) {
					Fail(e);
				}
				
				try {
					File.Delete(launcherExeFile);
				} catch(System.Exception e) {
					Fail(e);
				}*/
				
				// Install launcher
				string newLauncherPath = curDir + "/Download/NewLauncher/";
				var enumer = InstallPatch("launcher.7z", newLauncherPath);
				while(enumer.MoveNext()) {
					yield return null;
				}
				
				string launcherExePath = curDir + "/" + launcherExeFile;
				Debug.Log ("Launcher installation finished, launcher is located at: " + launcherExePath);
				
				progress = 1f;
				statusMessage = "Finished installing the latest launcher version.";
				yield return null;
				
				// Restart
				StartProcess(curDir + "/Tools/updatelauncher.bat", "\"" + curDir + "\\Tools\\xcopy.exe\" " + "\"" + newLauncherPath.Replace("/", "\\") + "*\" \"" + curDir.Replace("/", "\\") + "\" \"" + launcherExePath.Replace("/", "\\") + "\"", false);
				Application.Quit();
			} else {
				// Download game
				StartCoroutine(GameDownload());
			}
		} else {
			statusMessage = "Error downloading launcher version info.";
			Debug.LogError(request.error);
		}
	}
	
	// GameDownload
	IEnumerator GameDownload() {
		statusMessage = "Downloading game updates...";
		
		request = new WWW(gameVersionURL);
		downloadStartTime = Time.time;
		
		yield return request;
		
		string gameURL = "";
		int nextVersion = 0;
		
		if(request.error == null) {
			var lines = request.text.Split('\n');
			
			nextVersion = int.Parse(lines[0].Split('=')[1]);
			gameURL = ParseURL(lines[1].Split('=')[1], nextVersion);
			
			Debug.Log ("Game version: " + nextVersion + " @ " + gameURL);
		} else {
			statusMessage = "Error downloading game version info.";
			Debug.LogError(request.error);
		}
		
		string versionFilePath = curDir + "/GameClient/version.ini";
		
		gameVersion = LoadVersion(versionFilePath);
		
		if(gameVersion < nextVersion) {
			request = new WWW(gameURL);
			downloadStartTime = Time.time;
			
			// Wait for download to finish
			yield return request;
			
			Debug.Log ("Finished downloading new launcher.");
			
			progress = 1f;
			yield return null;
			
			if(request.error == null) {
				// Install game
				var enumer = InstallPatch("game.7z", curDir + "/GameClient/");
				while(enumer.MoveNext()) {
					yield return null;
				}
				
				// Write version file
				WriteVersion(versionFilePath, nextVersion);
				
				Debug.Log ("Installation finished, client is located at: " + exePath);
				
				progress = 1f;
				statusMessage = "Finished installing the latest version.";
				installed = true;
			} else {
				statusMessage = "Error downloading latest game version.";
				Debug.LogError(request.error);
			}
		} else {
			if(File.Exists(exePath))
				installed = true;
		}
	}
	
	// InstallPatch
	IEnumerator InstallPatch(string fileName, string extractTo) {
		// Write patch file
		statusMessage = "Installing [1/2]...";
		progress = 0f;
		
		yield return null;
		
		try {
			File.WriteAllBytes("./Download/" + fileName, request.bytes);
		} catch (System.Exception e) {
			Fail(e);
		}
		
		// Delete all existing client files
		/*statusMessage = "Installing [2/3]...";
		progress = 0.02f;
		yield return null;
		
		try {
			Directory.Delete("./GameClient", true);
		} catch (System.Exception e) {
			Fail(e);
		}*/
		
		// Extract it into the client directory
		statusMessage = "Installing [2/2]...";
		progress = 0.05f;
		yield return null;
		
		//Directory.CreateDirectory("./GameClient");
		
		string cmd = curDir + "/Tools/7za.exe";
		string args = "x \"" + curDir + "/Download/" + fileName + "\" -o\"" + extractTo + "\" -aoa";
		Debug.Log ("Starting extraction: " + cmd + " with arguments " + args);
		yield return null;
		
		System.Diagnostics.Process p = null;
		
		try {
			//var extractProcess = System.Diagnostics.Process.Start(extractExe, extractArgs);
			//extractProcess.WaitForExit();
			p = StartProcess(cmd, args, false, false);
		} catch (System.Exception e) {
			Fail(e);
		}
		
		if(p == null)
			Fail(null);
		
		Debug.Log (request.size);
		while(!p.HasExited) {
			progress += Time.deltaTime / 12f;
			
			if(progress > 0.99f)
				progress = 0.99f;
			
			yield return null;
		}
	}
	
	// LoadVersion
	int LoadVersion(string filePath) {
		string content;
		
		try {
			content = File.ReadAllText(filePath);
		} catch(FileNotFoundException) {
			return 0;
		}
		
		return int.Parse(content.Split('=')[1]);
	}
	
	// WriteVersion
	void WriteVersion(string filePath, int versionNumber) {
		string content = "Version=" + versionNumber.ToString();
		
		File.WriteAllText(filePath, content);
	}
	
	// To start a process silently
	public static System.Diagnostics.Process StartProcess(string cmd, string args = "", bool show = true, bool shell = true) {
		System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(cmd, args);
		System.Diagnostics.Process p = new System.Diagnostics.Process();
		
		if(!show) {
			psi.CreateNoWindow = true;
		}
		
		if(!shell) {
			psi.UseShellExecute = false;
		}
		
		p.StartInfo = psi;
		p.Start();
		
		return p;
	}
	
	void OnGUI() {
		// Set font
		if(GUI.skin.font != font)
			GUI.skin.font = font;
		
		var width = Screen.width * 0.9f;
		var height = 92;
		
		GUILayout.BeginArea(new Rect(Screen.width / 2 - width / 2, Screen.height / 2 - height / 2, width, height));
		GUILayout.BeginVertical();
		
		if(!installed) {
			GUILayout.Label(statusMessage, textStyle);
			
			if(!request.isDone)
				progress = request.progress;
			
			DrawProgress();
		} else {
			DrawPlayButton();
		}
		
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}
	
	void DrawPlayButton() {
		GUI.skin.button.fontSize = 32;
		GUI.skin.button.padding = new RectOffset(15, 15, 15, 15);
		
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.BeginVertical();
		
		if(GUILayout.Button("Play", GUILayout.Width(224))) {
			string args = "-adapter 0";
			
			if(windowedFullScreen) {
				args += " -popupwindow -screen-width " + maxWidth + " -screen-height " + maxHeight;
			}
			
			//System.Diagnostics.Process.Start(exePath, args);
			StartProcess(exePath, args, true);
			Application.Quit();
		}
		
		bool newWindowedFullScreen = GUILayout.Toggle(windowedFullScreen, " Windowed fullscreen");
		
		if(newWindowedFullScreen != windowedFullScreen) {
			windowedFullScreen = newWindowedFullScreen;
			PlayerPrefs.SetInt("Launcher_GraphicsMode", windowedFullScreen ? 1 : 0);
		}
		
		GUILayout.EndVertical();
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}
	
	void DrawProgress() {
		ProgressBar(
			((int)(progress * 100)) + " %",
			progress,
			new Color(0.0f, 0.25f, 0.5f, 1.0f),
			progressBarEmptyStyle,
			Color.black,
			progressBarFullStyle,
			new Color(0.0f, 0.5f, 1.0f, 1.0f),
			progressBarFullStyle,
			Color.white,
			progressBarTextStyle
		);
		
		// Show remaining time
		if(request.progress != 0f && !request.isDone) {
			GUILayout.Space(12);
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			
			int secs = (int)((1 / (request.progress) - 1) * (Time.time - downloadStartTime));
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
	
	void Fail(System.Exception e) {
		statusMessage = "Error installing the latest version.";
		Debug.LogError(e != null ? e.ToString() : "Unknown error.");
		installed = false;
	}
	
	string ParseURL(string url, int nextVersion) {
		return url.Replace("{version}", nextVersion.ToString()).Replace("{os}", "windows");
	}
	
	// Progress bar
	public static void ProgressBar(
		string text,
		float progressValue,
		Color backgroundColor,
		GUIStyle backgroundStyle,
		Color backgroundFillColor,
		GUIStyle backgroundFillStyle,
		Color fillColor,
		GUIStyle fillStyle,
		Color textColor,
		GUIStyle textStyle
	) {
		if(progressValue > 1f)
			progressValue = 1f;
		
		GUI.color = backgroundColor;
		GUILayout.Box("", backgroundStyle);
		
		var levelRect = GUILayoutUtility.GetLastRect();
		var levelFilledRect = new Rect(levelRect);
		levelFilledRect.x += 1;
		levelFilledRect.y += 1;
		levelFilledRect.width -= 2;
		levelFilledRect.height -= 2;
		
		GUI.color = backgroundFillColor;
		GUI.Box(levelFilledRect, "", backgroundFillStyle);
		
		levelFilledRect.width *= progressValue;
		
		GUI.color = fillColor;
		GUI.Box(levelFilledRect, "", fillStyle);
		
		GUI.color = textColor;
		GUI.Label(levelRect, text, textStyle);
	}
}
