using CyanTooth.Platform.Helpers;


using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using CyanTooth.Core.Events;
using CyanTooth.Core.Models;


namespace CyanTooth.Core.Services;

/// <summary>
/// Service for showing Windows notifications
/// </summary>
public class NotificationService
{
    private const string AppId = "CyanTooth";
    private readonly ConfigService _configService;

    public NotificationService(ConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// Shows a device connection notification
    /// </summary>
    public void ShowConnectionNotification(DeviceConnectionChangedEventArgs args)
    {
        if (!_configService.Settings.ShowConnectionNotifications)
            return;

        var title = args.IsConnected ? "设备已连接" : "设备已断开";
        var message = args.DeviceName ?? "未知设备";
        var icon = args.IsConnected ? "🔗" : "🔌";

        ShowToast(title, $"{icon} {message}");
    }

    /// <summary>
    /// Shows a low battery notification
    /// </summary>
    public void ShowLowBatteryNotification(DeviceBatteryChangedEventArgs args)
    {
        if (!_configService.Settings.ShowLowBatteryNotifications)
            return;

        if (!args.NewBatteryLevel.HasValue)
            return;

        var threshold = _configService.Settings.LowBatteryThreshold;
        if (args.NewBatteryLevel.Value > threshold)
            return;

        // Don't notify if we already notified for this level range
        if (args.OldBatteryLevel.HasValue && args.OldBatteryLevel.Value <= threshold)
            return;

        var title = "低电量警告";
        var message = $"🔋 {args.DeviceName ?? "设备"}: {args.NewBatteryLevel}%";

        ShowToast(title, message);
    }

    /// <summary>
    /// Shows a generic toast notification
    /// </summary>
    public void ShowToast(string title, string message, string? imageUri = null)
    {
        try
        {
            var template = ToastTemplateType.ToastText02;
            var toastXml = ToastNotificationManager.GetTemplateContent(template);

            var textNodes = toastXml.GetElementsByTagName("text");
            textNodes[0].AppendChild(toastXml.CreateTextNode(title));
            textNodes[1].AppendChild(toastXml.CreateTextNode(message));

            // Add image if provided
            if (!string.IsNullOrEmpty(imageUri))
            {
                var imageTemplate = ToastTemplateType.ToastImageAndText02;
                toastXml = ToastNotificationManager.GetTemplateContent(imageTemplate);
                
                textNodes = toastXml.GetElementsByTagName("text");
                textNodes[0].AppendChild(toastXml.CreateTextNode(title));
                textNodes[1].AppendChild(toastXml.CreateTextNode(message));

                var imageNodes = toastXml.GetElementsByTagName("image");
                ((XmlElement)imageNodes[0]).SetAttribute("src", imageUri);
            }

            // Create and show the toast
            var toast = new ToastNotification(toastXml)
            {
                ExpirationTime = DateTimeOffset.Now.AddMinutes(5)
            };

            ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
        }
        catch
        {
            // Ignore notification errors
        }
    }
}
