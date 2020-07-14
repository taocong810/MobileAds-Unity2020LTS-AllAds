using System.IO;
using System.Text;
using System.Xml;

// IPostGenerateGradleAndroidProject is supported only from 2018.2 and higher
#if UNITY_2018_2_OR_NEWER
// Post process script to enable hardware acceleration
// Revised from https://raw.githubusercontent.com/gree/unity-webview/master/plugins/Editor/UnityWebViewPostprocessBuild.cs
// It is not guaranteed that the publisher would have the Android module downloaded
// from the Hub
#if UNITY_WSA_10_0 || UNITY_WINRT_8_1 || UNITY_METRO || UNITY_IOS
public class VungleGradlePostProcessor
#else
public class VungleGradlePostProcessor : UnityEditor.Android.IPostGenerateGradleAndroidProject
#endif
{
    public void OnPostGenerateGradleAndroidProject(string basePath)
    {
        var androidManifest = new AndroidManifest(GetManifestPath(basePath));
        var changed = androidManifest.SetHardwareAccelerated(true);
        if (changed)
        {
            androidManifest.Save();
        }
    }

    public int callbackOrder
    {
        get
        {
            return 1;
        }
    }

    private string GetManifestPath(string basePath)
    {
        var pathBuilder = new StringBuilder(basePath);
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
        return pathBuilder.ToString();
    }
}

internal class AndroidXmlDocument : XmlDocument
{
    private string m_Path;
    protected XmlNamespaceManager nsMgr;
    public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";

    public AndroidXmlDocument(string path)
    {
        m_Path = path;
        using (var reader = new XmlTextReader(m_Path))
        {
            reader.Read();
            Load(reader);
        }
        nsMgr = new XmlNamespaceManager(NameTable);
        nsMgr.AddNamespace("android", AndroidXmlNamespace);
    }

    public string Save()
    {
        return SaveAs(m_Path);
    }

    public string SaveAs(string path)
    {
        using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
        {
            writer.Formatting = Formatting.Indented;
            Save(writer);
        }
        return path;
    }
}

internal class AndroidManifest : AndroidXmlDocument
{
    public AndroidManifest(string path) : base(path)
    {
    }

    internal XmlNode GetActivityWithLaunchIntent()
    {
        return
            SelectSingleNode(
                "/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN' and "
                + "intent-filter/category/@android:name='android.intent.category.LAUNCHER']",
                nsMgr);
    }

    internal bool SetHardwareAccelerated(bool enabled)
    {
        bool changed = false;
        var activity = GetActivityWithLaunchIntent() as XmlElement;
        if (activity.GetAttribute("hardwareAccelerated", AndroidXmlNamespace) != ((enabled) ? "true" : "false"))
        {
            activity.SetAttribute("hardwareAccelerated", AndroidXmlNamespace, (enabled) ? "true" : "false");
            changed = true;
        }
        return changed;
    }
}
#endif
