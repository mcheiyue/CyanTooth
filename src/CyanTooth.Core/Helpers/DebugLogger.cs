using System;
using System.IO;

namespace BluetoothManager.Core.Helpers;

/// <summary>
/// 增强的线程安全日志器，支持 AppData 存储和简单的日志滚动
/// </summary>
public static class DebugLogger
{
    private static readonly object _lock = new();
    private static readonly string _logDirectory = string.Empty;
    private static readonly string _logPath = "debug.log";
    private const long MaxLogSize = 10 * 1024 * 1024; // 10MB
    private const int MaxArchiveFiles = 5;

    static DebugLogger()
    {
        try
        {
            // 将日志存储在 %AppData%\CyanTooth\logs 目录下
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appData, "CyanTooth", "logs");
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            _logPath = Path.Combine(_logDirectory, "debug.log");
        }
        catch
        {
            // 如果无法创建目录，回退到当前目录
            _logPath = "debug.log";
        }
    }

    public static void Log(string message, string level = "INFO")
    {
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
        try
        {
            FileInfo fileInfo = new FileInfo(_logPath);
            if (!fileInfo.Exists || fileInfo.Length < MaxLogSize)
            {
                return;
            }

            // 滚动旧日志: debug.4.log -> debug.5.log, ..., debug.log -> debug.1.log
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
            // 滚动失败则继续写入当前文件或清空
            try { File.WriteAllText(_logPath, string.Empty); } catch { }
        }
    }
}
