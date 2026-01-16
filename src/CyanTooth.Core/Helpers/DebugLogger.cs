using System;
using System.IO;
using System.Windows;

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
                // 获取 LocalAppData 绝对路径
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                // 如果系统路径获取失败，强制报错，绝不使用相对路径
                if (string.IsNullOrEmpty(localAppData))
                {
                    throw new InvalidOperationException("无法获取系统的 LocalApplicationData 路径。");
                }

                _logDirectory = Path.Combine(localAppData, "CyanTooth", "logs");
                
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                _logPath = Path.Combine(_logDirectory, "debug.log");
                
                // 写入明确的启动绝对路径标记
                File.AppendAllText(_logPath, $"{Environment.NewLine}>>>> [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SYSTEM_BOOT] 日志流已重定向至: {_logPath}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // 如果 LocalAppData 写入失败，最后的退避方案是 Temp 目录（绝对路径）
                try
                {
                    string tempPath = Path.GetTempPath();
                    _logPath = Path.Combine(tempPath, "cyantooth_emergency.log");
                    File.AppendAllText(_logPath, $"[CRITICAL_FAILURE] 无法写入系统应用目录，回退至临时目录。错误: {ex.Message}{Environment.NewLine}");
                }
                catch
                {
                    // 如果连 Temp 都写不了，说明环境极度受限
                    _logPath = null;
                }
            }
        }
    }

    public static void Log(string message, string level = "INFO")
    {
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
        string fullMessage = ex != null ? $"{message}{Environment.NewLine}堆栈信息: {ex}" : message;
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
