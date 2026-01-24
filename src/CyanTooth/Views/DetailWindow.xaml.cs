using CyanTooth.Platform.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CyanTooth.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CyanTooth.Views;

/// <summary>
/// Detailed main window with tabs
/// </summary>
public partial class DetailWindow : Wpf.Ui.Controls.FluentWindow
{
    private bool _hasNavigated = false;

    public DetailWindow()
    {
        InitializeComponent();
        try
        {
            DataContext = App.Current.Services.GetRequiredService<DetailViewModel>();
            
            // Set TargetPageTypes in code-behind to avoid XAML parser issues
            if (RootNavigation.MenuItems[0] is Wpf.Ui.Controls.NavigationViewItem deviceItem)
                deviceItem.TargetPageType = typeof(Pages.DevicePage);
                
            if (RootNavigation.MenuItems[1] is Wpf.Ui.Controls.NavigationViewItem settingsItem)
                settingsItem.TargetPageType = typeof(Pages.SettingsPage);
                
            if (RootNavigation.FooterMenuItems[0] is Wpf.Ui.Controls.NavigationViewItem aboutItem)
                aboutItem.TargetPageType = typeof(Pages.AboutPage);
            
            // Ensure back button is hidden
            Loaded += (s, e) => 
            {
                RootNavigation.IsBackButtonVisible = Wpf.Ui.Controls.NavigationViewBackButtonVisible.Collapsed;
                
                // Navigate to first page only if not yet navigated
                if (!_hasNavigated)
                {
                     RootNavigation.Navigate(typeof(Pages.DevicePage));
                     _hasNavigated = true;
                }
            };

            // Handle mouse wheel globally to fix scroll issues with NavigationView
            // Use AddHandler with handledEventsToo=true to receive events even if already handled
            AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnPreviewMouseWheel), true);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("DetailWindow DataContext injection failed", ex);
        }
    }

    /// <summary>
    /// Handle mouse wheel events globally and forward them to the appropriate ScrollViewer.
    /// This fixes the issue where WPF-UI's NavigationView intercepts scroll events.
    /// </summary>
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Find the NavigationView's internal ScrollViewer (which actually handles page scrolling)
        if (e.OriginalSource is DependencyObject source)
        {
            // Find any ScrollViewer in the parent chain that has scrollable content
            var scrollViewer = FindScrollableScrollViewer(source);
            
            if (scrollViewer != null)
            {
                double scrollAmount = -e.Delta;
                double newOffset = scrollViewer.VerticalOffset + scrollAmount;
                newOffset = Math.Max(0, Math.Min(newOffset, scrollViewer.ScrollableHeight));
                
                scrollViewer.ScrollToVerticalOffset(newOffset);
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Find the first ScrollViewer in the parent chain that has scrollable content (ScrollableHeight > 0).
    /// </summary>
    private static ScrollViewer? FindScrollableScrollViewer(DependencyObject element)
    {
        DependencyObject? current = element;
        while (current != null)
        {
            if (current is ScrollViewer sv && sv.ScrollableHeight > 0)
            {
                return sv;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 除非是应用显式退出(通过托盘菜单)，否则点击关闭只隐藏窗口
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
