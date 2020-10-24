using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 618

public class VungleManager : MonoBehaviour
{
	private static AdFinishedEventArgs adWinFinishedEventArgs = null;

	#region Constructor and Lifecycle

	static VungleManager()
	{
		// try/catch this so that we can warn users if they try to stick this script on a GO manually
		try
		{
			// create a new GO for our manager
			var go = new GameObject("VungleManager");
			go.AddComponent<VungleManager>();
			DontDestroyOnLoad(go);
		}
		catch
		{
			VungleLog.Log(VungleLog.Level.Error, VungleLog.Context.SDKInitialization, "VungleManager",
				"Vungle.cs will create the VungleManager instance. A Vungle manager already exists in your scene. Please remove the script from the scene.");
		}
	}

	// used to ensure the VungleManager will always be in the scene to avoid SendMessage logs if the user isn't using any events
	public static void noop() { }

	#endregion

	// Fired when the video is shown
	public static event Action<string> OnAdStartEvent;

	// Fired when a Vungle ad is ready to be displayed
	public static event Action<string, bool> OnAdPlayableEvent;

	// Fired when a Vungle write log (implemented only for iOS)
	public static event Action<string> OnSDKLogEvent;

	// Fired when a Vungle Placement Prepared (implemented only for iOS)
	public static event Action<string, string> OnPlacementPreparedEvent;

	// Fired when a Vungle Creative fired (implemented only for iOS)
	public static event Action<string, string> OnVungleCreativeEvent;

	//Fired when a Vungle ad finished and provides the entire information about this event.
	public static event Action<string, AdFinishedEventArgs> OnAdFinishedEvent;

	// Fired when a Vungle SDK initialized
	public static event Action OnSDKInitializeEvent;

	// Fired when the error is thrown
	public static event Action<string> OnErrorEvent;

	// Fired when the warning is thrown
	public static event Action<string> OnWarningEvent;

	// Windows SDK calls this function
	public static void OnEvent(string sdkEvent, string arg)
	{
		switch (sdkEvent)
		{
			case "OnAdStart":
				// Placement
				if (OnAdStartEvent != null)
				{
					OnAdStartEvent(arg);
				}
				break;
			case "OnAdEnd":
				// CallToActionClicked:Placement:IsCompletedView:WatchedDuration(Milliseconds)
				if (OnAdFinishedEvent != null)
				{
					adWinFinishedEventArgs = new AdFinishedEventArgs();
					var args = arg.Split(new char[] { ':' });
					adWinFinishedEventArgs.WasCallToActionClicked = "1".Equals(args[0]);
					adWinFinishedEventArgs.IsCompletedView = bool.Parse(args[2]);
					adWinFinishedEventArgs.TimeWatched = double.Parse(args[3]) / 1000;
					OnAdFinishedEvent(args[1], adWinFinishedEventArgs);
				}
				break;
			case "OnAdPlayableChanged":
				// Playable(int):Placement
				if (OnAdPlayableEvent != null)
				{
					var args1 = arg.Split(new char[] { ':' });
					OnAdPlayableEvent(args1[1], "1".Equals(args1[0]));
				}
				break;
			case "Diagnostic":
				// LogMessage
				if (OnSDKLogEvent != null)
				{
					OnSDKLogEvent(arg);
				}
				break;
			case "OnInitCompleted":
				// InitializeSuccess(int)
				if (OnSDKInitializeEvent != null && "1".Equals(arg))
				{
					OnSDKInitializeEvent();
				}
				break;
			case "OnError":
				if (OnErrorEvent != null)
				{
					OnErrorEvent(arg);
				}
				break;
			case "OnWarning":
				if (OnWarningEvent != null)
				{
					OnWarningEvent(arg);
				}
				break;
			default:
				VungleLog.Log(VungleLog.Level.Error, VungleLog.Context.LogEvent, "OnEvent",
					string.Format("nhandled SDK Event: {0}", sdkEvent));
				break;

		}
	}

	#region Native code will call these methods

	//methods for ios and andriod platforms
	void OnAdStart(string placementID)
	{
		if (OnAdStartEvent != null)
		{
			OnAdStartEvent(placementID);
		}
	}

	void OnAdPlayable(string param)
	{
		if (OnAdPlayableEvent != null)
		{
			Dictionary<string, object> attrs = (Dictionary<string, object>)MiniJSONV.Json.Deserialize(param);
			bool isAdAvailable = ExtractBoolValue(attrs, "isAdAvailable");
			string placementID = attrs["placementID"].ToString();
			OnAdPlayableEvent(placementID, isAdAvailable);
		}
	}

	void OnSDKLog(string log)
	{
		if (OnSDKLogEvent != null)
		{
			OnSDKLogEvent(log);
		}
	}

	void OnPlacementPrepared(string param)
	{
		if (OnPlacementPreparedEvent != null)
		{
			Dictionary<string, object> attrs = (Dictionary<string, object>)MiniJSONV.Json.Deserialize(param);
			string placementID = ExtractStringValue(attrs, "placementID");
			string bidToken = ExtractStringValue(attrs, "bidToken");
			OnPlacementPreparedEvent(placementID, bidToken);
		}
	}

	void OnVungleCreative(string param)
	{
		if (OnVungleCreativeEvent != null)
		{
			Dictionary<string, object> attrs = (Dictionary<string, object>)MiniJSONV.Json.Deserialize(param);
			string placementID = ExtractStringValue(attrs, "placementID");
			string creativeID = ExtractStringValue(attrs, "creativeID");
			OnVungleCreativeEvent(placementID, creativeID);
		}
	}

	void OnInitialize(string arg)
	{
		if (OnSDKInitializeEvent != null && "1".Equals(arg))
		{
			OnSDKInitializeEvent();
		}
	}

	//methods only for android
	void OnAdEnd(string param)
	{
		if (OnAdFinishedEvent != null)
		{
			AdFinishedEventArgs args = new AdFinishedEventArgs();
			Dictionary<string, object> attrs = (Dictionary<string, object>)MiniJSONV.Json.Deserialize(param);
#if UNITY_ANDROID
			args.WasCallToActionClicked = ExtractBoolValue(attrs, "wasCallToActionClicked");
			args.IsCompletedView = ExtractBoolValue(attrs, "wasSuccessfulView");
			args.TimeWatched = 0.0;
#elif UNITY_IOS
			//param is the json string
			args.WasCallToActionClicked = ExtractBoolValue(attrs, "didDownload");
			args.IsCompletedView = ExtractBoolValue(attrs, "completedView");
			args.TimeWatched = double.Parse(attrs["playTime"].ToString());
#endif
			OnAdFinishedEvent(ExtractStringValue(attrs, "placementID"), args);
		}
	}

	void TrackingCallback(string trackingValue) {
		int res = 0;
		if (!Int32.TryParse(trackingValue, out res))
		{
			VungleLog.Log(VungleLog.Level.Error, "App Tracking Callback",
				"TrackingCallback", "Failed to get a valid return value from the App Tracking Transparency callback.");
			return;
		}

		VungleLog.Log(VungleLog.Level.Debug, "App Tracking Callback",
			"TrackingCallback", string.Format("The result of the tracking callback {0}", trackingValue));

		if (Vungle.onAppTrackingEvent == null)
		{
			return;
		}

		// 0 - Not Determined
		// 1 - Restricted
		// 2 - Denied
		// 3 - Authorized
		switch (res)
		{
			case 1:
				// Access to app-related data for tracking is restricted
				// User cannot change the setting
				Vungle.onAppTrackingEvent(Vungle.AppTrackingStatus.RESTRICTED);
				break;
			case 2:
				// The user denied access to app-related data for tracking
				// The user needs to go to settings to allow tracking
				Vungle.onAppTrackingEvent(Vungle.AppTrackingStatus.DENIED);
				break;
			case 3:
				// The user authorized access to app-related data for tracking
				Vungle.onAppTrackingEvent(Vungle.AppTrackingStatus.AUTHORIZED);
				break;
			case 0:
			default:
				// Tracking dialog has not been shown
				Vungle.onAppTrackingEvent(Vungle.AppTrackingStatus.NOT_DETERMINED);
				break;
		}
	}

	void OnError(string message)
	{
		if (OnErrorEvent != null)
		{
			OnErrorEvent(message);
		}
	}

	void OnWarning(string message)
	{
		if (OnWarningEvent != null)
		{
			OnWarningEvent(message);
		}
	}
	#endregion

	#region util methods
	private bool ExtractBoolValue(Dictionary<string, object> attrs, string key)
	{
		object val = null;
		if (attrs.TryGetValue(key, out val))
		{
			return bool.Parse(val.ToString());
		}
		return false;
	}

	private string ExtractStringValue(Dictionary<string, object> attrs, string key)
	{
		object val = null;
		if (attrs.TryGetValue(key, out val))
		{
			return val.ToString();
		}
		return string.Empty;
	}
	#endregion

	void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			Vungle.onPause();
		}
		else
		{
			Vungle.onResume();
		}
	}
}
