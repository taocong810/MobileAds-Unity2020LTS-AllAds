using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

using MJ = MoPubInternal.ThirdParty.MiniJSON;

/// <summary>
/// Support classes used by the <see cref="MoPub"/> Unity API for publishers.
/// </summary>
public abstract class MoPubBase : MoPubBaseInternal
{
    public enum AdPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        Centered,
        BottomLeft,
        BottomCenter,
        BottomRight
    }


    public static class Consent
    {
        /// <summary>
        /// User's consent for providing personal tracking data for ad tailoring.
        /// </summary>
        /// <remarks>
        /// The enum values match the iOS SDK enum.
        /// </remarks>
        public enum Status
        {
            /// <summary>
            /// Status is unknown. Either the status is currently updating or the SDK initialization has not completed.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Consent is denied.
            /// </summary>
            Denied,

            /// <summary>
            /// Advertiser tracking is disabled.
            /// </summary>
            DoNotTrack,

            /// <summary>
            /// Your app has attempted to grant consent on the user's behalf, but your whitelist status is not verfied
            /// with the ad server.
            /// </summary>
            PotentialWhitelist,

            /// <summary>
            /// User has consented.
            /// </summary>
            Consented
        }


        // The Android SDK uses these strings to indicate consent status.
        private static class Strings
        {
            public const string ExplicitYes = "explicit_yes";
            public const string ExplicitNo = "explicit_no";
            public const string Unknown = "unknown";
            public const string PotentialWhitelist = "potential_whitelist";
            public const string Dnt = "dnt";
        }


        // Helper string to convert Android SDK consent status strings to our consent enum.
        // Also handles integer values.
        public static Status FromString(string status)
        {
            switch (status) {
                case Strings.ExplicitYes:
                    return Status.Consented;
                case Strings.ExplicitNo:
                    return Status.Denied;
                case Strings.Dnt:
                    return Status.DoNotTrack;
                case Strings.PotentialWhitelist:
                    return Status.PotentialWhitelist;
                case Strings.Unknown:
                    return Status.Unknown;
                default:
                    try {
                        return (Status) Enum.Parse(typeof(Status), status);
                    }
                    catch {
                        Debug.LogError("Unknown consent status string: " + status);
                        return Status.Unknown;
                    }
            }
        }
    }


    /// <summary>
    /// The maximum size, in density-independent pixels (DIPs), an ad should have.
    /// </summary>
    public enum MaxAdSize
    {
        Width300Height50,
        Width300Height250,
        Width320Height50,
        Width336Height280,
        Width728Height90,
        Width970Height90,
        Width970Height250,
        ScreenWidthHeight50,
        ScreenWidthHeight90,
        ScreenWidthHeight250,
        ScreenWidthHeight280
    }


    public enum LogLevel
    {
        Debug = 20,
        Info = 30,
        None = 70
    }


    /// <summary>
    /// Data object holding any SDK initialization parameters.
    /// </summary>
    public class SdkConfiguration
    {
        /// <summary>
        /// Any ad unit that your app uses.
        /// </summary>
        public string AdUnitId;

        /// <summary>
        /// Used for rewarded video initialization. This holds each custom event's unique settings.
        /// </summary>
        public MediatedNetwork[] MediatedNetworks;

        /// <summary>
        /// Allow supported SDK networks to collect user information on the basis of legitimate interest.
        /// Can also be set via MoPub.<see cref="MoPub.SdkConfiguration"/> on
        /// MoPub.<see cref="MoPubUnityEditor.InitializeSdk(MoPub.SdkConfiguration)"/>
        /// </summary>
        public bool AllowLegitimateInterest;

        /// <summary>
        /// MoPub SDK log level. Defaults to MoPub.<see cref="MoPub.LogLevel.None"/>
        /// </summary>
        public LogLevel LogLevel
        {
            get { return _logLevel != 0 ? _logLevel : LogLevel.None; }
            set { _logLevel = value; }
        }

        private LogLevel _logLevel;


        public string AdditionalNetworksString
        {
            get {
                var cn = from n in MediatedNetworks ?? Enumerable.Empty<MediatedNetwork>()
                         where n is MediatedNetwork && !(n is SupportedNetwork)
                         where !String.IsNullOrEmpty(n.AdapterConfigurationClassName)
                         select n.AdapterConfigurationClassName;
                return String.Join(",", cn.ToArray());
            }
        }


        public string NetworkConfigurationsJson
        {
            get {
                var nc = from n in MediatedNetworks ?? Enumerable.Empty<MediatedNetwork>()
                         where n.NetworkConfiguration != null
                         where !String.IsNullOrEmpty(n.AdapterConfigurationClassName)
                         select n;
                return MJ.Json.Serialize(nc.ToDictionary(n => n.AdapterConfigurationClassName,
                                                      n => n.NetworkConfiguration));
            }
        }

        public string MediationSettingsJson
        {
            get {
                var ms = from n in MediatedNetworks ?? Enumerable.Empty<MediatedNetwork>()
                         where n.MediationSettings != null
                         where !String.IsNullOrEmpty(n.MediationSettingsClassName)
                         select n;
                return MJ.Json.Serialize(ms.ToDictionary(n => n.MediationSettingsClassName,
                                                      n => n.MediationSettings));
            }
        }


        public string MoPubRequestOptionsJson
        {
            get {
                var ro = from n in MediatedNetworks ?? Enumerable.Empty<MediatedNetwork>()
                         where n.MoPubRequestOptions != null
                         where !String.IsNullOrEmpty(n.AdapterConfigurationClassName)
                         select n;
                return MJ.Json.Serialize(ro.ToDictionary(n => n.AdapterConfigurationClassName,
                                                      n => n.MoPubRequestOptions));
            }
        }


        // Allow looking up an entry in the MediatedNetwork array using the network name, which is presumed to be
        // part of the AdapterConfigurationClassName value.
        public MediatedNetwork this[string networkName]
        {
            get {
                return MediatedNetworks.FirstOrDefault(mn =>
                    mn.AdapterConfigurationClassName == networkName ||
                    mn.AdapterConfigurationClassName == networkName + "AdapterConfiguration" ||
                    mn.AdapterConfigurationClassName.EndsWith("." + networkName) ||
                    mn.AdapterConfigurationClassName.EndsWith("." + networkName + "AdapterConfiguration"));
            }
        }
    }


    public class LocalMediationSetting : Dictionary<string, object>
    {
        public string MediationSettingsClassName { get; set; }

        public LocalMediationSetting() { }

        public LocalMediationSetting(string adVendor)
        {
#if UNITY_IOS
            MediationSettingsClassName = adVendor + "InstanceMediationSettings";
#else
            MediationSettingsClassName = "com.mopub.mobileads." + adVendor + "RewardedVideo$" + adVendor + "MediationSettings";
#endif
        }

        public LocalMediationSetting(string android, string ios) :
#if UNITY_IOS
            this(ios)
#else
            this(android)
#endif
            {}


        public static string ToJson(IEnumerable<LocalMediationSetting> localMediationSettings)
        {
            var ms = from n in localMediationSettings ?? Enumerable.Empty<LocalMediationSetting>()
                     where n != null && !String.IsNullOrEmpty(n.MediationSettingsClassName)
                     select n;
            return MJ.Json.Serialize(ms.ToDictionary(n => n.MediationSettingsClassName, n => n));
        }


        // Shortcut class names so you don't have to remember the right ad vendor string (also to not misspell it).
        public class AdColony : LocalMediationSetting { public AdColony() : base("AdColony") {
#if UNITY_ANDROID
                MediationSettingsClassName = "com.mopub.mobileads.AdColonyRewardedVideo$AdColonyInstanceMediationSettings";
#endif
            }
        }
        public class AdMob      : LocalMediationSetting { public AdMob()      : base(android: "GooglePlayServices",
                                                                                     ios:     "MPGoogle") { } }
        public class Chartboost : LocalMediationSetting { public Chartboost() : base("Chartboost") { } }
        public class Vungle     : LocalMediationSetting { public Vungle()     : base("Vungle") { } }
    }


    // Networks that are supported by MoPub.
    public class SupportedNetwork : MediatedNetwork
    {
        protected SupportedNetwork(string adVendor)
        {
#if UNITY_IOS
            AdapterConfigurationClassName = adVendor + "AdapterConfiguration";
            MediationSettingsClassName    = adVendor + "GlobalMediationSettings";
#else
            AdapterConfigurationClassName = "com.mopub.mobileads." + adVendor + "AdapterConfiguration";
            MediationSettingsClassName    = "com.mopub.mobileads." + adVendor + "RewardedVideo$" + adVendor + "MediationSettings";
#endif
        }

        public class AdColony   : SupportedNetwork { public AdColony()   : base("AdColony") {
#if UNITY_ANDROID
               MediationSettingsClassName = "com.mopub.mobileads.AdColonyRewardedVideo$AdColonyGlobalMediationSettings";
#endif
            }
        }
        public class AdMob      : SupportedNetwork { public AdMob()      : base("GooglePlayServices") {
#if UNITY_IOS
               AdapterConfigurationClassName = "GoogleAdMobAdapterConfiguration";
               MediationSettingsClassName    = "MPGoogleGlobalMediationSettings";
#endif
            }
        }
        public class AppLovin   : SupportedNetwork { public AppLovin()   : base("AppLovin") { } }
        public class Chartboost : SupportedNetwork { public Chartboost() : base("Chartboost") { } }
        public class Facebook   : SupportedNetwork { public Facebook()   : base("Facebook") { } }
        public class Fyber      : SupportedNetwork { public Fyber()      : base("Fyber") { } }
        public class Flurry     : SupportedNetwork { public Flurry()     : base("Flurry") { } }
        public class InMobi     : SupportedNetwork { public InMobi()     : base("InMobi") { } }
        public class IronSource : SupportedNetwork { public IronSource() : base("IronSource") { } }
        public class Mintegral  : SupportedNetwork { public Mintegral()  : base("Mintegral") { } }
        public class Ogury      : SupportedNetwork { public Ogury()      : base("Ogury") { } }
        public class Pangle     : SupportedNetwork { public Pangle()     : base("Pangle") { } }
        public class Snap       : SupportedNetwork { public Snap()       : base("SnapAd") { } }
        public class Tapjoy     : SupportedNetwork { public Tapjoy()     : base("Tapjoy") { } }
        public class Unity      : SupportedNetwork { public Unity()      : base("UnityAds") { } }
        public class Verizon    : SupportedNetwork { public Verizon()    : base("Verizon") { } }
        public class Vungle     : SupportedNetwork { public Vungle()     : base("Vungle") { } }
    }


    public struct Reward
    {
        public string Label;
        public int Amount;


        public override string ToString()
        {
            return String.Format("\"{0} {1}\"", Amount, Label);
        }


        public bool IsValid()
        {
            return !String.IsNullOrEmpty(Label) && Amount > 0;
        }
    }


    public struct ImpressionData
    {
        public string AppVersion;
        public string AdUnitId;
        public string AdUnitName;
        public string AdUnitFormat;
        public string ImpressionId;
        public string Currency;
        public double? PublisherRevenue;
        public string AdGroupId;
        public string AdGroupName;
        public string AdGroupType;
        public int? AdGroupPriority;
        public string Country;
        public string Precision;
        public string NetworkName;
        public string NetworkPlacementId;
        public string JsonRepresentation;

        public static ImpressionData FromJson(string json)
        {
            var impData = new ImpressionData();
            if (string.IsNullOrEmpty(json)) return impData;

            var fields = MJ.Json.Deserialize(json) as Dictionary<string, object>;
            if (fields == null) return impData;

            object obj;
            double parsedDouble;
            int parsedInt;

            if (fields.TryGetValue("app_version", out obj) && obj != null)
                impData.AppVersion = obj.ToString();

            if (fields.TryGetValue("adunit_id", out obj) && obj != null)
                impData.AdUnitId = obj.ToString();

            if (fields.TryGetValue("adunit_name", out obj) && obj != null)
                impData.AdUnitName = obj.ToString();

            if (fields.TryGetValue("adunit_format", out obj) && obj != null)
                impData.AdUnitFormat = obj.ToString();

            if (fields.TryGetValue("id", out obj) && obj != null)
                impData.ImpressionId = obj.ToString();

            if (fields.TryGetValue("currency", out obj) && obj != null)
                impData.Currency = obj.ToString();

            if (fields.TryGetValue("publisher_revenue", out obj) && obj != null
                && double.TryParse(MoPubUtils.InvariantCultureToString(obj), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out parsedDouble))
                impData.PublisherRevenue = parsedDouble;

            if (fields.TryGetValue("adgroup_id", out obj) && obj != null)
                impData.AdGroupId = obj.ToString();

            if (fields.TryGetValue("adgroup_name", out obj) && obj != null)
                impData.AdGroupName = obj.ToString();

            if (fields.TryGetValue("adgroup_type", out obj) && obj != null)
                impData.AdGroupType = obj.ToString();

            if (fields.TryGetValue("adgroup_priority", out obj) && obj != null
                && int.TryParse(MoPubUtils.InvariantCultureToString(obj), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out parsedInt))
                impData.AdGroupPriority = parsedInt;

            if (fields.TryGetValue("country", out obj) && obj != null)
                impData.Country = obj.ToString();

            if (fields.TryGetValue("precision", out obj) && obj != null)
                impData.Precision = obj.ToString();

            if (fields.TryGetValue("network_name", out obj) && obj != null)
                impData.NetworkName = obj.ToString();

            if (fields.TryGetValue("network_placement_id", out obj) && obj != null)
                impData.NetworkPlacementId = obj.ToString();

            impData.JsonRepresentation = json;

            return impData;
        }
    }

    // Data structure to register and initialize a mediated network.
    public class MediatedNetwork
    {
        public string AdapterConfigurationClassName { get; set; }
        public string MediationSettingsClassName    { get; set; }

        public Dictionary<string, string> NetworkConfiguration { get; set; }
        public Dictionary<string, object> MediationSettings    { get; set; }
        public Dictionary<string, string> MoPubRequestOptions  { get; set; }
    }

}
