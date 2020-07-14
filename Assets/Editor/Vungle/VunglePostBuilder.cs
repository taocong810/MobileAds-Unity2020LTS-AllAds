using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Diagnostics;

public class VunglePostBuilder
{
	private static string PostBuildDirectoryKey { get { return "VunglePostBuildPath-" + PlayerSettings.productName; } }
	private static string PostBuildDirectory
	{
		get
		{
			return EditorPrefs.GetString(PostBuildDirectoryKey);
		}
		set
		{
			EditorPrefs.SetString(PostBuildDirectoryKey, value);
		}
	}

	[PostProcessBuild(800)]
	private static void OnPostProcessBuildPlayer(BuildTarget target, string pathToBuiltProject)
	{
		switch (target)
		{
			case BuildTarget.iOS:
				PostProcessIosBuild(pathToBuiltProject);
				break;
		}
	}

	private static void PostProcessIosBuild(string pathToBuiltProject)
	{
		PostBuildDirectory = pathToBuiltProject;

		// grab the path to the postProcessor.py file
		var scriptPath = Path.Combine(Application.dataPath, "Editor/Vungle/VunglePostProcessor.py");

		// sanity check
		if (!File.Exists(scriptPath))
		{
			UnityEngine.Debug.LogError("Vungle post builder could not find the VunglePostProcessor.py file. Did you accidentally delete it?");
			return;
		}

		var pathToNativeCodeFiles = Path.Combine(Application.dataPath, "Plugins/iOS/VungleSDK");

		var args = string.Format("\"{0}\" \"{1}\" \"{2}\"", scriptPath, pathToBuiltProject, pathToNativeCodeFiles);
		ProcessStartInfo psi = new ProcessStartInfo
		{
			FileName = "python",
			Arguments = args,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		var errors = string.Empty;
		var results = string.Empty;
		using (var process = Process.Start(psi))
		{
			errors = process.StandardError.ReadToEnd();
			results = process.StandardOutput.ReadToEnd();
		}

		UnityEngine.Debug.LogFormat("Vungle iOS post processor completed.\nErrors: {0}\nResults: {1}", errors, results);
	}

	[UnityEditor.MenuItem("Tools/Vungle/Open Documentation Website...")]
	static void DocumentationSite()
	{
		UnityEditor.Help.BrowseURL("https://support.vungle.com/hc/en-us/articles/360003455452-Get-Started-with-Vungle-SDK-v-6-Unity#add-the-vungle-unity-plugin-to-your-unity-project-0-0");
	}

	[UnityEditor.MenuItem("Tools/Vungle/Run iOS Post Processor")]
	static void RunPostBuilder()
	{
		OnPostProcessBuildPlayer(BuildTarget.iOS, PostBuildDirectory);
	}

	[UnityEditor.MenuItem("Tools/Vungle/Run iOS Post Processor", true)]
	static bool ValidateRunPostBuilder()
	{
		var iPhoneProjectPath = PostBuildDirectory;
		if (iPhoneProjectPath == null || !Directory.Exists(iPhoneProjectPath))
			return false;

		var projectFile = Path.Combine(iPhoneProjectPath, "Unity-iPhone.xcodeproj/project.pbxproj");
		if (!File.Exists(projectFile))
			return false;

		return true;
	}

	//https://docs.unity3d.com/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html
	[MenuItem("Tools/Vungle/Switch Platform - Android")]
	public static void PerformSwitchAndroid()
	{
		// Switch to Windows standalone build.
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
	}

	[MenuItem("Tools/Vungle/Switch Platform - iOS")]
	public static void PerformSwitchiOS()
	{
		// Switch to Windows standalone build.
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
	}

	[MenuItem("Tools/Vungle/Switch Platform - Windows")]
	public static void PerformSwitchWindows()
	{
		// Switch to Windows standalone build.
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);
	}
}