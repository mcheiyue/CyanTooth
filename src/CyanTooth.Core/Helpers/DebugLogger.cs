using System;
using System.IO;
using System.Windows;
using System.Diagnostics;

namespace CyanTooth.Core.Helpers;

/// <summary>
/// 生产级线程安全日志器。
/// 严格锁定路径为 %LocalAppData%，禁止任何形式的相对路径回退。
/// </summary>
public static class DebugLogger
{
    private static readonly object _lock = new();
    private static string? _logPath;
    private static string? _logDirectory;
    private const long MaxLogSize = 10 * 1024 * 1024; // 10MB
    private const int MaxArchiveFiles = 5;

    static DebugLogger()
    {
        Initialize();
    }

    public static void Initialize()
    {
        if (_logPath != null) return;

        lock (_lock)
        {
            if (_logPath != null) return;

            try
            {
                // 1. 尝试获取 LocalAppData 绝对路径
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                // 2. 如果系统路径获取失败，尝试用户 Profile 路径
                if (string.IsNullOrEmpty(localAppData))
                {
                    localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local");
                }

                // 3. 最终退避：系统临时目录 (绝对路径)
                if (string.IsNullOrEmpty(localAppData) || !Directory.Exists(Path.GetDirectoryName(localAppData) ?? "C:\\"))
                {
                    localAppData = Path.GetTempPath();
                }

                _logDirectory = Path.Combine(localAppData, "CyanTooth", "logs");
                
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // 使用唯一的日志文件名，避免与系统自带的 debug.log 混淆
                _logPath = Path.Combine(_logDirectory, "cyantooth_runtime_v5.log");
                
                // 写入启动标记，使用绝对路径
                File.AppendAllText(_logPath, $"{Environment.NewLine}=================================================={Environment.NewLine}");
                File.AppendAllText(_logPath, $">>>> [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SYSTEM_BOOT] 日志重定向成功{Environment.NewLine}");
                File.AppendAllText(_logPath, $">>>> 进程 ID: {Environment.ProcessId}{Environment.NewLine}");
                File.AppendAllText(_logPath, $">>>> 运行目录: {AppDomain.CurrentDomain.BaseDirectory}{Environment.NewLine}");
                File.AppendAllText(_logPath, $"=================================================={Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // 最后的防线：尝试直接写入 Temp 目录下的紧急文件
                try
                {
                    string emergencyPath = Path.Combine(Path.GetTempPath(), "cyantooth_critical_err.log");
                    File.AppendAllText(emergencyPath, $"[{DateTime.Now}] 初始化失败: {ex}{Environment.NewLine}");
                    _logPath = emergencyPath;
                }
                catch
                {
                    _logPath = null;
                }
            }
        }
    }

    public static void Log(string message, string level = "INFO")
    {
        if (_logPath == null) Initialize();
        if (_logPath == null) return;

        try
        {
            lock (_lock)
            {
                RotateLogFilesIfNeeded();
                File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level.ToUpper()}] {message}{Environment.NewLine}");
            }
        }
        catch { /* 忽略写入错误 */ }
    }

    public static void LogError(string message, Exception? ex = null)
    {
        string fullMessage = ex != null ? $"{message}{Environment.NewLine}异常详情: {ex}" : message;
        Log(fullMessage, "ERROR");
    }

    private static void RotateLogFilesIfNeeded()
    {
        if (_logPath == null || _logDirectory == null) return;

        try
        {
            FileInfo fileInfo = new FileInfo(_logPath);
            if (!fileInfo.Exists || fileInfo.Length < MaxLogSize) return;

            for (int i = MaxArchiveFiles - 1; i >= 1; i--)
            {
                string oldFile = Path.Combine(_logDirectory, $"debug.{i}.log");
                string newFile = Path.Combine(_logDirectory, $"debug.{i + 1}.log");
                if (File.Exists(oldFile)) File.Move(oldFile, newFile, true);
            }

            string firstArchive = Path.Combine(_logDirectory, "debug.1.log");
            File.Move(_logPath, firstArchive, true);
        }
        catch { }
    }
}
