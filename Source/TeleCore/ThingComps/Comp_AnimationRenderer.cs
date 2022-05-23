using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class Comp_AnimationRenderer : ThingComp
    {
        private List<Material>[] MaterialsBySide;
        private Dictionary<string, AnimationPart>[] TaggedAnimationsBySide;
        private Dictionary<string, Dictionary<Material, List<KeyFrame>>>[] TaggedAnimationFramesBySide;

        private string currentAnim = null;
        private int currentFrame;
        private int finalFrame;

        private bool sustain = false;

        public int CurrentFrame => currentFrame;

        public CompProperties_AnimationRenderer Props => (CompProperties_AnimationRenderer) props;

        private Dictionary<Material, TextureData> DataByMat = new();
        private Dictionary<string, AnimationPart> CurrentAnimations => TaggedAnimationsBySide[UsedRotation];
        private Dictionary<string, Dictionary<Material, List<KeyFrame>>> CurrentKeyFrames => TaggedAnimationFramesBySide[UsedRotation];
        private List<Material> CurrentMaterials => MaterialsBySide[UsedRotation];

        private bool CurrentFlipped => parent.Rotation == Rot4.West;
        private int UsedRotation
        {
            get
            {
                if (CurrentFlipped)
                    return Rot4.East.AsInt;
                return parent.Rotation.AsInt;
            }
        }

        private Vector3 UsedDrawPos
        {
            get
            {
                if (parent is Pawn pawn)
                {
                    return pawn.drawer.DrawPos;
                }
                return parent.DrawPos;
            }
        }

        private Vector2 UsedDrawSize
        {
            get
            {
                if (parent is Pawn pawn)
                {
                    return pawn.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize;
                }
                return parent.Graphic.drawSize;
            }
        }

        public void Start(string tag, bool sustain = false)
        {
            if (!CurrentAnimations.ContainsKey(tag))
            {
                TLog.Error($"{Props.animationDef} does not contain animation tag '{tag}'");
                return;
            }
            if (currentAnim == tag && sustain) return;
            currentFrame = 0;
            currentAnim = tag;
            finalFrame = CurrentAnimations[tag].frames;
            this.sustain = sustain;
        }

        public void Stop()
        {
            currentAnim = null;
            currentFrame = 0;
            finalFrame = 0;
            sustain = false;

            if (Props.defaultAnimationTag != null)
            {
                Start(Props.defaultAnimationTag, true);
            }
        }

        private void Ticker()
        {
            if (currentAnim == null) return;
            if (currentFrame < finalFrame)
            {
               // GetDataFor();
                currentFrame++;
            }

            if (currentFrame >= finalFrame)
            {
                if (sustain)
                {
                    currentFrame = 0;
                    return;
                }
                Stop();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.animationDef == null) return;
            
            //
            MaterialsBySide = new List<Material>[4] {new(), new(), new(), new()};
            TaggedAnimationFramesBySide = new Dictionary<string, Dictionary<Material, List<KeyFrame>>>[4] {new(), new(), new(), new()};
            TaggedAnimationsBySide = new Dictionary<string, AnimationPart>[4]{new(), new(), new(), new()};

            //Construct data
            for (int i = 0; i < 4; i++)
            {
                var set = Props.animationDef.animationSets[i];
                if (!(set.HasAnimations && set.HasTextures)) continue;
                foreach (var textureData in set.textureParts)
                {
                    MaterialsBySide[i].Add(textureData.Material);
                    DataByMat.Add(textureData.Material, textureData);
                }

                foreach (var animation in set.animations)
                { 
                    Dictionary<Material, List<KeyFrame>> frames = new();
                    if (animation.keyFrames != null)
                    {
                        for (var k = 0; k < animation.keyFrames.Count; k++)
                        {
                            var frameSet = animation.keyFrames[k];
                            var list = frameSet?.savedList ?? new List<KeyFrame>();
                            frames.Add(MaterialsBySide[i][k], list);
                        }
                    }
                    TaggedAnimationsBySide[i].Add(animation.tag, animation);
                    TaggedAnimationFramesBySide[i].Add(animation.tag, frames);
                }
            }

            //
            TFind.TickManager?.RegisterMapTickAction(Ticker);

            //
            if (Props.defaultAnimationTag != null)
            {
                Start(Props.defaultAnimationTag, true);
            }
        }

        private bool GetKeyFrames(Material material, out KeyFrame? frame1, out KeyFrame? frame2, out float dist)
        {
            frame1 = frame2 = null;
            var frames = CurrentKeyFrames[currentAnim][material];
            var framesMin = frames.Where(t => t.Frame <= CurrentFrame);
            var framesMax = frames.Where(t => t.Frame >= CurrentFrame);
            if (framesMin.TryMaxBy(t => t.Frame, out var value1))
                frame1 = value1;

            if (framesMax.TryMinBy(t => t.Frame, out var value2))
                frame2 = value2;
            dist = Mathf.InverseLerp(frame1?.Frame ?? 0, frame2?.Frame ?? 0, CurrentFrame);

            return frame1 != null && frame2 != null;
        }

        private KeyFrameData GetDataFor(Material material)
        {
            if (GetKeyFrames(material, out var frame1, out var frame2, out var lerpVal))
                return frame1.Value.Data.Interpolated(frame2.Value.Data, lerpVal);

            if (frame1.HasValue)
                return frame1.Value.Data;
            if (frame2.HasValue)
                return frame2.Value.Data;

            return new KeyFrameData();
        }

        public override void PostDraw()
        {
            if (currentAnim == null) return;

            float curAlt = parent.DrawPos.y + Altitudes.AltInc * CurrentMaterials.Count;

            var drawSize = UsedDrawSize;
            var origin = new Vector3(UsedDrawPos.x, curAlt, UsedDrawPos.z);

            for (var i = 0; i < CurrentMaterials.Count; i++)
            {
                var material = CurrentMaterials[i];
                var keyFrame = GetDataFor(material);
                var data = DataByMat[material];

                var drawOffset = PixelToCellOffset(keyFrame.TPosition, drawSize);
                var rotation = keyFrame.TRotation;

                var size = keyFrame.TSize * keyFrame.TexCoords.size * drawSize;
                var actualSize = keyFrame.TSize * data.TexCoordReference.size * drawSize;

                if (CurrentFlipped)
                {
                    drawOffset.x = -drawOffset.x;
                    rotation = -rotation;
                }

                var newDrawPos = origin + drawOffset;
                newDrawPos += GenTransform.OffSetByCoordAnchor(actualSize, size, data.TexCoordAnchor);
                if (keyFrame.PivotPoint != Vector2.zero)
                {
                    var pivotPoint = newDrawPos + PixelToCellOffset(keyFrame.PivotPoint, drawSize);
                    Vector3 relativePos = rotation.ToQuat() * (newDrawPos - pivotPoint);
                    newDrawPos = pivotPoint + relativePos;
                }

                material.SetTextureOffset("_MainTex", keyFrame.TexCoords.position);
                material.SetTextureScale("_MainTex", keyFrame.TexCoords.size);

                Mesh mesh = CurrentFlipped ? MeshPool.GridPlaneFlip(size) : MeshPool.GridPlane(size);
                Graphics.DrawMesh(mesh, newDrawPos, rotation.ToQuat(), material, 0, null, 0);
                curAlt -= Altitudes.AltInc;
            }
        }

        private Vector3 PixelToCellOffset(Vector2 pixelOffset, Vector2 drawSize)
        {
            float width = (pixelOffset.x / TextureCanvas._TileSize) * drawSize.x;
            float height = -((pixelOffset.y / TextureCanvas._TileSize) * drawSize.y);
            return new Vector3(width, 0, height);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action()
            {
                defaultLabel = "Run Animation Once",
                action = delegate
                {
                    var options = new List<FloatMenuOption>();
                    foreach (var animationPart in CurrentAnimations.Keys)
                    {
                        options.Add(new FloatMenuOption($"Run {animationPart}", delegate
                        {
                            Start(animationPart);
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }
    }

    public class CompProperties_AnimationRenderer : CompProperties
    {
        public AnimationDataDef animationDef;

        public string defaultAnimationTag = null;

        public CompProperties_AnimationRenderer()
        {
            compClass = typeof(Comp_AnimationRenderer);
        }
    }
}
