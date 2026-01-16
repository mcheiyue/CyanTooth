using CyanTooth.Platform.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CyanTooth.ViewModels;

namespace CyanTooth.Controls;

/// <summary>
/// Device card control for displaying a Bluetooth device
/// </summary>
public partial class DeviceCard : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty DeviceProperty =
        DependencyProperty.Register(nameof(Device), typeof(DeviceViewModel), typeof(DeviceCard),
            new PropertyMetadata(null, OnDeviceChanged));

    public static readonly DependencyProperty ConnectCommandProperty =
        DependencyProperty.Register(nameof(ConnectCommand), typeof(ICommand), typeof(DeviceCard));

    public static readonly DependencyProperty FavoriteCommandProperty =
        DependencyProperty.Register(nameof(FavoriteCommand), typeof(ICommand), typeof(DeviceCard));

    public DeviceViewModel? Device
    {
        get => (DeviceViewModel?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public ICommand? ConnectCommand
    {
        get => (ICommand?)GetValue(ConnectCommandProperty);
        set => SetValue(ConnectCommandProperty, value);
    }

    public ICommand? FavoriteCommand
    {
        get => (ICommand?)GetValue(FavoriteCommandProperty);
        set => SetValue(FavoriteCommandProperty, value);
    }

    public DeviceCard()
    {
        InitializeComponent();
    }

    private static void OnDeviceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DeviceCard card && e.NewValue is DeviceViewModel device)
        {
            card.DataContext = device;
        }
    }

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Card clicked, Device={Device?.Name}, IsAudioDevice={Device?.IsAudioDevice}, ConnectCommand={ConnectCommand != null}");
        
        // 单击设备时连接/断开（不限制只有音频设备）
        if (Device != null && ConnectCommand != null)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Executing ConnectCommand for {Device.Name}");
            ConnectCommand.Execute(Device);
        }
    }

    private void MoreOptions_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button)
        {
            var contextMenu = new ContextMenu();
            
            // 切换收藏
            var favoriteItem = new MenuItem 
            { 
                Header = Device?.IsFavorite == true ? "取消收藏" : "添加收藏"
            };
            favoriteItem.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Star24 };
            favoriteItem.Click += (s, args) => FavoriteCommand?.Execute(Device);
            contextMenu.Items.Add(favoriteItem);
            
            contextMenu.Items.Add(new Separator());
            
            // 打开系统设置
            var settingsItem = new MenuItem { Header = "打开系统蓝牙设置" };
            settingsItem.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Settings24 };
            settingsItem.Click += OpenSettings_Click;
            contextMenu.Items.Add(settingsItem);
            
            contextMenu.PlacementTarget = button;
            contextMenu.IsOpen = true;
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:bluetooth",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors
        }
    }
}
