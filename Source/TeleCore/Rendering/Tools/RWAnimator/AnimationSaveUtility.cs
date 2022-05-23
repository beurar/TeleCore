using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    internal static class AnimationSaveUtility
    {
        private static readonly string NewFileSuffix = ".anim";
        private static readonly string NewDefSuffix = ".xml";
        public static readonly string _SavingNode = "animation";

        public static string SavedAnimationsFolderPath => GenFilePaths.FolderUnderSaveData("Animations");
        public static string SavedAnimationDefsFolderPath => TeleCoreMod.Settings.animationSettings.SaveAnimationDefLocation;


        public static void Save(string fileName, string documentElementName, Action saveAction)
        {
            var path = Path.GetFullPath($"{Path.Combine(SavedAnimationsFolderPath, fileName)}{NewFileSuffix}");
            try
            {
                if (!File.Exists(path))
                {
                    DoSave(path, documentElementName, saveAction);
                }
                else
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation($"Override {fileName}?", delegate
                    {
                        RemoveFileIfExists(path, false);
                        DoSave(path, documentElementName, saveAction);
                    }, true, null, WindowLayer.Super));
                }
            }
            catch (Exception ex4)
            {
                GenUI.ErrorDialog("ProblemSavingFile".Translate(path, ex4.ToString()));
                throw;
            }
        }

        public static void SaveDef(string fileName, string documentElementName, Action saveAction)
        {
            var path = Path.GetFullPath($"{Path.Combine(SavedAnimationDefsFolderPath, fileName)}{NewDefSuffix}");
            if (!File.Exists(path))
            {
                DoSave(path, documentElementName, saveAction);
            }
            else
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation($"Override {fileName}?", delegate
                {
                    RemoveFileIfExists(path, false);
                    DoSave(path, documentElementName, saveAction);
                }, true, null, WindowLayer.Super));
            }
        }

        private static void DoSave(string fullPath, string documentElementName, Action saveAction)
        {
            try
            {
                Scribe.saver.InitSaving(fullPath, documentElementName);
                saveAction();
                Scribe.saver.FinalizeSaving();
            }
            catch (Exception ex)
            {
                TLog.Warning($"An exception was thrown during saving to \"{fullPath}\": {ex}");
                Scribe.saver.ForceStop();
                SafeSaver.RemoveFileIfExists(fullPath, false);
                throw;
            }
        }

        private static void RemoveFileIfExists(string path, bool rethrow)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                TLog.Warning($"Could not remove file \"{path}\": {ex}");
                if (rethrow) throw;
            }
        }

        private static void FileMove(string from, string to)
        {
            Exception ex = null;
            for (int i = 0; i < 50; i++)
            {
                try
                {
                    File.Move(from, to);
                    return;
                }
                catch (Exception ex2)
                {
                    if (ex == null)
                    {
                        ex = ex2;
                    }
                }
                Thread.Sleep(1);
            }
            throw ex;
        }
    }
}
