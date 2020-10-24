using System;
using System.Collections.Generic;

public static partial class Vungle
{
	public const string PluginVersion = "6.8.1.0";

	public enum Consent
	{
		Undefined = 0,
		Accepted = 1,
		Denied = 2,
	}

	public enum VungleBannerPosition
	{
		TopLeft = 0,
		TopCenter,
		TopRight,
		Centered,
		BottomLeft,
		BottomCenter,
		BottomRight,
	}

	public enum VungleBannerSize
	{
		VungleAdSizeBanner = 0,         // width = 320.0f, .height = 50.0f
		VungleAdSizeBannerShort,        // width = 300.0f, .height = 50.0f
		VungleAdSizeBannerMedium,       // width = 300.0f, .height = 250.0f
		VungleAdSizeBannerLeaderboard,  // width = 728.0f, .height = 90.0f
	}

	public enum AppTrackingStatus
	{
		NOT_DETERMINED = 0,
		RESTRICTED = 1,
		DENIED = 2,
		AUTHORIZED = 3,
	}

	static IVungleHelper helper;

	// Fired when a Vungle SDK initialized
	public static Action onInitializeEvent;
	// Fired when a Vungle ad is ready to be displayed
	public static Action<string, bool> adPlayableEvent;
	// Fired when a Vungle ad starts
	public static Action<string> onAdStartedEvent;
	// Fired when a Vungle ad finished and provides the entire information about this event.
	public static Action<string, AdFinishedEventArgs> onAdFinishedEvent;
	// Fired log event from sdk.
	public static Action<string> onLogEvent;
	public static Action<string, string> onPlacementPreparedEvent;
	public static Action<string, string> onVungleCreativeEvent;
	// Fired when the banner error is thrown
	public static Action<string> onErrorEvent;
	// Fired when the warning is thrown
	public static Action<string> onWarningEvent;
	// Fired when the App Tracking callback is fired
	public static Action<AppTrackingStatus> onAppTrackingEvent;

	public static long? minimumDiskSpaceForInitialization;
	public static long? minimumDiskSpaceForAd;
	public static bool? enableHardwareIdPrivacy;

	public static VungleLog.Level logLevel { get; set; }

	static Vungle()
	{
#if UNITY_EDITOR
		helper = new VungleUnityEditor();
#elif UNITY_IOS
		helper = new VungleiOS();
#elif UNITY_ANDROID
		helper = new VungleAndroid();
#elif UNITY_WSA_10_0 || UNITY_WINRT_8_1 || UNITY_METRO
		helper = new VungleWindows();
#endif

		logLevel = VungleLog.Level.Debug;

		VungleManager.noop();
		VungleManager.OnSDKInitializeEvent += OnInitialize;
		VungleManager.OnAdPlayableEvent += AdPlayable;
		VungleManager.OnAdStartEvent += AdStarted;
		VungleManager.OnAdFinishedEvent += AdFinished;
		VungleManager.OnSDKLogEvent += OnLog;
		VungleManager.OnPlacementPreparedEvent += OnPlacementPrepared;
		VungleManager.OnVungleCreativeEvent += OnVungleCreative;
		VungleManager.OnErrorEvent += OnError;
		VungleManager.OnWarningEvent += OnWarning;
	}

	#region EventHandling
	private static void OnInitialize()
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.SDKInitialization, "SendOnInitializeEvent", "SDK is initialized");
		if (onInitializeEvent != null)
		{
#if UNITY_WSA_10_0 || UNITY_WINRT_8_1 || UNITY_METRO
			VungleSceneLoom.Loom.QueueOnMainThread(() =>
				{
					onInitializeEvent();
				});
#else
			onInitializeEvent();
#endif
		}
	}

	private static void AdPlayable(string placementId, bool playable)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.AdLifecycle, "SendAdPlayableEvent",
			string.Format("Placement {0} - Playable status {1}", placementId, playable.ToString()));
		if (adPlayableEvent != null)
		{
#if UNITY_WSA_10_0 || UNITY_WINRT_8_1 || UNITY_METRO
			VungleSceneLoom.Loom.QueueOnMainThread(() =>
				{
					adPlayableEvent(placementId, playable);
				});
#else
			adPlayableEvent(placementId, playable);
#endif
		}
	}

	private static void AdStarted(string placementId)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.AdLifecycle, "SendOnAdStartedEvent",
			string.Format("An ad started displaying for {0}", placementId));
		if (onAdStartedEvent != null)
		{
#if UNITY_WSA_10_0 || UNITY_WINRT_8_1 || UNITY_METRO
			VungleSceneLoom.Loom.QueueOnMainThread(() =>
				{
					onAdStartedEvent(placementId);
				});
#else
			onAdStartedEvent(placementId);
#endif
		}
	}

	private static void AdFinished(string placementId, AdFinishedEventArgs args)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.AdLifecycle, "SendOnAdFinishedEvent",
			string.Format("An ad finished displaying for {0}", placementId));
		if (onAdFinishedEvent != null)
		{
#if UNITY_WSA_10_0 || UNITY_WINRT_8_1 || UNITY_METRO
			VungleSceneLoom.Loom.QueueOnMainThread(() =>
				{
					onAdFinishedEvent(placementId, args);
				});
#else
			onAdFinishedEvent(placementId, args);
#endif
		}
	}

	private static void OnLog(string log)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.LogEvent, "SendOnLogEvent", "An log event. Log message is' " + log + "'.");
		if (onLogEvent != null)
		{
#if UNITY_WSA_10_0 || UNITY_WINRT_8_1 || UNITY_METRO
			VungleSceneLoom.Loom.QueueOnMainThread(() =>
				{
					onLogEvent(log);
				});
#else
			onLogEvent(log);
#endif
		}
	}

	private static void OnPlacementPrepared(string placementId, string bidToken)
	{
		if (onPlacementPreparedEvent != null)
		{
			onPlacementPreparedEvent(placementId, bidToken);
		}
	}

	private static void OnVungleCreative(string placementId, string creativeID)
	{
		if (onVungleCreativeEvent != null)
		{
			onVungleCreativeEvent(placementId, creativeID);
		}
	}

	private static void OnError(string error)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.LogEvent, "SendOnErrorEvent",
			"Vungle.onWarning => From Unity:"  + error);
		if (onErrorEvent != null)
		{
			onErrorEvent(error);
		}
	}

	private static void OnWarning(string warning)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.LogEvent, "SendOnWarningEvent",
			"Vungle.onWarning => From Unity:" + warning);
		if (onWarningEvent != null)
		{
			onWarningEvent(warning);
		}
	}
	#endregion

	#region SDKSetup
	public static void RequestTrackingAuthorization()
	{
		try
		{
			helper.RequestTrackingAuthorization();
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "RequestTrackingAuthorization", "Vungle.RequestTrackingAuthorization", e.Message);
		}
	}


	public static void SetMinimumDiskSpaceForInitialization(long minimumDiskSpace)
	{
		minimumDiskSpaceForInitialization = minimumDiskSpace;
	}

	public static void SetMinimumDiskSpaceForAd(long minimumDiskSpace)
	{
		minimumDiskSpaceForAd = minimumDiskSpace;
	}


	public static void EnableHardwareIdPrivacy(bool dontSendHardwareId)
	{
		enableHardwareIdPrivacy = dontSendHardwareId;
	}

	public static void updateConsentStatus(Consent consent, string version = "1.0")
	{
		try
		{
			helper.UpdateConsentStatus(consent, version);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "updateConsentStatus", "Vungle.updateConsentStatus", e.Message);
		}
	}

	public static Consent getConsentStatus()
	{
		try
		{
			return helper.GetConsentStatus();
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "getConsentStatus", "Vungle.getConsentStatus", e.Message);
			return Consent.Undefined;
		}
	}

	public static void updateCCPAStatus(Consent consent)
	{
		try
		{
			helper.UpdateCCPAStatus(consent);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "updateCCPAStatus", "Vungle.updateCCPAStatus", e.Message);
		}
	}

	public static Consent getCCPAStatus()
	{
		try
		{
			return helper.GetCCPAStatus();
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "getCCPAStatus", "Vungle.getCCPAStatus", e.Message);
			return Consent.Undefined;
		}
	}
	#endregion

	#region SDKInitialization
	public static void init(string appId)
	{
		try
		{
			helper.Init(appId);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "init", "Vungle.init", e.Message);
		}
	}

	public static void init(string appId, bool initHeaderBiddingDelegate)
	{
		try
		{
			helper.Init(appId, initHeaderBiddingDelegate);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "init", "Vungle.init", e.Message);
		}
	}
	#endregion

	#region AdLifeCycle
	public static bool isAdvertAvailable(string placementId)
	{
		try
		{
			return helper.IsAdvertAvailable(placementId);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "isAdvertAvailable", "Vungle.isAdvertAvailable", e.Message);
			return false;
		}
	}

	public static void loadAd(string placementId)
	{
		try
		{
			helper.LoadAd(placementId);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "loadAd", "Vungle.loadAd", e.Message);
		}
	}

	public static void playAd(string placementId)
	{
		try
		{
			helper.PlayAd(placementId);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "playAd", "Vungle.playAd", e.Message);
		}
	}

	public static void playAd(Dictionary<string, object> options, string placementId)
	{
		if (options == null)
		{
			options = new Dictionary<string, object>();
		}

		try
		{
			helper.PlayAd(options, placementId);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "playAd", "Vungle.playAd", e.Message);
		}
	}

	public static bool closeAd(string placementId)
	{
		try
		{
			return helper.CloseAd(placementId);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "closeAd", "Vungle.closeAd", e.Message);
			return false;
		}
	}
	#endregion

	#region AdLifeCycle - Banner
	public static bool isAdvertAvailable(string placementId, VungleBannerSize adSize)
	{
		try
		{
			return helper.IsAdvertAvailable(placementId, adSize);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "isAdvertAvailable", "Vungle.isAdvertAvailable", e.Message);
			return false;
		}
	}

	/**
	 * Load Banner of given AdSize and at given AdPosition
	 *
	 * placementId String
	 * adSize AdSize Size of the banner
	 * adPosition AdPosition Position of the Banner on screen
	*/
	public static void loadBanner(string placementId, VungleBannerSize adSize, VungleBannerPosition adPosition)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.LogEvent, "Vungle.loadBanner",
			GetLogMessage("loading Banner", placementId));
		try
		{
			helper.LoadBanner(placementId, adSize, adPosition);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "loadBanner", "Vungle.loadBanner", e.Message);
		}
	}

	public static void showBanner(string placementId)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.LogEvent, "Vungle.showBanner",
			GetLogMessage("playing Banner", placementId));
		try
		{
			helper.ShowBanner(placementId);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "showBanner", "Vungle.showBanner", e.Message);
		}
	}

	public static void closeBanner(string placementId)
	{
		VungleLog.Log(VungleLog.Level.Debug, VungleLog.Context.LogEvent, "Vungle.closeBanner",
			GetLogMessage("closing Banner", placementId));
		try
		{
			helper.CloseBanner(placementId);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "closeBanner", "Vungle.closeBanner", e.Message);
		}
	}

	private static string GetLogMessage(string context, string placementId)
	{
		return string.Format("{0} {1}: {2} for placement ID: {3}", helper.VersionInfo, DateTime.Today.ToString(), context, placementId);
	}
	#endregion

	#region PlaybackOptions
	public static void setSoundEnabled(bool isEnabled)
	{
		try
		{
			helper.SetSoundEnabled(isEnabled);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "setSoundEnabled", "Vungle.setSoundEnabled", e.Message);
		}
	}
	#endregion

	#region PauseAndResumeHandling
	public static void onResume()
	{
		try
		{
			helper.OnResume();
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "onResume", "Vungle.onResume", e.Message);
		}
	}

	public static void onPause()
	{
		try
		{
			helper.OnPause();
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "onPause", "Vungle.onPause", e.Message);
		}
	}
	#endregion

	#region TestUsage
	public static void setLogEnable(bool enable)
	{
		try
		{
			helper.SetLogEnable(enable);
		}
		catch (Exception e)
		{
			VungleLog.Log(VungleLog.Level.Error, "setLogEnable", "Vungle.setLogEnable", e.Message);
		}
	}
	#endregion
}
