using System;
using System.IO;

namespace CyanTooth.Core.Helpers;

/// <summary>
/// 增强的线程安全日志器，支持 AppData 存储和简单的日志滚动。
/// 严格禁止向程序运行目录写入日志，确保在单文件打包模式下的路径稳定性。
/// </summary>
public static class DebugLogger
{
    private static readonly object _lock = new();
    private static string? _logDirectory;
    private static string? _logPath;
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
                // 统一使用 LocalApplicationData (%LocalAppData%)
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(appData))
                {
                    // 极端备选：系统临时目录
                    appData = Path.GetTempPath();
                }

                _logDirectory = Path.Combine(appData, "CyanTooth", "logs");
                
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                _logPath = Path.Combine(_logDirectory, "debug.log");
                
                // 写入启动标记，使用绝对路径
                File.AppendAllText(_logPath, $"{Environment.NewLine}>>>> [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [BOOT] CyanTooth 正在启动...{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // 最后的防线：尝试写入系统临时目录，绝对不写当前目录
                try 
                {
                    _logPath = Path.Combine(Path.GetTempPath(), "cyantooth_fallback.log");
                    File.AppendAllText(_logPath, $"[CRITICAL] 日志系统初始化失败，使用备选路径: {ex.Message}{Environment.NewLine}");
                }
                catch { /* 彻底失败则保持 _logPath 为 null */ }
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
        catch
        {
            // 忽略日志写入错误
        }
    }

    public static void LogError(string message, Exception? ex = null)
    {
        string fullMessage = ex != null ? $"{message}{Environment.NewLine}Exception: {ex}" : message;
        Log(fullMessage, "ERROR");
    }

    private static void RotateLogFilesIfNeeded()
    {
        if (_logPath == null || _logDirectory == null) return;

        try
        {
            FileInfo fileInfo = new FileInfo(_logPath);
            if (!fileInfo.Exists || fileInfo.Length < MaxLogSize)
            {
                return;
            }

            for (int i = MaxArchiveFiles - 1; i >= 1; i--)
            {
                string oldFile = Path.Combine(_logDirectory, $"debug.{i}.log");
                string newFile = Path.Combine(_logDirectory, $"debug.{i + 1}.log");
                if (File.Exists(oldFile))
                {
                    File.Move(oldFile, newFile, true);
                }
            }

            string firstArchive = Path.Combine(_logDirectory, "debug.1.log");
            File.Move(_logPath, firstArchive, true);
        }
        catch
        {
            // 滚动失败的处理逻辑
            try { File.WriteAllText(_logPath, string.Empty); } catch { }
        }
    }
}
