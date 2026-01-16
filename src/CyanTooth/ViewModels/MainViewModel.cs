using CyanTooth.Platform.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyanTooth.Core.Models;
using CyanTooth.Core.Services;

namespace CyanTooth.ViewModels;

/// <summary>
/// ViewModel for the main flyout window
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly BluetoothService _bluetoothService;
    private readonly ConfigService _configService;
    private readonly NotificationService _notificationService;

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();
    public ICollectionView DevicesView { get; }

    [ObservableProperty]
    private DeviceViewModel? _selectedDevice;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showOnlyConnected;

    [ObservableProperty]
    private bool _showOnlyAudio;

    public int ConnectedDeviceCount => Devices.Count(d => d.IsConnected);
    public int TotalDeviceCount => Devices.Count;

    public MainViewModel(BluetoothService bluetoothService, ConfigService configService, NotificationService notificationService)
    {
        _bluetoothService = bluetoothService;
        _configService = configService;
        _notificationService = notificationService;

        // Setup collection view with filtering
        DevicesView = CollectionViewSource.GetDefaultView(Devices);
        DevicesView.Filter = FilterDevices;
        DevicesView.SortDescriptions.Add(new SortDescription(nameof(DeviceViewModel.IsFavorite), ListSortDirection.Descending));
        DevicesView.SortDescriptions.Add(new SortDescription(nameof(DeviceViewModel.IsConnected), ListSortDirection.Descending));
        DevicesView.SortDescriptions.Add(new SortDescription(nameof(DeviceViewModel.Name), ListSortDirection.Ascending));

        // Subscribe to service events
        _bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
        _bluetoothService.DeviceRemoved += OnDeviceRemoved;
        _bluetoothService.DeviceConnectionChanged += OnDeviceConnectionChanged;
        _bluetoothService.DeviceBatteryChanged += OnDeviceBatteryChanged;
    }

    public void Initialize()
    {
        _bluetoothService.Start();
    }

    partial void OnSearchTextChanged(string value)
    {
        DevicesView.Refresh();
    }

    partial void OnShowOnlyConnectedChanged(bool value)
    {
        DevicesView.Refresh();
    }

    partial void OnShowOnlyAudioChanged(bool value)
    {
        DevicesView.Refresh();
    }

    private bool FilterDevices(object obj)
    {
        if (obj is not DeviceViewModel device) return false;

        // Check hidden devices
        if (_configService.Settings.HiddenDevices.Contains(device.Id))
            return false;

        // Check connected filter
        if (ShowOnlyConnected && !device.IsConnected)
            return false;

        // Check audio filter
        if (ShowOnlyAudio && !device.IsAudioDevice)
            return false;

        // Check search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            return device.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   device.MacAddress.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            Devices.Clear();
            _bluetoothService.RefreshDevices();
            // 增加一点延迟，确保搜索结果能显示出来
            await Task.Delay(1500);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync(DeviceViewModel? device)
    {
        DebugLogger.Log($" MainViewModel.ConnectAsync: device={device?.Name ?? "null"}, Id={device?.Id ?? "null"}");
        if (device == null) return;
        device.IsOperating = true;
        try
        {
            var result = await _bluetoothService.ConnectAsync(device.Id);
            DebugLogger.Log($" MainViewModel.ConnectAsync: result={result}");
        }
        catch (Exception ex)
        {
            DebugLogger.Log($" MainViewModel.ConnectAsync: EXCEPTION={ex.Message}");
        }
        finally
        {
            device.IsOperating = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync(DeviceViewModel? device)
    {
        DebugLogger.Log($" MainViewModel.DisconnectAsync: device={device?.Name ?? "null"}, Id={device?.Id ?? "null"}");
        if (device == null) return;
        device.IsOperating = true;
        try
        {
            var result = await _bluetoothService.DisconnectAsync(device.Id);
            DebugLogger.Log($" MainViewModel.DisconnectAsync: result={result}");
        }
        catch (Exception ex)
        {
            DebugLogger.Log($" MainViewModel.DisconnectAsync: EXCEPTION={ex.Message}");
        }
        finally
        {
            device.IsOperating = false;
        }
    }

    [RelayCommand]
    private async Task ToggleConnectionAsync(DeviceViewModel? device)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] ToggleConnectionAsync called, device={device?.Name ?? "null"}");
        DebugLogger.Log($" ToggleConnectionAsync: device={device?.Name ?? "null"}, IsConnected={device?.IsConnected}");
        
        if (device == null) return;
        if (device.IsConnected)
            await DisconnectAsync(device);
        else
            await ConnectAsync(device);
    }

    [RelayCommand]
    private void ToggleFavorite(DeviceViewModel? device)
    {
        if (device == null) return;
        device.IsFavorite = !device.IsFavorite;
        if (device.IsFavorite)
            _configService.AddFavorite(device.Id);
        else
            _configService.RemoveFavorite(device.Id);
        DevicesView.Refresh();
    }

    [RelayCommand]
    private void HideDevice(DeviceViewModel? device)
    {
        if (device == null) return;
        _configService.HideDevice(device.Id);
        DevicesView.Refresh();
    }

    private void OnDeviceDiscovered(object? sender, Core.Events.DeviceDiscoveredEventArgs e)
    {
        var device = _bluetoothService.GetDevice(e.DeviceId);
        if (device == null) return;

        App.Current.Dispatcher.Invoke(() =>
        {
            var existingDevice = Devices.FirstOrDefault(d => d.Id == e.DeviceId);
            if (existingDevice != null) return;

            var viewModel = new DeviceViewModel(device)
            {
                IsFavorite = _configService.Settings.FavoriteDevices.Contains(device.Id)
            };
            Devices.Add(viewModel);
            OnPropertyChanged(nameof(TotalDeviceCount));
            OnPropertyChanged(nameof(ConnectedDeviceCount));
        });
    }

    private void OnDeviceRemoved(object? sender, Core.Events.DeviceRemovedEventArgs e)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var device = Devices.FirstOrDefault(d => d.Id == e.DeviceId);
            if (device != null)
            {
                Devices.Remove(device);
                OnPropertyChanged(nameof(TotalDeviceCount));
                OnPropertyChanged(nameof(ConnectedDeviceCount));
            }
        });
    }

    private void OnDeviceConnectionChanged(object? sender, Core.Events.DeviceConnectionChangedEventArgs e)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var device = Devices.FirstOrDefault(d => d.Id == e.DeviceId);
            if (device != null)
            {
                device.IsConnected = e.IsConnected;
                OnPropertyChanged(nameof(ConnectedDeviceCount));
                DevicesView.Refresh();
            }
        });

        _notificationService.ShowConnectionNotification(e);
    }

    private void OnDeviceBatteryChanged(object? sender, Core.Events.DeviceBatteryChangedEventArgs e)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var device = Devices.FirstOrDefault(d => d.Id == e.DeviceId);
            if (device != null)
            {
                device.BatteryLevel = e.NewBatteryLevel;
            }
        });

        _notificationService.ShowLowBatteryNotification(e);
    }

    public void Dispose()
    {
        _bluetoothService.DeviceDiscovered -= OnDeviceDiscovered;
        _bluetoothService.DeviceRemoved -= OnDeviceRemoved;
        _bluetoothService.DeviceConnectionChanged -= OnDeviceConnectionChanged;
        _bluetoothService.DeviceBatteryChanged -= OnDeviceBatteryChanged;
    }
}
