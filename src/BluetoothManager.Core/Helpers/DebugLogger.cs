namespace BluetoothManager.Core.Helpers;

/// <summary>
/// Thread-safe debug logger
/// </summary>
public static class DebugLogger
{
    private static readonly object _lock = new();
    private static readonly string _logPath = "debug.log";

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                System.IO.File.AppendAllText(_logPath, $"[{DateTime.Now}] {message}\n");
            }
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
