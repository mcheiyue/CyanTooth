using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CyanTooth.Platform.Helpers;

namespace CyanTooth.Views.Pages;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private void OnGithubClick(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/mcheiyue/CyanTooth");
    }

    private void OnFeedbackClick(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/mcheiyue/CyanTooth/issues");
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (System.Exception ex)
        {
            DebugLogger.LogError($"Failed to open URL '{url}' with default browser", ex);
            
            // Fallback for some environments
            try 
            {
                Process.Start("explorer.exe", url);
            }
            catch (System.Exception fallbackEx)
            {
                DebugLogger.LogError($"Failed to open URL '{url}' with explorer.exe", fallbackEx);
            }
        }
    }
}
