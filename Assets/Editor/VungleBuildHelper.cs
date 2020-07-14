using UnityEditor;
using UnityEngine;

class VungleBuildHelper
{
	// Sets the correct import settings for the Windows plugins
	[UnityEditor.MenuItem("Tools/Vungle/Prepare Windows Plugins")]
	static void PrepareWin10()
	{
		PluginImporter pi;
		pi = (PluginImporter)PluginImporter.GetAtPath("Assets/Plugins/VungleSDKProxy.dll");
		pi.SetCompatibleWithAnyPlatform(false);
		pi.SetCompatibleWithEditor(true);
		pi.SaveAndReimport();
		pi = (PluginImporter)PluginImporter.GetAtPath("Assets/Plugins/Metro/VungleSDKProxy.winmd");
		pi.SetPlatformData(BuildTarget.WSAPlayer, "PlaceholderPath", "Assets/Plugins/VungleSDKProxy.dll");
		pi.SaveAndReimport();
		pi = (PluginImporter)PluginImporter.GetAtPath("Assets/Plugins/Metro/UWP/VungleSDK.winmd");
		pi.SetPlatformData(BuildTarget.WSAPlayer, "SDK", "UWP");
		pi.SetCompatibleWithPlatform(BuildTarget.WSAPlayer, true);
		pi.SaveAndReimport();
	}
}
