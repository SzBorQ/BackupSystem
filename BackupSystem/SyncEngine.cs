using System;
using System.IO;

namespace BackupSystem;


public static class SyncEngine
{
    public static void CopyDirectory(string sourceDir, string targetDir, string rootSource, string rootTarget)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(targetDir, fileName);
            CopyFileOrSymlink(file, destFile, rootSource, rootTarget);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(directory);
            string destDir = Path.Combine(targetDir, dirName);
            CopyDirectory(directory, destDir, rootSource, rootTarget);
        }
    }

    public static void CopyFileOrSymlink(string sourcePath, string targetPath, string rootSource, string rootTarget)
    {
        var info = new FileInfo(sourcePath);

        if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            try
            {
               
                var target = File.ResolveLinkTarget(sourcePath, true);
                if (target == null) return;

                string linkTarget = target.FullName;

                if (linkTarget.StartsWith(rootSource, StringComparison.OrdinalIgnoreCase))
                {
                    linkTarget = linkTarget.Replace(rootSource, rootTarget, StringComparison.OrdinalIgnoreCase);
                }

                if (File.Exists(targetPath) || Directory.Exists(targetPath))
                    File.Delete(targetPath);

                File.CreateSymbolicLink(targetPath, linkTarget);
            }
            catch (Exception ex)
            {
                Logger.Error($"Błąd kopiowania symlinku {sourcePath}: {ex.Message}");
            }
        }
        else 
        {
            
            File.Copy(sourcePath, targetPath, true);
        }
    }

    public static void Restore(string sourceRoot, string targetRoot)
    { 
        
        DeleteExtras(sourceRoot, targetRoot);
        
        CopyChanged(targetRoot, sourceRoot);
    }

    private static void DeleteExtras(string currentSource, string referenceTarget)
    {
        if (!Directory.Exists(currentSource)) return;

        foreach (var file in Directory.GetFiles(currentSource))
        {
  
            string relative = Path.GetRelativePath(currentSource, file);
            string targetPath = Path.Combine(referenceTarget, relative);

            if (!File.Exists(targetPath) && !IsSymlink(targetPath)) 
            {
                File.Delete(file);
                Logger.Info($"Usunięto nadmiarowy plik podczas przywracania: {file}");
            }
        }

        foreach (var dir in Directory.GetDirectories(currentSource))
        {
            string relative = Path.GetRelativePath(currentSource, dir);
            string targetPath = Path.Combine(referenceTarget, relative);

            if (!Directory.Exists(targetPath))
            {
                Directory.Delete(dir, true);
                Logger.Info($"Usunięto nadmiarowy katalog podczas przywracania: {dir}");
            }
            else
            {
                DeleteExtras(dir, targetPath);
            }
        }
    }

    private static void CopyChanged(string sourceDir, string destDir)
    {
        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destDir, fileName);

            if (ShouldCopy(file, destFile)) 
            {
                File.Copy(file, destFile, true); 
                Logger.Info($"Przywrócono: {fileName}");
            }
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            string destSubDir = Path.Combine(destDir, dirName);
            CopyChanged(dir, destSubDir);
        }
    }

    private static bool ShouldCopy(string source, string dest)
    {
        if (!File.Exists(dest)) return true;
        var sInfo = new FileInfo(source);
        var dInfo = new FileInfo(dest);
        
        return sInfo.LastWriteTimeUtc != dInfo.LastWriteTimeUtc || sInfo.Length != dInfo.Length;
    }

    private static bool IsSymlink(string path)
    { 
        try
        {
            var attr = File.GetAttributes(path);
            return (attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch { return false; }
    }
}