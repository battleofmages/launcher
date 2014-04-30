using UnityEngine;
using System.Collections;
using System.IO;

public class Updater : MonoBehaviour {
	public static Updater instance;

	public int installedLauncherVersion;
	public string statusMessage = "Checking for updates...";
	public string launcherExeFile = "BoM-Launcher.exe";

	public string launcherVersionURL;
	public string gameVersionURL;

	private WWW request;
	private int installedGameVersion;

	private int launcherVersionOnline = -1;
	private int gameVersionOnline = -1;

	private string launcherURL;
	private string gameURL;
	
	private string newLauncherPath;

	// Awake
	void Awake() {
		instance = this;

		// Clear download cache
		try {
			Directory.Delete("./Download", true);
		} catch(DirectoryNotFoundException) {
			Debug.Log("Download directory does not exist yet.");
		}
		
		// Create directories if they don't exist yet
		if(!Directory.Exists("./GameClient"))
			Directory.CreateDirectory("./GameClient");
		
		if(!Directory.Exists("./Download"))
			Directory.CreateDirectory("./Download");

		// Paths
		currentDir = Directory.GetCurrentDirectory().Replace("\\", "/");
		exePath = currentDir + "/GameClient/BoM-LobbyClient.exe";
		newLauncherPath = currentDir + "/Download/NewLauncher/";
	}

	// Start
	void Start() {
		StartCoroutine(LauncherDownload());
	}

	// Update
	void Update() {
		if(!isDone)
			progress = request.progress;
	}

#region Coroutines
	// --------------------------------------------------------------------------------
	// Launcher download
	// --------------------------------------------------------------------------------
	public IEnumerator LauncherDownload() {
		statusMessage = "Downloading launcher updates...";

		yield return StartCoroutine(DownloadVersionInfo(
			launcherVersionURL,
			(version, url) => {
				launcherVersionOnline = version;
				launcherURL = url;
			}
		));

		if(launcherVersionOnline == -1)
			yield break;
		
		Debug.Log("Launcher version: " + launcherVersionOnline + " @ " + launcherURL);
		
		// Download launcher
		if(installedLauncherVersion >= launcherVersionOnline) {
			// Download game
			StartCoroutine(GameDownload());

			yield break;
		}

		// Wait for download to finish
		request = Download(launcherURL);
		yield return request;

		// Finished
		Debug.Log("Finished downloading new launcher.");
		progress = 1f;

		// Let the progress bar update
		yield return null;
		
		// Install launcher
		var enumer = Extract7z("launcher.7z", newLauncherPath);
		while(enumer.MoveNext()) {
			yield return null;
		}
		
		string launcherExePath = currentDir + "/" + launcherExeFile;
		Debug.Log("Launcher installation finished, launcher is located at: " + launcherExePath);
		
		progress = 1f;
		statusMessage = "Finished installing the latest launcher version.";
		yield return null;

		// Script path
		var scriptPath = currentDir + "/Tools/updatelauncher.bat";

		// Arguments for the update script
		var args = new string[] {
			currentDir + "\\Tools\\xcopy.exe",
			newLauncherPath.WindowsPath() + "*",
			launcherExePath.WindowsPath()
		};

		// Add quotes
		for(int i = 0; i < args.Length; i++)
			args[i] = "\"" + args[i] + "\"";

		// Restart
		Launcher.StartProcess(
			scriptPath,
			string.Join(" ", args),
			false
		);
		Application.Quit();
	}
	
	// --------------------------------------------------------------------------------
	// Game download
	// --------------------------------------------------------------------------------
	public IEnumerator GameDownload() {
		statusMessage = "Downloading game updates...";
		
		yield return StartCoroutine(DownloadVersionInfo(
			gameVersionURL,
			(version, url) => {
				gameVersionOnline = version;
				gameURL = url;
			}
		));
		
		if(gameVersionOnline == -1)
			yield break;

		Debug.Log("Game version: " + gameVersionOnline + " @ " + gameURL);

		// Get installed game version
		string versionFilePath = currentDir + "/GameClient/version.ini";
		installedGameVersion = LoadVersionFromFile(versionFilePath);

		// Download game
		if(installedGameVersion >= gameVersionOnline) {
			if(File.Exists(exePath))
				installed = true;

			yield break;
		}

		// Wait for download to finish
		request = Download(gameURL);
		yield return request;
		
		Debug.Log("Finished downloading new launcher.");

		// Let the progress bar update
		progress = 1f;
		yield return null;
		
		if(request.error == null) {
			// Install game
			var enumer = Extract7z("game.7z", currentDir + "/GameClient/");
			while(enumer.MoveNext()) {
				yield return null;
			}
			
			// Write version file
			WriteVersionToFile(versionFilePath, gameVersionOnline);
			
			Debug.Log("Installation finished, client is located at: " + exePath);
			
			progress = 1f;
			statusMessage = "Finished installing the latest version.";
			installed = true;
		} else {
			statusMessage = "Error downloading latest game version.";
			Debug.LogError(request.error);
		}
	}
	
	// Extract7z
	IEnumerator Extract7z(string fileName, string extractTo) {
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
		
		string cmd = currentDir + "/Tools/7za.exe";
		string args = "x \"" + currentDir + "/Download/" + fileName + "\" -o\"" + extractTo + "\" -aoa";
		Debug.Log("Starting extraction: " + cmd + " with arguments " + args);
		yield return null;
		
		System.Diagnostics.Process p = null;
		
		try {
			//var extractProcess = System.Diagnostics.Process.Start(extractExe, extractArgs);
			//extractProcess.WaitForExit();
			p = Launcher.StartProcess(cmd, args, false, false);
		} catch (System.Exception e) {
			Fail(e);
		}
		
		if(p == null)
			Fail(null);
		
		Debug.Log(request.size);
		while(!p.HasExited) {
			progress += Time.deltaTime / 12f;
			
			if(progress > 0.99f)
				progress = 0.99f;
			
			yield return null;
		}
	}

	// VersionInfoFunc
	public delegate void VersionInfoFunc(int version, string url);
	
	// DownloadVersionInfo
	public IEnumerator DownloadVersionInfo(string versionInfoURL, VersionInfoFunc func) {
		// Wait for download to finish
		request = Download(versionInfoURL);
		yield return request;
		
		if(request.error != null) {
			statusMessage = "Error downloading version info from: " + versionInfoURL;
			Debug.LogError(request.error);
			yield break;
		}
		
		int version;
		string url;
		
		ParseVersionInfo(request.text, out version, out url);
		
		if(func != null)
			func(version, url);
	}
#endregion

	// Download
	WWW Download(string url) {
		var req = new WWW(url);
		downloadStartTime = Time.time;

		return req;
	}

	// Fail
	void Fail(System.Exception e) {
		statusMessage = "Error installing the latest version.";
		Debug.LogError(e != null ? e.ToString() : "Unknown error.");
		installed = false;
	}

	// ParseVersionInfo
	public static void ParseVersionInfo(string text, out int onlineVersion, out string url) {
		var lines = text.Split('\n');
		
		onlineVersion = int.Parse(lines[0].Split('=')[1]);
		url = Updater.ParseURL(lines[1].Split('=')[1], onlineVersion);
	}

	// ParseURL
	public static string ParseURL(string url, int nextVersion) {
		return url.Replace("{version}", nextVersion.ToString()).Replace("{os}", "windows");
	}

	// LoadVersionFromFile
	public static int LoadVersionFromFile(string filePath) {
		string content;
		
		try {
			content = File.ReadAllText(filePath);
		} catch(FileNotFoundException) {
			return 0;
		}
		
		return int.Parse(content.Split('=')[1]);
	}
	
	// WriteVersionToFile
	public static void WriteVersionToFile(string filePath, int versionNumber) {
		string content = "Version=" + versionNumber.ToString();
		
		File.WriteAllText(filePath, content);
	}

#region Properties
	// Is done
	public bool isDone {
		get {
			return request.isDone;
		}
	}

	// Progress
	public float progress {
		get;
		protected set;
	}

	// Download start time
	public float downloadStartTime {
		get;
		protected set;
	}

	// Current directory
	public string currentDir {
		get;
		protected set;
	}

	// Installed
	public bool installed {
		get;
		protected set;
	}

	// Executable path
	public string exePath {
		get;
		protected set;
	}
#endregion
}
