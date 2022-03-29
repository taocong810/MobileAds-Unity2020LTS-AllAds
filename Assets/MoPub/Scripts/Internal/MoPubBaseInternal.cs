using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Internal functionality used by the MoPub API; publishers should not call these methods directly and use the
/// <see cref="MoPub"/> class instead.
/// </summary>
public class MoPubBaseInternal
{
    public static MoPubBase.LogLevel CachedLogLevel;

    /// <summary>
    /// Set this to an ISO language code (e.g., "en-US") if you wish the next two URL properties to point
    /// to a web resource that is localized to a specific language.
    /// </summary>
    public static string ConsentLanguageCode { get; set; }

    public const double LatLongSentinel = 99999.0;

    private static bool _validInit;

    protected static void LoadPluginsForAdUnits(string[] adUnitIds, string adType = null)
    {
        AdUnitManager.InitAdUnits(adUnitIds, adType);
    }


    protected static void ValidateAdUnitForSdkInit(string adUnitId)
    {
        _validInit = !string.IsNullOrEmpty(adUnitId);
        ValidateInit("FATAL ERROR: A valid ad unit ID is needed to initialize the MoPub SDK.\n" +
                     "Please enter an ad unit ID from your app into the MoPubManager GameObject.");
    }


    protected static void ValidateInit(string message = null)
    {
        if (_validInit) return;

        message = message ?? "FATAL ERROR: MoPub SDK has not been successfully initialized.";
        Debug.LogError(message);
        throw new Exception("0xDEADDEAD");
    }


    private static void ReportAdUnitNotFound(string adUnitId)
    {
        Debug.LogWarning(string.Format("AdUnit {0} not found: no plugin was initialized", adUnitId));
    }


    static MoPubBaseInternal()
    {
        InitManager();
    }


    // Allocate the MoPubManager singleton, which receives all callback events from the native SDKs.
    // This is done in case the app is not using the new MoPubManager prefab, for backwards compatibility.
    private static void InitManager()
    {
        if (MoPubManager.Instance == null)
            new GameObject("MoPubManager", typeof(MoPubManager));
    }


    protected static class AdUnitManager
    {
        private static readonly Dictionary<string, MoPubAdUnit> AdUnits = new Dictionary<string, MoPubAdUnit>();

        public static void InitAdUnits(string[] adUnitIds, string adType)
        {
            foreach (var adUnitId in adUnitIds) {
                if (!AdUnits.ContainsKey(adUnitId)) {
                    AdUnits[adUnitId] = MoPubAdUnit.CreateMoPubAdUnit(adUnitId, adType);
                    MoPubLog.Log("InitAdUnits", "Initialized {0} AdUnit with id {1}", adType, adUnitId);
                }
                else {
                    MoPubLog.Log("InitAdUnits", "WARNING: Skipping {0} AdUnit with id {1} since it was already initialized",
                        adType, adUnitId);
                }
            }
        }

        public static void InitAdUnits(string adType, params string[] adUnitIds)
        {
            InitAdUnits(adUnitIds, adType);
        }

        public static MoPubAdUnit GetAdUnit(string adUnitId)
        {
            ValidateInit();
            MoPubAdUnit adUnit;
            if (AdUnits.TryGetValue(adUnitId, out adUnit))
                return adUnit;

            ReportAdUnitNotFound(adUnitId);
            return MoPubAdUnit.NullMoPubAdUnit;
        }
    }
}
