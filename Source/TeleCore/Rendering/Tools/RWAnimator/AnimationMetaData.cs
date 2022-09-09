using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal class AnimationMetaData : IExposable
    {
        //KeyFrame String Buffer
        internal const int BufferSize = 2 + 2 + 2 + 4 + 1;
        internal Dictionary<KeyFrame, string[]> StringBuffers = new();

        //Reference Data
        private TextureCanvas parentCanvas;

        //Animation
        internal string defName;
        private bool initialized = false;
        private Rot4 internalRot = Rot4.North;
        private List<UIElement>[] elementsByRotation;
        private List<AnimationPartValue>[] animationsByRotation;

        private AnimationDataDef internalDef;
        private List<int> lastSelectedElementIndex;
        private List<int> lastSelectedAnimationIndex;

        private AnimationPartValue _currentAnimation;


        public Rot4 CurRot => internalRot;
        public bool Initialized => initialized;
        public bool Loading { get; private set; }

        public int ElementIndex => lastSelectedElementIndex[CurRot.AsInt];
        public int AnimationIndex => lastSelectedAnimationIndex[CurRot.AsInt];
        public List<UIElement> CurrentElementList => elementsByRotation[CurRot.AsInt];
        public List<AnimationPartValue> CurrentAnimations => animationsByRotation[CurRot.AsInt];
        public AnimationPartValue SelectedAnimationPart => _currentAnimation;

        public AnimationMetaData(TextureCanvas parentCanvas)
        {
            this.parentCanvas = parentCanvas;
            lastSelectedElementIndex = new (){ -1, -1, -1, -1 };
            lastSelectedAnimationIndex = new (){ -1, -1, -1, -1 };
            //
            elementsByRotation = new List<UIElement>[4]
            {
                new(), new(), new(), new(),
            };

            animationsByRotation = new List<AnimationPartValue>[4]
            {
                new(), new(), new(), new(),
            };
        }

        public void Notify_Init()
        {
            this.initialized = true;
        }

        public AnimationDataDef ConstructAnimationDef()
        {
            AnimationDataDef newDef = new AnimationDataDef();
            newDef.defName = defName;
            newDef.animationSets = new List<AnimationSet>(4);
            for (int i = 0; i < 4; i++)
            {
                newDef.animationSets.Add(ConstructSetForRotation(i));
            }
            return newDef;
        }

        private AnimationSet ConstructSetForRotation(int rot)
        {
            AnimationSet animationSet = new AnimationSet();
            if (!elementsByRotation[rot].NullOrEmpty())
            {
                var orderedElements = elementsByRotation[rot].Select(t => (t as TextureElement));
                animationSet.textureParts = orderedElements.Select(t => t.GetData()).ToList();
            }

            if (!animationsByRotation[rot].NullOrEmpty())
            {
                animationSet.animations = new List<AnimationPart>(animationsByRotation[rot].Count);
                foreach (var partValue in animationsByRotation[rot])
                {
                    animationSet.animations.Add(partValue.ToAnimationPart(animationSet.textureParts));
                }
            }

            return animationSet;
        }

        private void LoadFromDef(AnimationDataDef dataDef)
        {
            defName = dataDef.defName;
            for (int i = 0; i < 4; i++)
            {
                LoadSetFrom(dataDef, i);
            }
        }

        private void LoadSetFrom(AnimationDataDef dataDef, int rot)
        {
            var animationSet = dataDef.animationSets[rot];

            if (animationSet.HasTextures)
            {
                foreach (var textureData in animationSet.textureParts)
                {
                    var newElement = new TextureElement(new Rect(Vector2.zero, parentCanvas.Size), textureData);
                    parentCanvas.Notify_LoadedElement(newElement);
                    elementsByRotation[rot].Add(newElement);
                }
            }

            if (animationSet.HasAnimations)
            {
                foreach (var animation in animationSet.animations)
                {
                    animationsByRotation[rot].Add(new AnimationPartValue(elementsByRotation[rot], animation));
                }
            }
        }

        public string[] BufferFor(KeyFrame frame)
        {
            if (StringBuffers.ContainsKey(frame))
            {
                return StringBuffers[frame];
            }
            var buffer = new string[BufferSize];
            frame.Data.UpdateBuffer(buffer);
            StringBuffers.Add(frame, buffer);
            return StringBuffers[frame];
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                internalDef = ConstructAnimationDef();
            }

            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_Values.Look(ref internalRot, "internalRot");
            Scribe_Deep.Look(ref internalDef, "internalDef");

            Scribe_Collections.Look(ref lastSelectedElementIndex, "lastSelectedElementIndex");
            Scribe_Collections.Look(ref lastSelectedAnimationIndex, "lastSelectedAnimationIndex");
        }

        public void LoadAnimation()
        {
            Loading = true;

            Scribe_Deep.Look(ref internalDef, "internalDef");
            Scribe_Values.Look(ref internalRot, "internalRot");
            Scribe_Collections.Look(ref lastSelectedElementIndex, "lastSelectedElementIndex");
            Scribe_Collections.Look(ref lastSelectedAnimationIndex, "lastSelectedAnimationIndex");

            //GenDataFromDef
            LoadFromDef(internalDef);

            if (internalDef != null)
            {
                initialized = true;
                _currentAnimation = CurrentAnimations[AnimationIndex];
            }

            Loading = false;
        }

        //
        public void Notify_RemoveAnimationPart(int index)
        {
            CurrentAnimations.RemoveAt(index);

            int nextIndex = CurrentAnimations.Count - 1;
            SetAnimationIndex(nextIndex);
            if (nextIndex >= 0)
                _currentAnimation = CurrentAnimations[nextIndex];
            else
                _currentAnimation = null;
        }

        public AnimationPartValue Notify_CreateNewAnimationPart(string tag, float lengthSeconds)
        {
            AnimationPartValue animation = new AnimationPartValue(tag, lengthSeconds);
            CurrentAnimations.Add(animation);
            return animation;
        }

        public void Notify_PostCreateAnimation()
        {
            _currentAnimation = CurrentAnimations.Last();
            SetAnimationIndex(CurrentAnimations.Count - 1);
        }

        public void SetRotation(Rot4 newRotation)
        {
            internalRot = newRotation;
            if (Enumerable.Any(CurrentAnimations))
            {
                _currentAnimation = CurrentAnimations[AnimationIndex];
            }
            else
            {
                _currentAnimation = null;
            }
        }

        public void SetAnimationPart(AnimationPartValue animationPart)
        {
            _currentAnimation = animationPart;
            SetAnimationIndex(CurrentAnimations.IndexOf(_currentAnimation));
        }

        public void SetElementIndex(int index)
        {
            lastSelectedElementIndex[CurRot.AsInt] = index;
        }

        public void SetAnimationIndex(int index)
        {
            lastSelectedAnimationIndex[CurRot.AsInt] = index;
        }

        public bool AnimationPartsFor(int i)
        {
            return !animationsByRotation[i].NullOrEmpty();
        }
    }

    public class AnimationPartValue
    {
        private IntRange _workingBounds;
        internal float _internalSeconds;
        internal string _internalSecondsBuffer;

        public string tag;
        public int frames;

        internal IntRange ReplayBounds
        {
            get => _workingBounds;
            set => _workingBounds = value;
        }

        internal Dictionary<IKeyFramedElement, Dictionary<int, KeyFrame>> InternalFrames { get; }
        internal Dictionary<int, AnimationActionEventFlag> InternalEventFlags { get; }

        internal bool InternalDifference => _internalSeconds.SecondsToTicks() != frames;

        public AnimationPartValue(string tag, float seconds)
        {
            _internalSeconds = seconds;
            this.tag = tag;
            this.frames = seconds.SecondsToTicks();
            ReplayBounds = new IntRange(0, frames);

            InternalFrames = new();
            InternalEventFlags = new();
        }

        public AnimationPartValue(List<UIElement> uiElements, AnimationPart animationPart)
        {
            tag = animationPart.tag;
            frames = animationPart.frames;
            _internalSeconds = frames.TicksToSeconds();
            _internalSecondsBuffer = _internalSeconds.ToString();

            ReplayBounds = animationPart.bounds;

            InternalFrames = new();

            int index = 0;
            foreach (var keyFrame in animationPart.keyFrames)
            {
                var texture = (TextureElement) uiElements[index];
                InternalFrames.Add(texture, new Dictionary<int, KeyFrame>());
                foreach (var frame in keyFrame.savedList)
                {
                    InternalFrames[texture].Add(frame.Frame, frame);
                }
                index++;
            }
        }

        public void SetFrames()
        {
            this.frames = _internalSeconds.SecondsToTicks();
            ReplayBounds = new IntRange(0, frames);
        }

        public List<ScribeList<KeyFrame>> KeyFramesList(List<TextureData> orderBy)
        {
            return InternalFrames.Copy()
                .OrderByDescending((x) => (orderBy.Count - 1) - orderBy.IndexOf((x.Key as TextureElement).GetData()))
                .Select(parentDict => new ScribeList<KeyFrame>(parentDict.Value.Select(keyFrameDict => keyFrameDict.Value).ToList(), LookMode.Deep))
                .ToList();
        }

        public AnimationPart ToAnimationPart(List<TextureData> orderBy)
        {
            return new AnimationPart()
            {
                tag = tag,
                frames = frames,
                bounds = ReplayBounds,
                keyFrames = KeyFramesList(orderBy),
                eventFlags = InternalEventFlags.OrderByDescending(t => frames- t.Key).Select(t => t.Value).ToList(),
            };
        }
    }
}
