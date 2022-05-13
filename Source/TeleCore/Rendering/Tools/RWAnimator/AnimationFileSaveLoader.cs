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
    public class AnimationFileSaveLoader : Window
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
                DirectoryInfo directoryInfo = new DirectoryInfo(AnimationSaver.SavedAnimationsFolderPath);
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
            this.layer = WindowLayer.Super;
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
                AnimationSaver.SaveDef($"{AnimationInfo.defName}Def", "Defs", delegate
                {
                    var animationData = canvas.AnimationData;
                    var newDef = animationData.ConstructAnimationDef();
                    Scribe_Deep.Look(ref newDef, nameof(AnimationDataDef));
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
                AnimationSaver.Save(AnimationInfo.defName.TrimStart().TrimEnd(), "AnimationMetaData", delegate
                {
                    var animationData = canvas.AnimationData;
                    Scribe_Deep.Look(ref animationData, AnimationSaver._SavingNode);
                });
            }
            catch (Exception arg)
            {
                TLog.Error("Exception while saving animation: " + arg);
            }
        }

        private void LoadAnimation(FileInfo fileInfo)
        {
            TLog.Message($"Init Loading {fileInfo.FullName}", Color.magenta);
            Scribe.loader.InitLoading(fileInfo.FullName);
            try
            {
                if (!Scribe.EnterNode(AnimationSaver._SavingNode))
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

        public override void DoWindowContents(Rect inRect)
        {
            if (files == null)
            {
                files = new List<AnimationFileInfo>();
                ReloadFiles();
            }
            var SaveCurrentRect = new Rect(inRect.x, inRect.y, 140, 50).ContractedBy(5);
            if (Widgets.ButtonText(SaveCurrentRect, "Save Current"))
            {
                SaveAnimation();
                ReloadFiles();
            }

            var SaveCurrentDefRect = new Rect(SaveCurrentRect.xMax + 5, SaveCurrentRect.y, SaveCurrentRect.width, SaveCurrentRect.height);
            if (Widgets.ButtonText(SaveCurrentDefRect, "Save As Def"))
            {
                SaveAnimationDef();
            }


            var listerRect = inRect.BottomPartPixels(inRect.height - SaveCurrentRect.height).ContractedBy(5);
            TWidgets.DrawColoredBox(listerRect, TColor.BlueHueBG, TColor.MenuSectionBGBorderColor, 1);
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
