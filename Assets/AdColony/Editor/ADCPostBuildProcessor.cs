#if UNITY_IOS || UNITY_ANDROID

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.IO;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace AdColony.Editor
{
    public class ADCPostBuildProcessor : MonoBehaviour
    {

#if UNITY_CLOUD_BUILD
        public static void OnPostprocessBuildiOS(string exportPath) {
            OnPostprocessBuild(BuildTarget.iOS, exportPath);
        }
#endif

        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget == BuildTarget.iOS)
            {
#if UNITY_IOS
                Debug.Log("AdColony: OnPostprocessBuild");
                UpdateProjectPlist(buildTarget, buildPath + "/Info.plist");
#endif
            }
        }

#if UNITY_IOS && UNITY_2019_3_OR_NEWER
        [PostProcessBuild(45)] // after Podfile generation (40) and that before "pod install" (50)
        private static void FixPodfile(BuildTarget buildTarget, string buildPath)
        {
            string podfilePath = buildPath + "/Podfile";
            string podfile = "";
            using (StreamReader sr = new StreamReader(podfilePath))
            {
                bool skipUntilEnd = false;
                string line = null;
                while ((line = sr.ReadLine()) != null) {
                    if (!skipUntilEnd) {
                        if (line.Contains("target 'Unity-iPhone'"))
                            skipUntilEnd = true;
                        else
                            podfile += line + "\n";
                    } else if (line.Contains("end")) {
                        skipUntilEnd = false;
                    }
                }
            }

            File.WriteAllText(podfilePath, podfile);
        }
#endif

        private static void UpdateProjectPlist(BuildTarget buildTarget, string plistPath)
        {
#if UNITY_IOS
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            PlistElementDict root = plist.root;
            var applicationQueriesSchemes = plist.root["LSApplicationQueriesSchemes"] != null ? plist.root["LSApplicationQueriesSchemes"].AsArray() : null;
            if (applicationQueriesSchemes == null)
                applicationQueriesSchemes = plist.root.CreateArray("LSApplicationQueriesSchemes");
            foreach (var scheme in new[]{ "fb", "instagram", "tumblr", "twitter" })
                if (applicationQueriesSchemes.values.Find(x => x.AsString() == scheme) == null)
                    applicationQueriesSchemes.AddString(scheme);
            foreach (var kvp in new[] {
            new []
                { "NSCalendarsUsageDescription", "Some ad content may create a calendar event." }
                ,
            new []
                { "NSPhotoLibraryUsageDescription", "Some ad content may require access to the photo library." }
                ,
            new []
                { "NSCameraUsageDescription", "Some ad content may access camera to take picture." }
                ,
            new []
                { "NSMotionUsageDescription", "Some ad content may require access to accelerometer for interactive ad experience." }
            })
            if (!root.values.ContainsKey(kvp[0]))
                    root.SetString(kvp[0], kvp[1]);

            const string skadnetworksKey = "SKAdNetworkItems";
            const string skadnetworkKey = "SKAdNetworkIdentifier";
            string[] skadnetworksIds = {
                "4pfyvq9l8r.skadnetwork",
                "yclnxrl5pm.skadnetwork",
                "v72qych5uu.skadnetwork",
                "tl55sbb4fm.skadnetwork",
                "t38b2kh725.skadnetwork",
                "prcb7njmu6.skadnetwork",
                "ppxm28t8ap.skadnetwork",
                "mlmmfzh3r3.skadnetwork",
                "klf5c3l5u5.skadnetwork",
                "hs6bdukanm.skadnetwork",
                "c6k4g5qg8m.skadnetwork",
                "9t245vhmpl.skadnetwork",
                "9rd848q2bz.skadnetwork",
                "8s468mfl3y.skadnetwork",
                "7ug5zh24hu.skadnetwork",
                "4fzdc2evr5.skadnetwork",
                "4468km3ulz.skadnetwork",
                "3rd42ekr43.skadnetwork",
                "2u9pt9hc89.skadnetwork",
                "m8dbw4sv7c.skadnetwork",
                "7rz58n8ntl.skadnetwork",
                "ejvt5qm6ak.skadnetwork",
                "5lm9lj6jb7.skadnetwork",
                "44jx6755aq.skadnetwork",
                "mtkv5xtk9e.skadnetwork",
                "6g9af3uyq4.skadnetwork",
                "uw77j35x4d.skadnetwork",
                "u679fj5vs4.skadnetwork",
                "rx5hdcabgc.skadnetwork",
                "g28c52eehv.skadnetwork",
                "cg4yq2srnc.skadnetwork",
                "9nlqeag3gk.skadnetwork",
                "275upjj5gd.skadnetwork",
                "wg4vff78zm.skadnetwork",
                "qqp299437r.skadnetwork",
                "kbmxgpxpgc.skadnetwork",
                "294l99pt4k.skadnetwork",
                "2fnua5tdw4.skadnetwork",
                "ydx93a7ass.skadnetwork",
                "523jb4fst2.skadnetwork",
                "cj5566h2ga.skadnetwork",
                "r45fhb6rf7.skadnetwork",
                "g2y4y55b64.skadnetwork",
                "wzmmz9fp6w.skadnetwork",
                "n6fk4nfna4.skadnetwork",
                "kbd757ywx3.skadnetwork",
                "n9x2a789qt.skadnetwork",
                "pwa73g5rt2.skadnetwork",
                "74b6s63p6l.skadnetwork",
                "44n7hlldy6.skadnetwork",
                "5l3tpt7t6e.skadnetwork",
                "e5fvkxwrpn.skadnetwork",
                "gta9lk7p23.skadnetwork",
                "84993kbrcf.skadnetwork",
                "pwdxu55a5a.skadnetwork",
                "6964rsfnh4.skadnetwork",
                "a7xqa6mtl2.skadnetwork"
            };
            var skadnetworks = plist.root[skadnetworksKey] != null ? plist.root[skadnetworksKey].AsArray() : null;
            if (skadnetworks == null)
                skadnetworks = plist.root.CreateArray(skadnetworksKey);

            List<string> skadnetworksCopy = new List<string>();
            foreach (var skadnetwork in skadnetworks.values)
                skadnetworksCopy.Add(skadnetwork[skadnetworkKey].AsString());

            foreach (var skadnetwork in skadnetworksIds) {
                if (skadnetworksCopy.Find(x => x == skadnetwork) == null) {
                    var dict = skadnetworks.AddDict();
                    dict.SetString(skadnetworkKey, skadnetwork);
                }
            }

            File.WriteAllText(plistPath, plist.WriteToString());
#endif
        }
    }
}

#endif
