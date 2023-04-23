using System;
using System.IO;
using System.Text;
using Verse;

namespace TeleCore.Data.Logging;

[StaticConstructorOnStartup]
public static class TLogger
{
    private static StreamWriter _Writer;

    private static string Prefix => $"[{Find.TickManager.TicksGame}]";
    
    static TLogger()
    {
        try
        {
            var directory = new DirectoryInfo(GenFilePaths.FolderUnderSaveData("Logging"));
            var file = new FileInfo(Path.Combine(directory.FullName, "TeleCore.txt"));
            if (file.Exists)
            {
                var count = directory.GetFiles().Length;
                string backupPath = $"{Path.GetFileNameWithoutExtension(file.FullName)}_{count-1}.txt";
                file.CopyTo(backupPath, true);
                file.Delete();
            }
            var _fileStream = file.Open(FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            _Writer = new StreamWriter(_fileStream);
            _Writer.AutoFlush = true;
            
            ApplicationQuitUtility.ApplicationQuitEvent += delegate
            {
                _Writer.Close();
                _Writer.Dispose();
            };
  
        }
        catch(Exception ex)
        {
            TLog.Error($"Error while creating logger: {ex}");
        }
    }

    public static void Log(string message)
    {
        try
        {
            _Writer.WriteLine($"{Prefix}: {message}");
        }
        catch (Exception ex)
        {
            TLog.Error($"Error while logging: {ex}");
        }
    }
}