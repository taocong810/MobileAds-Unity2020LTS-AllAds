using UnityEngine;

using MaxAdSize = MoPub.MaxAdSize;

public static class MoPubAdSizeMapping
{
    public static float Width(this MaxAdSize adSize)
    {
        switch (adSize) {
            case MaxAdSize.Width300Height50:
            case MaxAdSize.Width300Height250:
                return 300;
            case MaxAdSize.Width320Height50:
                return 320;
            case MaxAdSize.Width336Height280:
                return 336;
            case MaxAdSize.Width728Height90:
                return 728;
            case MaxAdSize.Width970Height90:
            case MaxAdSize.Width970Height250:
                return 970;
            case MaxAdSize.ScreenWidthHeight50:
            case MaxAdSize.ScreenWidthHeight90:
            case MaxAdSize.ScreenWidthHeight250:
            case MaxAdSize.ScreenWidthHeight280:
// screen width sizing is handled differently between the MoPub SDKs:
// > Android expects the LinearLayout.LayoutParams.MATCH_PARENT constant (-1)
// > iOS expects an explicit width in device-independent pixels (dips)
// more internal context at ADF-5729 and ADF-5945
#if UNITY_ANDROID
                return -1;
#else
                var mdpi = 160.0f; // standard baseline density
                var pixels = Screen.width;
                var dpi = Screen.dpi;
                var dips = pixels / (dpi / mdpi);
                return dips;
#endif
            default:
                // fallback to default size: Width320Height50
                return 300;
        }
    }


    public static float Height(this MaxAdSize adSize)
    {
        switch (adSize) {
            case MaxAdSize.Width300Height50:
            case MaxAdSize.Width320Height50:
            case MaxAdSize.ScreenWidthHeight50:
                return 50;
            case MaxAdSize.Width728Height90:
            case MaxAdSize.Width970Height90:
            case MaxAdSize.ScreenWidthHeight90:
                return 90;
            case MaxAdSize.Width300Height250:
            case MaxAdSize.Width970Height250:
            case MaxAdSize.ScreenWidthHeight250:
                return 250;
            case MaxAdSize.Width336Height280:
            case MaxAdSize.ScreenWidthHeight280:
                return 280;
            default:
                // fallback to default size: Width320Height50
                return 50;
        }
    }
}
