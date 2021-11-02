using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID
public enum VungleAdOrientation
{
	Portrait = 1,
	Landscape = 5,
	All = 6,
	AutoRotate = 6,
	MatchVideo = 8,
}

public partial class VungleAndroid : IVungleHelper
{
	private AndroidJavaObject plugin;
	private string version;

	public VungleAndroid()
	{
		using (var pluginClass = new AndroidJavaClass("com.vungle.androidplugin.VunglePlugin"))
		{
			plugin = pluginClass.CallStatic<AndroidJavaObject>("instance");
		}
	}

	public string SdkVersion
	{
		get
		{
			if (string.IsNullOrEmpty(version))
			{
				version = plugin.Call<string>("getSdkVersion");
			}
			return version;
		}
	}

	public string VersionInfo
	{
		get
		{
			return string.Format("VungleSDKCall-Android/{0}-{1}", Vungle.PluginVersion, SdkVersion);
		}
	}

	#region SDKSetup
	public void RequestTrackingAuthorization() { }

	public void UpdateConsentStatus(Vungle.Consent consent, string version = "1.0")
	{
		if (Vungle.Consent.Undefined == consent) { return; }
		plugin.Call("updateConsentStatus", (int)consent, version);
	}

	public Vungle.Consent GetConsentStatus()
	{
		return (Vungle.Consent)(plugin.Call<int>("getConsentStatus"));
	}

	public string GetConsentMessageVersion()
	{
		if (Application.platform != RuntimePlatform.Android) { return string.Empty; }

		return plugin.Call<string>("getConsentMessageVersion");
	}

	public void UpdateCCPAStatus(Vungle.Consent consent)
	{
		if (Vungle.Consent.Undefined == consent) { return; }
		plugin.Call("updateCCPAStatus", (int)consent);
	}

	public Vungle.Consent GetCCPAStatus()
	{
		return (Vungle.Consent)(plugin.Call<int>("getCCPAStatus"));
	}
	#endregion

	#region SDKInitialization
	public void Init(string appId)
	{
		initialize(appId);
	}

	public void Init(string appId, bool initHeaderBiddingDelegate)
	{
		initialize(appId);
	}

	// Starts up the SDK with the given appId
	private void initialize(string appId)
	{
		if (Vungle.minimumDiskSpaceForInitialization.HasValue)
		{
			plugin.Call("setMinimumDiskSpaceForInit", Vungle.minimumDiskSpaceForInitialization.Value);
		}
		if (Vungle.minimumDiskSpaceForAd.HasValue)
		{
			plugin.Call("setMinimumDiskSpaceForAd", Vungle.minimumDiskSpaceForAd.Value);
		}
		if (Vungle.enableHardwareIdPrivacy.HasValue)
		{
			plugin.Call("setHardwareIdOptOut", Vungle.enableHardwareIdPrivacy.Value);
		}

		plugin.Call("init", appId, Vungle.PluginVersion);
	}

	public bool IsInitialized()
	{
		return plugin.Call<bool>("isInitialized");
	}
	#endregion

	#region AdLifeCycle
	// Checks to see if a video is available
	public bool IsAdvertAvailable(string placementId)
	{
		return plugin.Call<bool>("isVideoAvailable", placementId);
	}

	// Loads an ad
	public void LoadAd(string placementId)
	{
		plugin.Call("loadAd", placementId);
	}

	public void PlayAd(string placementId)
	{
		PlayAd(null, placementId);
	}

	public void PlayAd(Dictionary<string, object> options, string placementId)
	{
		if (options == null)
		{
			options = new Dictionary<string, object>();
		}

		plugin.Call("playAd", placementId, MiniJSONV.Json.Serialize(options));
	}

	// Close dlex ad
	public bool CloseAd(string placementId)
	{
		return plugin.Call<bool>("closeAd", placementId);
	}
	#endregion

	#region AdLifeCycle - Banner

	public bool IsAdvertAvailable(string placementId, Vungle.VungleBannerSize adSize)
	{
		return plugin.Call<bool>("isVideoAvailable", placementId, (int)adSize);
	}

	/**
	 * Load Banner of given AdSize and at given AdPosition
	 *
	 * placementId String
	 * adSize AdSize Size of the banner
	 * adPosition AdPosition Position of the Banner on screen
	*/
	public void LoadBanner(string placementId, Vungle.VungleBannerSize adSize, Vungle.VungleBannerPosition adPosition)
	{
		plugin.Call("loadBanner", placementId, (int)adSize, (int)adPosition);
	}

	/**
	 * Sets offset for banner based on the position
	 * Left and up are negative, right and down are positive
	 * x = offset on horizontal (x-axis)
	 * y = offset on vertical (y-axis)
	*/
	public void SetBannerOffset(string placementId, int x, int y)
	{
		plugin.Call("setOffset", placementId, x, y);
	}

	/**
	 * Play Banner with given placementId
	 *
	 * placementId String
	 */
	public void ShowBanner(string placementId)
	{
		ShowBanner(placementId, null);
	}

	public void ShowBanner(string placementId, Dictionary<string, object> options)
	{
		if (options == null)
		{
			plugin.Call("showBanner", placementId);
		}
		else
		{
			plugin.Call("showBanner", placementId, MiniJSONV.Json.Serialize(options));
		}
	}

	/**
	 * Close Banner with given placementId
	 *
	 * placementId String
	 */
	public void CloseBanner(string placementId)
	{
		plugin.Call("closeBanner", placementId);
	}
	#endregion

	#region PlaybackOptions

	public bool IsSoundEnabled()
	{
		return plugin.Call<bool>("isSoundEnabled");
	}

	// Sets if sound should be enabled or not
	public void SetSoundEnabled(bool isEnabled)
	{
		plugin.Call("setSoundEnabled", isEnabled);
	}

	// Sets the allowed orientations of any ads that are displayed
	public void setAdOrientation(VungleAdOrientation orientation)
	{
		plugin.Call("setAdOrientation", (int)orientation);
	}
	#endregion

	#region PauseAndResumeHandling
	// Call this when your application resumes
	public void OnResume()
	{
		plugin.Call("onResume");
	}

	// Call this when your application is sent to the background
	public void OnPause()
	{
		plugin.Call("onPause");
	}
	#endregion

	#region TestUsage
	public void SetLogEnable(bool enable)
	{
		plugin.Call("setLogEnabled", enable);
	}
	#endregion
}
#endif
