using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BackupSystem;

public class BackupManager
{

    private readonly ConcurrentDictionary<(string Source, string Target), BackupWorker> _workers = new();

  
    private string NormalizePath(string path)
    {
        string fullPath = Path.GetFullPath(path);
    
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public async Task AddBackup(string source, string[] targets)
    {
      
        string absSource = NormalizePath(source);

        if (!Directory.Exists(absSource))
        {
            Logger.Error($"Katalog źródłowy nie istnieje: {absSource}");
            return;
        }

        foreach (var target in targets)
        {
            
            string absTarget = NormalizePath(target);

            if (absTarget.StartsWith(absSource, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Error($"Niedozwolone: Katalog docelowy {absTarget} jest wewnątrz źródłowego!");
                continue;
            }

            
            if (_workers.ContainsKey((absSource, absTarget)))
            {
                Logger.Error($"Kopia {absSource} -> {absTarget} już istnieje.");
                continue;
            }

            if (Directory.Exists(absTarget))
            {
                if (Directory.GetFileSystemEntries(absTarget).Any())
                {
                    Logger.Error($"Katalog docelowy {absTarget} nie jest pusty!");
                    continue;
                }
            }
            else
            {
                Directory.CreateDirectory(absTarget);
            }
            
            var worker = new BackupWorker(absSource, absTarget);
            if (_workers.TryAdd((absSource, absTarget), worker))
            { 
                _ = worker.StartAsync();
            }
        }
    }

    public void EndBackup(string source, string[] targets)
    {
        string absSource = NormalizePath(source);

        foreach (var target in targets)
        {
            string absTarget = NormalizePath(target);

            if (_workers.TryRemove((absSource, absTarget), out var worker))
            {
                worker.Dispose(); 
                Logger.Success($"Zatrzymano kopię do: {absTarget}");
            }
            else
            {
                Logger.Error($"Nie znaleziono aktywnej kopii: {absSource} -> {absTarget}");
            }
        }
    }

    public void ListBackups()
    {
        if (_workers.IsEmpty)
        {
            Logger.Info("Brak aktywnych kopii.");
            return;
        }

        Logger.Info("Aktywne kopie zapasowe:");
        foreach (var key in _workers.Keys)
        {
            Logger.ListEntry(key.Source, key.Target);
        }
    }

    public void RestoreBackup(string source, string target)
    {
        string absSource = NormalizePath(source);
        string absTarget = NormalizePath(target);

        if (!Directory.Exists(absTarget))
        {
            Logger.Error("Katalog z kopią zapasową (źródło przywracania) nie istnieje.");
            return;
        }

        Logger.Info($"Rozpoczynanie przywracania: {absTarget} -> {absSource} ...");
        try
        {
            
            SyncEngine.Restore(absSource, absTarget);
            Logger.Success("Przywracanie zakończone.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Błąd przywracania: {ex.Message}");
        }
    }

    public void Shutdown()
    {
        foreach (var worker in _workers.Values)
        {
            worker.Dispose();
        }
        _workers.Clear();
    }
}