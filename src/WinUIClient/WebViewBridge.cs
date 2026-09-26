using Microsoft.UI.Xaml.Controls;

namespace WinUIClient;

public static class WebViewBridge
{
    public static WebView2? WebViewInstance { get; set; }

    public static void PostMessageToChart(string json)
    {
        try
        {
            if (WebViewInstance?.CoreWebView2 != null)
            {
                WebViewInstance.CoreWebView2.PostWebMessageAsJson(json);
            }
        }
        catch
        {
            // ignore for POC
        }
    }
}
