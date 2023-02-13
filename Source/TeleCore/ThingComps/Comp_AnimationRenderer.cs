using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public struct RuntimeAnimationGroup
    {
        private Dictionary<(string, Rot4), RuntimeAnimation> animationsBySide;
        
        public RuntimeAnimation this[string refId, Rot4 side]
        {
            get
            {
                if (animationsBySide.TryGetValue((refId, side), out var value))
                {
                    return value;
                }

                return RuntimeAnimation.Invalid;
            }
        }

        public RuntimeAnimationGroup(AnimationDataDef def)
        {
            animationsBySide = new Dictionary<(string, Rot4), RuntimeAnimation>();
            //Go  through each rotation
            for (int i = 0; i < 4; i++)
            {
                //Get set
                var set = def.animationSets[i];
                if (!(set.HasAnimations && set.HasTextures)) continue;
                
                //
                foreach (var textureData in set.textureParts)
                {
                    materialsBySide[i].Add(textureData.Material);
                    DataByMat.Add(textureData.Material, textureData);
                }

                //
                foreach (var animation in set.animations)
                { 
                    Dictionary<Material, List<KeyFrame>> frames = new();
                    if (animation.keyFrames != null)
                    {
                        for (var k = 0; k < animation.keyFrames.Count; k++)
                        {
                            var frameSet = animation.keyFrames[k];
                            var list = frameSet?.savedList ?? new List<KeyFrame>();
                            frames.Add(materialsBySide[i][k], list);
                        }
                    }
                    TaggedAnimationsBySide[i].Add(animation.tag, animation);
                    TaggedAnimationFramesBySide[i].Add(animation.tag, frames);
                }
            }
        }
    }
    
    
    public struct KeyFrameChain
    {
        private int allocated;
        private RuntimeKeyFrame[] internalChain;
        
        public RuntimeKeyFrame this[int index]
        {
            get
            {
                if (index < 0) return internalChain[0];
                if (index <= internalChain.Length) return internalChain[internalChain.Length - 1];
                return internalChain[index];
            }
        }
        
        public RuntimeKeyFrame First => internalChain[0];
        public RuntimeKeyFrame Last => internalChain[internalChain.Length - 1];

        //
        public KeyFrameChain Allocate(int size)
        {
            internalChain = new RuntimeKeyFrame[size];
            return this;
        }
        
        public KeyFrameChain SetNext(KeyFrameData keyframe)
        {
            internalChain[allocated] = new RuntimeKeyFrame(this, keyframe, allocated);
            allocated++;
            return this;
        }
    }

    public readonly struct RuntimeKeyFrame
    {
        private readonly KeyFrameChain chain;
        
        public KeyFrameData KeyFrame { get; }
        public int Index { get; }

        public RuntimeKeyFrame Next => chain[Index + 1];
        public RuntimeKeyFrame Previous => chain[Index - 1];

        public RuntimeKeyFrame(KeyFrameChain chain, KeyFrameData data, int index)
        {
            this.chain = chain;
            KeyFrame = data;
            Index = index;
        }
    }

    public struct RuntimeAnimation
    {
        public string animationTag;
        public int curTick;
        public IntRange range;

        public bool IsActive => animationTag != null;

        public static RuntimeAnimation Prepare()
        {
            return new RuntimeAnimation
            {
                animationTag = null,
                curTick = 0,
                range = default
            };
        }
    }
    
    public class Comp_AnimationRenderer : ThingComp
    {
        //Dynamic cache
        private Dictionary<(string, Rot4, int), KeyFrameChain> cachedKeyChains = new();
        //private readonly List<KeyFrameData> cachedCurrentKeyFrames = new ();
        
        //Static animation data
        private List<Material>[] materialsBySide;
        private Dictionary<string, AnimationPart>[] TaggedAnimationsBySide;
        private Dictionary<string, Dictionary<Material, List<KeyFrame>>>[] TaggedAnimationFramesBySide;

        private string currentAnim = null;
        private int currentFrame;
        private int finalFrame;

        private bool shouldSustain = false;

        public Comp_AnimationRenderer()
        {
        }

        public int CurrentFrame => currentFrame;

        public CompProperties_AnimationRenderer Props => (CompProperties_AnimationRenderer) props;

        private Dictionary<Material, TextureData> DataByMat { get; } = new();
        private Dictionary<string, AnimationPart> CurrentAnimations => TaggedAnimationsBySide[UsedRotation];
        private Dictionary<string, Dictionary<Material, List<KeyFrame>>> CurrentKeyFrames => TaggedAnimationFramesBySide[UsedRotation];
        private List<Material> CurrentMaterials => materialsBySide[UsedRotation];

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

        //
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
            this.shouldSustain = sustain;
        }

        public void Stop()
        {
            currentAnim = null;
            currentFrame = 0;
            finalFrame = 0;
            shouldSustain = false;

            if (Props.defaultAnimationTag != null)
            {
                Start(Props.defaultAnimationTag, true);
            }
        }

        //
        private void Ticker()
        {
            //Skip when no animation selected
            if (currentAnim == null) return;
            
            //Check current frame


            if (currentFrame >= finalFrame)
            {
                if (shouldSustain)
                {
                    currentFrame = 0;
                    return;
                }
                Stop();
            }
        }

        private KeyFrameChain KeyFrames(string tag, Rot4 rot)
        {
            if (!cachedKeyChains.TryGetValue((tag, rot), out var value))
            {
                value = GetCurrentDataFor(CurrentMaterials[rot.AsInt]);
                cachedKeyChains.Add((tag, rot), value);
            }
            return value;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.animationDef == null) return;

            //
            materialsBySide = new List<Material>[4] {new(), new(), new(), new()};
            TaggedAnimationFramesBySide = new Dictionary<string, Dictionary<Material, List<KeyFrame>>>[4] {new(), new(), new(), new()};
            TaggedAnimationsBySide = new Dictionary<string, AnimationPart>[4]{new(), new(), new(), new()};
            
            //Construct data
            //Each side of a thing can have multiple animations
            //So: RuntimeAnimation belongs to a side, 
            
            //Go through each side of the thing
            for (int i = 0; i < 4; i++)
            {
                //Check if the side has an animationSet
                var set = Props.animationDef.animationSets[i];
                if (!(set.HasAnimations && set.HasTextures)) continue;
                
                //Go through each texture on the current animation set (each side has the same textures, for each animation)
                foreach (var textureData in set.textureParts)
                {
                    materialsBySide[i].Add(textureData.Material);
                    DataByMat.Add(textureData.Material, textureData);
                }

                //Now each side can have multiple animation parts
                foreach (var animation in set.animations)
                { 
                    Dictionary<Material, List<KeyFrame>> frames = new();
                    if (animation.keyFrames != null)
                    {
                        //For each previously selected texture-part, we have a mirrored keyframe set
                        for (var k = 0; k < animation.keyFrames.Count; k++)
                        {
                            //Working data
                            int lastFrameIndex = 0;
                            KeyFrame last = new KeyFrame();
                            KeyFrame next = new KeyFrame();
                            
                            //get basic keyframes
                            var keyFrames = animation.keyFrames[k]?.savedList ?? new List<KeyFrame>();
                            
                            //Generate chain with pre-cached interpolated frames
                            KeyFrameChain chain = new KeyFrameChain();
                            chain.Allocate(animation.frames);

                            //TODO: DEBUG AT HOME
                            KeyFrameData GetOrInterpolateKeyframe(int frame)
                            {
                                var keyFrame = keyFrames[frame];
                                if (keyFrame.Frame == frame)
                                {
                                    last = keyFrame;
                                    next = keyFrames[frame + 1];
                                    lastFrameIndex = frame;
                                    return keyFrame.Data;
                                }
                                return last.Data.Interpolated(next.Data, Mathf.InverseLerp(last.Frame, next.Frame, frame));
                            }
                            
                            for (int f = animation.bounds.min; f < animation.bounds.max; f++)
                            {
                                chain.SetNext(GetOrInterpolateKeyframe(f));
                            }
                            
                            cachedKeyChains.Add((animation.tag, new Rot4(i), k), chain);
                            
                            frames.Add(materialsBySide[i][k], keyFrames);
                        }
                    }
                    
                    //Add empty animations too
                    TaggedAnimationsBySide[i].Add(animation.tag, animation);
                    TaggedAnimationFramesBySide[i].Add(animation.tag, frames);
                }
            }

            //
            TFind.TickManager?.RegisterMapTickAction(Ticker);

            //Start default animation
            if (Props.defaultAnimationTag != null)
            {
                Start(Props.defaultAnimationTag, true);
            }
        }

        //TODO: CACHE INTERPOLATED FRAMES BY FRAME KEY - the keychain approach doesnt work, except when creating a full animation chain with pre-generated interpolated frames
        private bool GetCurrentKeyFrames(Material material, out KeyFrame frame1, out KeyFrame frame2, out float dist)
        {
            frame1 = frame2 = KeyFrame.Invalid;
            var frames = CurrentKeyFrames[currentAnim][material];
            var framesMin = frames.Where(t => t.Frame <= CurrentFrame);
            var framesMax = frames.Where(t => t.Frame >= CurrentFrame);
            if (framesMin.TryMaxBy(t => t.Frame, out var value1))
                frame1 = value1;

            if (framesMax.TryMinBy(t => t.Frame, out var value2))
                frame2 = value2;
            dist = Mathf.InverseLerp(frame1.Frame, frame2.Frame, CurrentFrame);

            return frame1.IsValid && frame2.IsValid;
        }

        private KeyFrameChain GetCurrentDataFor(Material material)
        {
            if (GetCurrentKeyFrames(material, out var frame1, out var frame2, out var lerpVal))
                return frame1.Data.Interpolated(frame2.Data, lerpVal);

            if (frame1.IsValid)
                return frame1.Data;
            if (frame2.IsValid)
                return frame2.Data;

            return new KeyFrameData();
        }

        private void PrepareAnimation(string tag)
        {
            
        }

        private KeyFrameData CurrentKeyFrame(int materialIndex)
        {
            var mat = CurrentMaterials[materialIndex];
            var frames = CurrentKeyFrames[currentAnim][mat];
        }

        private RuntimeAnimationGroup CurrentAnimation;
        
        public override void PostDraw()
        {
            if (currentAnim == null) return;
            
            //Base Information
            //Start with top layer of animation texture stack
            float currentLayer = parent.DrawPos.y + Altitudes.AltInc * CurrentMaterials.Count;

            //Get transform
            var drawSize = UsedDrawSize;
            var drawPos = new Vector3(UsedDrawPos.x, currentLayer, UsedDrawPos.z);

            //Iterate through animation layers, starting with first
            for (var i = 0; i < CurrentMaterials.Count; i++)
            {
                var material = CurrentMaterials[i];
                var keyFrame = currentKeyFramesBySide[i];
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

                var newDrawPos = drawPos + drawOffset;
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
                currentLayer -= Altitudes.AltInc;
            }
        }

        private Vector3 PixelToCellOffset(Vector2 pixelOffset, Vector2 drawSize)
        {
            float width = (pixelOffset.x / BaseCanvas._TileSize) * drawSize.x;
            float height = -((pixelOffset.y / BaseCanvas._TileSize) * drawSize.y);
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
