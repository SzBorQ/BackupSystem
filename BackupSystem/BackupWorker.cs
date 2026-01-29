using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BackupSystem;

public class BackupWorker : IDisposable
{
    public string SourcePath { get; }
    public string TargetPath { get; }
    private FileSystemWatcher? _watcher;
    private bool _isDisposed;

    public BackupWorker(string source, string target)
    {
        SourcePath = Path.GetFullPath(source);
        TargetPath = Path.GetFullPath(target);
    }

 
    public async Task StartAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                Logger.Info($"Rozpoczynanie synchronizacji: {SourcePath} -> {TargetPath}");
                SyncEngine.CopyDirectory(SourcePath, TargetPath, SourcePath, TargetPath);
                
                _watcher = new FileSystemWatcher(SourcePath);
                _watcher.IncludeSubdirectories = true;
                _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                        NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes;

                _watcher.Created += OnChanged;
                _watcher.Changed += OnChanged;
                _watcher.Renamed += OnRenamed;
                _watcher.Deleted += OnDeleted;
                _watcher.Error += OnError;

                _watcher.EnableRaisingEvents = true;
                Logger.Success($"Kopia aktywna i monitorowana: {TargetPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Błąd podczas inicjalizacji workera ({TargetPath}): {ex.Message}");
            }
        });
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;
        
        string relativePath = Path.GetRelativePath(SourcePath, e.FullPath);
        string targetFile = Path.Combine(TargetPath, relativePath);

        
        ProcessWithRetry(() =>
        {
            if (Directory.Exists(e.FullPath))
            {
                Directory.CreateDirectory(targetFile);
            }
            else if (File.Exists(e.FullPath))
            {
                SyncEngine.CopyFileOrSymlink(e.FullPath, targetFile, SourcePath, TargetPath);
            }
            Logger.Info($"Zaktualizowano: {relativePath}"); 
        }, e.FullPath);
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;
        
        try
        {
            string relativePath = Path.GetRelativePath(SourcePath, e.FullPath);
            string targetPath = Path.Combine(TargetPath, relativePath);

            if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
            else if (File.Exists(targetPath)) File.Delete(targetPath);
            
        }
        catch (Exception ex) { Logger.Error($"Błąd usuwania: {ex.Message}"); }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (_isDisposed) return;

        string relativeOld = Path.GetRelativePath(SourcePath, e.OldFullPath);
        string relativeNew = Path.GetRelativePath(SourcePath, e.FullPath);
        string targetOld = Path.Combine(TargetPath, relativeOld);
        string targetNew = Path.Combine(TargetPath, relativeNew);

        ProcessWithRetry(() =>
        {
            if (Directory.Exists(targetOld)) Directory.Move(targetOld, targetNew);
            else if (File.Exists(targetOld)) File.Move(targetOld, targetNew);
            else
            {
               
                if (File.Exists(e.FullPath))
                {
                    SyncEngine.CopyFileOrSymlink(e.FullPath, targetNew, SourcePath, TargetPath);
                }
            }
        }, e.FullPath);
    }

 
    private void ProcessWithRetry(Action action, string pathContext)
    {
        const int MaxRetries = 5;
        const int DelayMs = 200;

        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                action();
                return; 
            }
            catch (IOException)
            {
               
                if (i == MaxRetries - 1)
                {
                    Logger.Error($"Nie można uzyskać dostępu do pliku (zablokowany): {pathContext}");
                }
                else
                {
                    Thread.Sleep(DelayMs);
                }
            }
            catch (Exception ex)
            {
                
                Logger.Error($"Błąd operacji na pliku {pathContext}: {ex.Message}");
                return;
            }
        }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Logger.Error($"Błąd FileSystemWatcher: {e.GetException().Message}");
    }

    public void Dispose()
    {
        _isDisposed = true;
        _watcher?.Dispose();
    }
}