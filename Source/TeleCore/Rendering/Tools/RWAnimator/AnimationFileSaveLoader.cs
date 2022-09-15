using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal class AnimationFileSaveLoader : Window
    {
        private TextureCanvas canvas;

        //Loading
        private Task loadFilesTask;
        private List<AnimationFileInfo> files;

        //Rendering
        protected Vector2 scrollPosition = Vector2.zero;

        private AnimationMetaData AnimationInfo => canvas.AnimationData;

        //public override string Label => "Save & Load";

        private IEnumerable<FileInfo> AllAnimationFiles
        {
            get
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(AnimationSaveUtility.SavedAnimationsFolderPath);
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
                return from f in directoryInfo.GetFiles()
                    where f.Extension == ".anim"
                    orderby f.LastWriteTime descending
                    select f;
            }
        }

        //
        public override Vector2 InitialSize  => new Vector2(650, 500);

        public AnimationFileSaveLoader(TextureCanvas canvas) : base()
        {
            this.canvas = canvas;

            //Window Props
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            layer = WindowLayer.Super;
        }

        //Loading
        private void ReloadFiles()
        {
            if (loadFilesTask != null && loadFilesTask.Status != TaskStatus.RanToCompletion)
            {
                loadFilesTask.Wait();
            }
            files.Clear();
            foreach (var fileInfo in AllAnimationFiles)
            {
                try
                {
                    files.Add(new AnimationFileInfo(fileInfo));
                }
                catch (Exception ex)
                {
                    TLog.Error($"Exception loading {fileInfo.Name}: {ex}");
                }
            }

            loadFilesTask = Task.Run(ReloadFilesTask);
        }

        private void ReloadFilesTask()
        {
            Parallel.ForEach(files, delegate(AnimationFileInfo file)
            {
                try
                {
                    file.LoadData();
                }
                catch (Exception ex)
                {
                    TLog.Error($"Exception loading {file.FileInfo.Name}: {ex}");
                }
            });
        }

        private void SaveAnimationDef()
        {
            try
            {
                AnimationSaveUtility.SaveDef($"{AnimationInfo.defName}Def", "Defs", delegate
                {
                    var animationData = canvas.AnimationData;
                    var newDef = animationData.ConstructAnimationDef();
                    Scribe_Deep.Look(ref newDef, $"{nameof(TeleCore)}.{nameof(AnimationDataDef)}");
                });
            }
            catch (Exception arg)
            {
                TLog.Error("Exception while saving animation Def: " + arg);
            }
        }

        private void SaveAnimation()
        {
            try
            {
                AnimationSaveUtility.Save(AnimationInfo.defName.TrimStart().TrimEnd(), "AnimationMetaData", delegate
                {
                    var animationData = canvas.AnimationData;
                    Scribe_Deep.Look(ref animationData, AnimationSaveUtility._SavingNode);
                });
            }
            catch (Exception arg)
            {
                TLog.Error("Exception while saving animation: " + arg);
            }
        }

        //TODO: Add mod file metadata to know which mods are needef for textures
        private void LoadAnimation(FileInfo fileInfo)
        {
            TLog.Message($"Init Loading {fileInfo.FullName}", Color.magenta);
            Scribe.loader.InitLoading(fileInfo.FullName);
            try
            {
                if (!Scribe.EnterNode(AnimationSaveUtility._SavingNode))
                {
                    Log.Error("Could not find animation XML node.");
                    Scribe.ForceStop();
                    return;
                }
                canvas.Reset();
                canvas.AnimationData.LoadAnimation();
                Scribe.loader.FinalizeLoading();
            }
            catch (Exception)
            {
                Scribe.ForceStop();
                throw;
            }
        }

        private string PathFromMod(DirectoryInfo directory, string name)
        {
            var parent = directory;
            string pathBack = Path.Combine(parent.Name, name);
            while (!parent.Name.Equals("Animations"))
            {
                parent = parent.Parent;
                pathBack = Path.Combine(parent.Name, pathBack);
            }
            return pathBack;
        }

        private List<FloatMenuOption> ModSelectionMenuOptions
        {
            get
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var mod in LoadedModManager.RunningModsListForReading)
                {
                    options.Add(new FloatMenuOption(mod.Name, delegate
                    {
                        SetDefLocationForMod(mod);
                    }));
                }
                return options;
            }
        }

        private void SetDefLocationForMod(ModContentPack mod)
        {
            Application.OpenURL(mod.FolderName);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (files == null)
            {
                files = new List<AnimationFileInfo>();
                ReloadFiles();
            }

            //
            var topRect = new Rect(inRect.x, inRect.y, inRect.width, 30).ContractedBy(5);

            //
            var listerArea = inRect.BottomPartPixels(inRect.height - topRect.height).ContractedBy(5);
            var saverBlock = listerArea.TopPartPixels((WidgetRow.IconSize * 2) + 4);
            var animSaverBar = saverBlock.TopPartPixels(WidgetRow.IconSize + 2);
            var defSaverBar = saverBlock.BottomPartPixels(WidgetRow.IconSize + 2);
            var listerRect = listerArea.BottomPartPixels(listerArea.height - saverBlock.height);

            TWidgets.DrawColoredBox(listerArea, TColor.BlueHueBG, TColor.MenuSectionBGBorderColor, 1);
            TWidgets.GapLine(saverBlock.x, saverBlock.yMax, saverBlock.width, 6, 0, TextAnchor.LowerCenter);

            //Save Def
            animSaverBar = animSaverBar.ContractedBy(2);
            WidgetRow row = new WidgetRow(animSaverBar.x, animSaverBar.y, gap: 0);
            if (row.ButtonBox("Save .anim", TColor.White01, TColor.White025))
            {
                if (!canvas.AnimationData.Initialized) return;
                SaveAnimation();
                ReloadFiles();
            }
            if (row.ButtonBox("Open Save Directory", TColor.White01, TColor.White025))
            {
                Application.OpenURL(AnimationSaveUtility.SavedAnimationsFolderPath);
            }
            if (row.ButtonBox("Reload", TColor.White01, TColor.White025))
            {
                ReloadFiles();
            }

            //Save Def
            defSaverBar = defSaverBar.ContractedBy(2);
            WidgetRow row2 = new WidgetRow(defSaverBar.x, defSaverBar.y, gap: 0);
            if (row2.ButtonBox("Create Def", TColor.White01, TColor.White025))
            {
                if (!canvas.AnimationData.Initialized) return;
                DirectoryInfo directoryInfo = new DirectoryInfo(AnimationSaveUtility.SavedAnimationDefsFolderPath);
                if (!directoryInfo.Exists) directoryInfo.Create();

                SaveAnimationDef();
            }
            if (row2.ButtonBox("Open Def Directory", TColor.White01, TColor.White025))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(AnimationSaveUtility.SavedAnimationDefsFolderPath);
                if (!directoryInfo.Exists) directoryInfo.Create();

                Application.OpenURL(AnimationSaveUtility.SavedAnimationDefsFolderPath);
            }

            if (row2.ButtonBox("Set Def Directory", TColor.White01, TColor.White025))
            {
                var selDirAction = (DirectoryInfo info) =>
                {
                    TeleCoreMod.Settings.SetAnimationDefLocation(info.FullName);
                };
                Find.WindowStack.Add(new Dialog_DirectoryBrowser(selDirAction, "Select Def Creation Directory", GenFilePaths.ModsFolderPath));
            }

            if (files.Any())
            {
                Vector2 selSize = new Vector2(listerRect.width - 16f, 40);
                Rect outRect = listerRect.ContractedBy(5);
                Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, files.Count * selSize.y);
                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, false);
                {
                    float curY = 0;
                    int i = 0;
                    foreach (var animationFile in files)
                    {
                        var FileInfo = animationFile.FileInfo;
                        Rect fileSelRect = new Rect(0f, curY, selSize.x, selSize.y);
                        if (i % 2 == 0)
                            Widgets.DrawAltRect(fileSelRect);

                        Widgets.BeginGroup(fileSelRect);
                        {
                            Rect leftHalf = fileSelRect.AtZero().LeftHalf();
                            Rect rightHalf = fileSelRect.AtZero().RightHalf();
                            Rect nameLabelRect = leftHalf.TopHalf();
                            Rect pathLabelRect = leftHalf.BottomHalf();

                            Rect buttonsRect = rightHalf.RightPartPixels((fileSelRect.height * 3) + 10);

                            Rect loadButton = buttonsRect.LeftPartPixels(fileSelRect.height*2);
                            Rect deleteButton = buttonsRect.RightPartPixels(fileSelRect.height);

                            GUI.color = Dialog_FileList.DefaultFileTextColor;
                            Text.Font = GameFont.Small;
                            Text.Anchor = TextAnchor.UpperLeft;
                            Widgets.Label(nameLabelRect, FileInfo.Name);

                            GUI.color = TColor.White075;
                            Text.Font = GameFont.Tiny;
                            Text.Anchor = TextAnchor.LowerLeft;
                            Widgets.Label(pathLabelRect, $"{PathFromMod(FileInfo.Directory, FileInfo.Name)}");

                            GUI.color = Color.white;
                            Text.Font = GameFont.Small;
                            Text.Anchor = default;

                            if (Widgets.ButtonText(loadButton, "Load", true, true, true))
                            {
                                LoadAnimation(FileInfo);
                                Close();
                            }

                            //Rect buttonRect = new Rect(fileSelRect.width - 36f, (fileSelRect.height - 36f) / 2f, 36f, 36f);
                            if (Widgets.ButtonImage(deleteButton, TexButton.DeleteX, Color.white, GenUI.SubtleMouseoverColor, true))
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDelete".Translate(FileInfo.Name), delegate
                                {
                                    FileInfo.Delete();
                                    ReloadFiles();
                                }, true, null, WindowLayer.Super));
                            }
                        }
                        Widgets.EndGroup();
                        curY += selSize.y;
                        i++;
                    }
                }
                Widgets.EndScrollView();
            }
        }
    }
}
