using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;

public class VunglePostBuilder
{
	// Message to show the user to get permission for IDFA (targeted ads)
	private const string NSUserTrackingUsageDescription = "To ensure the best possible ad experience";

	// XCode project constants
	private static string[] SKAdNetworks = new string[]
	{
		// Updated 10/15/2020
		"GTA9LK7P23.skadnetwork", // Vungle
		"ydx93a7ass.skadnetwork", // Adikteev
		"4FZDC2EVR5.skadnetwork", // Aarki
		"4PFYVQ9L8R.skadnetwork", // AdColony
		"v72qych5uu.skadnetwork", // Appier
		"mlmmfzh3r3.skadnetwork", // Appreciate
		"c6k4g5qg8m.skadnetwork", // Beeswax
		"YCLNXRL5PM.skadnetwork", // Jampp
		"5lm9lj6jb7.skadnetwork", // LoopMe
		"n9x2a789qt.skadnetwork", // MyTarget
		"TL55SBB4FM.skadnetwork", // Pubnative
		"2U9PT9HC89.skadnetwork", // Remerge
		"8s468mfl3y.skadnetwork", // RTB House
		"GLQZH8VGBY.skadnetwork", // Sabio
		"22mmun2rn5.skadnetwork", // Webeye
		"3RD42EKR43.skadnetwork"  // YouAppi
	};

	private static Dictionary<string, bool> XCodeFramework = new Dictionary<string, bool>()
	{
		{ "AdSupport.framework", false },
		{ "CoreTelephony.framework", false },
		{ "StoreKit.framework", false },
		{ "WebKit.framework", false },
		// For iOS 14. Weak link the framework for XCode 11
		{ "AppTrackingTransparency.framework", true },
	};

	private static string[] XCodeFiles = new string[]
	{
		"libsqlite3.dylib",
		"libz.1.1.3.dylib"
	};

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
#if !UNITY_IOS
				Debug.Log("VunglePostBuilder: The build process was started when the active target is not iOS. You may need to run the post-processor manually.");
				EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

				// If the build process started off with the active target not as iOS
				// Force the scripts to recompile after switching to enable the UNITY_IOS
				// preprocessor flag
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
#endif
				PostBuildDirectory = pathToBuiltProject;
				PostProcessIosBuild();
				break;
		}
	}

	[UnityEditor.MenuItem("Tools/Vungle/Run iOS Post Processor")]
	private static void PostProcessIosBuild()
	{
#if UNITY_IOS
		UnityEditor.iOS.Xcode.PBXProject project = new UnityEditor.iOS.Xcode.PBXProject();
		string pbxPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(PostBuildDirectory);
		project.ReadFromFile(pbxPath);

#if UNITY_2019_3_OR_NEWER
		string targetId = project.GetUnityFrameworkTargetGuid();
#else
		string targetId = project.TargetGuidByName(UnityEditor.iOS.Xcode.PBXProject.GetUnityTargetName());
#endif
		foreach(KeyValuePair<string, bool> kvp in XCodeFramework)
		{
			project.AddFrameworkToProject(targetId, kvp.Key, kvp.Value);
		}

		for (int i = 0, n = XCodeFiles.Length; i < n; i++)
		{
			string path = string.Format("usr/lib/{0}", XCodeFiles[i]);
			string projectPath = string.Format("Frameworks/{0}", XCodeFiles[i]);
			project.AddFileToBuild(targetId, project.AddFile(path, projectPath, UnityEditor.iOS.Xcode.PBXSourceTree.Sdk));
		}

		project.AddBuildProperty(targetId, "OTHER_LDFLAGS", "-ObjC");

		string plistPath = System.IO.Path.Combine(PostBuildDirectory, "Info.plist");
		UnityEditor.iOS.Xcode.PlistDocument plist = new UnityEditor.iOS.Xcode.PlistDocument();
		plist.ReadFromFile(plistPath);

		UnityEditor.iOS.Xcode.PlistElementDict rootDict = plist.root;
		UnityEditor.iOS.Xcode.PlistElementArray skAdNetworkArray = rootDict.CreateArray("SKAdNetworkItems");
		UnityEditor.iOS.Xcode.PlistElementDict skAdNetworkIdentifierElement;

		string SKAdNetworkIdentifier = "SKAdNetworkIdentifier";
		for (int i = 0, n = SKAdNetworks.Length; i < n; i++)
		{
			skAdNetworkIdentifierElement = skAdNetworkArray.AddDict();
			skAdNetworkIdentifierElement.SetString(SKAdNetworkIdentifier, SKAdNetworks[i]);
		}
		rootDict.SetString("NSUserTrackingUsageDescription", NSUserTrackingUsageDescription);

		plist.WriteToFile(plistPath);

		project.WriteToFile(pbxPath);

		Debug.Log("Vungle iOS post processor completed.");
#else
		Debug.LogWarning("VunglePostBuilder: The active build target is not iOS. The Vungle post-processor has not run.");
#endif
	}

	[UnityEditor.MenuItem("Tools/Vungle/Open Documentation Website...")]
	static void DocumentationSite()
	{
		UnityEditor.Help.BrowseURL("https://support.vungle.com/hc/en-us/articles/360003455452");
	}

	[MenuItem("Tools/Vungle/Switch Platform - Android")]
	public static void PerformSwitchAndroid()
	{
		// Switch to Android build.
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
	}

	[MenuItem("Tools/Vungle/Switch Platform - iOS")]
	public static void PerformSwitchiOS()
	{
		// Switch to iOS build.
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
	}

	[MenuItem("Tools/Vungle/Switch Platform - Windows")]
	public static void PerformSwitchWindows()
	{
		// Switch to UWP build.
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);
	}
}
