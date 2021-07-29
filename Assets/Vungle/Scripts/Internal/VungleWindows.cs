using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_WSA_10_0 || UNITY_WINRT_8_1 || UNITY_METRO
using VungleSDKProxy;

public enum VungleAdOrientation
{
	Portrait = 1,
	Landscape = 5,
	All = 6,
	AutoRotate = 6,
	MatchVideo = 6,
}

public class OptionConstants
{
	public const string UserTag = "userTag";
	public const string Orientation = "orientation";
	public const string AlertText = "alertText";
	public const string AlertTitle = "alertTitle";
	public const string CloseText = "closeText";
	public const string ContinueText = "continueText";
	public const string BackImmediately = "backImmediately";
	public const string FlexCloseSec = "flexCloseSec";
}

public partial class VungleWindows : IVungleHelper
{
	private VungleAd sdk;
	private bool isSoundEnabled = true;
	private string endpoint = "https://ads.api.vungle.com";

	public string SdkVersion
	{
		get
		{
			return AdFactory.GetSdkVersion();
		}
	}

	public string VersionInfo
	{
		get
		{
			return string.Format("VungleSDKCall-UWP/{0}-{1}", Vungle.PluginVersion, SdkVersion);
		}
	}

	#region SDKSetup
	public void RequestTrackingAuthorization() { }

	public void UpdateConsentStatus(Vungle.Consent consent, string version = "1.0")
	{
		if (Vungle.Consent.Undefined == consent) { return; }
		if (sdk != null)
		{
			sdk.UpdateConsentStatus((Vungle.Consent.Accepted == consent) ? Consent.Accepted : Consent.Denied, version);
		}
	}

	public Vungle.Consent GetConsentStatus()
	{
		return (Vungle.Consent)sdk.GetConsentStatus();
	}

	public void UpdateCCPAStatus(Vungle.Consent status)
	{
		AdFactory.UpdateCCPAStatus((int)status);
	}

	public Vungle.Consent GetCCPAStatus()
	{
		int status = AdFactory.GetCCPAStatus();
		switch (status)
		{
			case 2:
				return Vungle.Consent.Denied;
			case 1:
				return Vungle.Consent.Accepted;
			default:
				return Vungle.Consent.Undefined;
		}
	}
	#endregion

	#region SDKInitialization
	public void Init(string appId)
	{
		Initialize(appId);
	}

	public void Init(string appId, bool initHeaderBiddingDelegate)
	{
		Initialize(appId);
	}

	// Starts up the SDK with the given appId
	private void Initialize(string appId)
	{
		VungleSceneLoom.Initialize();

		VungleAd.AddInitCallback(VungleManager.Instance.OnInitialize);
		VungleAd.AddAdPlayableCallback(VungleManager.Instance.OnAdPlayable);
		VungleAd.AddAdStartCallback(VungleManager.Instance.OnAdStart);
		VungleAd.AddAdEndCallback(VungleManager.Instance.OnAdEnd);
		VungleAd.AddLogCallback(VungleManager.Instance.OnSDKLog);
		VungleAd.AddWarningCallback(VungleManager.Instance.OnWarning);
		VungleAd.AddErrorCallback(VungleManager.Instance.OnError);

		VungleSDKConfig config = new VungleSDKConfig();
		if (Vungle.minimumDiskSpaceForInitialization.HasValue)
		{
			config.SetMinimumDiskSpaceForInit(Vungle.minimumDiskSpaceForInitialization.Value);
		}
		if (Vungle.minimumDiskSpaceForAd.HasValue)
		{
			config.SetMinimumDiskSpaceForAd(Vungle.minimumDiskSpaceForAd.Value);
		}
		if (Vungle.enableHardwareIdPrivacy.HasValue)
		{
			config.SetDisableAshwidTracking(Vungle.enableHardwareIdPrivacy.Value);
		}
		config.SetPluginName("unity");
		config.SetPluginVersion(Vungle.PluginVersion);
		config.SetApiEndpoint(new Uri(endpoint));
		sdk = AdFactory.GetInstance(appId, config);
	}

	public bool IsInitialized()
	{
		return AdFactory.IsInitialized();
	}
	#endregion

	#region AdLifeCycle
	public bool IsAdvertAvailable(string placementId)
	{
		return sdk != null ? sdk.IsAdPlayable(placementId) : false;
	}

	public void LoadAd(string placementId)
	{
		InvokeSafelyOnUIThreadAsync(delegate
		{
			if (sdk != null)
			{
				sdk.LoadAd(placementId);
			}
		});
	}

	public void PlayAd(string placementId)
	{
		InvokeSafelyOnUIThreadAsync(delegate
		{
			if (sdk != null && sdk.IsAdPlayable(placementId))
			{
				AdConfig cfg = new AdConfig();
				cfg.SetUserId(string.Empty);
				cfg.SetSoundEnabled(this.isSoundEnabled);
				cfg.SetOrientation(DisplayOrientations.AutoRotate);
				sdk.PlayAd(cfg, placementId);
			}
		});
	}

	public void PlayAd(Dictionary<string, object> options, string placementId)
	{
		InvokeSafelyOnUIThreadAsync(delegate
		{
			if (sdk != null && sdk.IsAdPlayable(placementId))
			{
				if (options == null)
				{
					options = new Dictionary<string, object>();
				}

				AdConfig cfg = new AdConfig();
				SetAdConfig(cfg, options);
				if (options.ContainsKey(OptionConstants.FlexCloseSec) && options[OptionConstants.FlexCloseSec] is string)
				{
					int seconds = 0;
					if (int.TryParse((string)options[OptionConstants.FlexCloseSec], out seconds))
					{
						sdk.SetFlexViewCloseTimeInSec(placementId, seconds);
					}
					else
					{
						VungleLog.Log(VungleLog.Level.Warning, VungleLog.Context.LogEvent, "PlayAd", "FlexViewCloseTime was not an integer.");
					}
				}
				sdk.PlayAd(cfg, placementId);
			}
		});
	}

	private void SetAdConfig(AdConfig config, Dictionary<string, object> options)
	{
		config.SetSoundEnabled(this.isSoundEnabled);
		SetValue<string>(options, OptionConstants.UserTag, config.SetUserId);
		SetValue<string>(options, OptionConstants.AlertText, config.SetIncentivizedDialogBody);
		SetValue<string>(options, OptionConstants.AlertTitle, config.SetIncentivizedDialogTitle);
		SetValue<string>(options, OptionConstants.CloseText, config.SetIncentivizedDialogCloseButton);
		SetValue<string>(options, OptionConstants.ContinueText, config.SetIncentivizedDialogContinueButton);
		SetValue<bool>(options, OptionConstants.BackImmediately, config.SetBackButtonImmediatelyEnabled);

		if (options.ContainsKey(OptionConstants.Orientation))
		{
			// Legacy implementation
			// If it is true, then it means Landscape
			// If it is false, then it means AutoRotate
			if (options[OptionConstants.Orientation] is bool)
			{
				bool orientation = (bool)options[OptionConstants.Orientation];
				if (orientation)
				{
					config.SetOrientation(DisplayOrientations.Landscape);
				}
				else
				{
					config.SetOrientation(DisplayOrientations.AutoRotate);
				}
			}
			else if (options[OptionConstants.Orientation] is int)
			{
				int orientation = (int)options[OptionConstants.Orientation];
				switch (orientation)
				{
					case 1:
						config.SetOrientation(DisplayOrientations.Portrait);
						break;
					case 5:
						config.SetOrientation(DisplayOrientations.Landscape);
						break;
					default:
						config.SetOrientation(DisplayOrientations.AutoRotate);
						break;
				}
			}
			else
			{
				config.SetOrientation(DisplayOrientations.AutoRotate);
			}
		}
		else
		{
			// default to autorotate
			config.SetOrientation(DisplayOrientations.AutoRotate);
		}
	}

	private void SetValue<T>(Dictionary<string, object> options, string key, Action<T> callback)
	{
		if (options != null && !string.IsNullOrEmpty(key) &&
			options.ContainsKey(key) && options[key] is T)
		{
			if (callback != null)
			{
				callback((T)options[key]);
			}
		}
	}

	private delegate U CalculateValue<T, U>(T obj);
	private void SetValue<T, U>(Dictionary<string, object> options, string key, Action<U> callback, CalculateValue<T, U> valueCallback)
	{
		if (options != null && !string.IsNullOrEmpty(key) &&
			options.ContainsKey(key) && options[key] is T)
		{
			if (callback != null && valueCallback != null)
			{
				callback(valueCallback((T)options[key]));
			}
		}
	}

	public bool CloseAd(string placementId)
	{
		if (sdk != null)
		{
			sdk.CloseFlexViewAd(placementId);
			return true;
		}
		return false;
	}
	#endregion

	#region AdLifeCycle - Banner
	public bool IsAdvertAvailable(string placementId, Vungle.VungleBannerSize adSize)
	{
		return sdk.IsAdPlayable(placementId, (int)adSize + 1);
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
#if !UNITY_2019_1 && !UNITY_2019_2
		InvokeSafelyOnUIThreadAsync(delegate
		{
			SetBannerGrid(delegate
			{
				sdk.LoadBanner(placementId, (int)adSize + 1, (int)adPosition);
			});
		});
#else
		VungleLog.Log(VungleLog.Level.Error, VungleLog.Context.LogEvent, "VungleWindows.LoadBanner",
			"2019.1 and 2019.2 crashes when instantiating a core class for banners, but it is fixed in 2019.3");
#endif
	}

	private void SetBannerGrid(Action callback)
	{
		if (!sdk.IsParentGridSet())
		{
			VungleSceneLoom.Instance.GetSwapChainPanel((object obj) =>
			{
				sdk.SetParentGrid(obj);
				if (callback != null)
				{
					callback();
				}
			});
		}
		else
		{
			if (callback != null)
			{
				callback();
			}
		}
	}

	public void SetBannerOffset(string placementID, int x, int y) 
	{
		InvokeSafelyOnUIThreadAsync(delegate
		{
			sdk.SetOffSet(placementID, x, y);
		});
	}

	/**
	 * Play Banner with given placementId
	 *
	 * placementId String
     */
	public void ShowBanner(string placementId)
	{
#if !UNITY_2019_1 && !UNITY_2019_2
		InvokeSafelyOnUIThreadAsync(delegate
		{
			SetBannerGrid(delegate
			{
				sdk.ShowBanner(placementId);
			});
		});
#else
		VungleLog.Log(VungleLog.Level.Error, VungleLog.Context.LogEvent, "VungleWindows.ShowBanner",
			"2019.1 and 2019.2 crashes when instantiating a core class for banners, but it is fixed in 2019.3");
#endif
	}

	/**
	 * Close Banner with given placementId
	 *
	 * placementId String
	 */
	public void CloseBanner(string placementId)
	{
#if !UNITY_2019_1 && !UNITY_2019_2
		InvokeSafelyOnUIThreadAsync(delegate
		{
			sdk.CloseBanner(placementId);
		});
#else
		VungleLog.Log(VungleLog.Level.Error, VungleLog.Context.LogEvent, "VungleWindows.CloseBanner",
			"2019.1 and 2019.2 crashes when instantiating a core class for banners, but it is fixed in 2019.3");
#endif
	}
	#endregion

	#region PlaybackOptions

	public bool IsSoundEnabled()
	{
		return this.isSoundEnabled;
	}

	public void SetSoundEnabled(bool isEnabled)
	{
		this.isSoundEnabled = isEnabled;
	}

	#endregion

	#region PauseAndResumeHandling
	public void OnResume()
	{
		return;
	}

	public void OnPause()
	{
		return;
	}
	#endregion

	#region TestUsage
	public void SetLogEnable(bool enable)
	{
		sdk.SetLogEnabled(enable);
	}
	#endregion

	private void InvokeSafelyOnUIThreadAsync(Action action)
	{
		UnityEngine.WSA.Application.InvokeOnUIThread(() =>
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				VungleLog.Log(VungleLog.Level.Error, VungleLog.Context.LogEvent, "InvokeSafelyOnUIThreadAsync",
					string.Format("Failed to perform action on UI thread:\n{0}", e.ToString()));
			}
		}, false);
	}
}
#endif
