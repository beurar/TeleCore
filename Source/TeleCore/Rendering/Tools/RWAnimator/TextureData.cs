using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public struct MaterialData : IExposable
    {
        private Color color;
        private string texPath;
        private string shaderPath;

        [Unsaved] 
        private WrappedTexture texture;
        [Unsaved]
        private Shader shader;

        public void ExposeData()
        {
            Scribe_Values.Look(ref texPath, "texPath", forceSave: true);
            Scribe_Values.Look(ref color, "color", forceSave: true); 
            Scribe_Values.Look(ref shaderPath, "shaderPath", forceSave: true);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                texture = new WrappedTexture(texPath, ContentFinder<Texture2D>.Get(texPath));
                shader = ShaderDatabase.LoadShader(shaderPath);
            }
        }

        public MaterialData(WrappedTexture fromTexture)
        {
            this.texPath = fromTexture.Path;
            shader = ShaderDatabase.CutoutComplex;
            shaderPath = ShaderTypeDefOf.CutoutComplex.shaderPath;
            color = Color.white;
            texture = fromTexture;
        }

        public MaterialData(Material fromMat)
        {
            shader = fromMat.shader;
            shaderPath = fromMat.shader.Location();
            texture = new WrappedTexture(fromMat.mainTexture.Location(), fromMat.mainTexture);
            texPath = texture.Path;
            color = fromMat.color;
        }

        public Material GetMat()
        {
            var materialInt = new Material(shader);
            materialInt.name = $"{texPath}";
            materialInt.mainTexture = texture.Texture;
            materialInt.color = color;
            return materialInt;
        }
    }

    public struct TextureData : IExposable
    {
        private MaterialData materialData;
        private bool attachScript = false;
        private string layerTag = null;
        private TexCoordAnchor anchor = TexCoordAnchor.Center;
        private int layerIndex = 0;

        //
        private Rect texCoordsReference;

        [Unsaved]
        private Material matInt;

        private Texture Texture => Material.mainTexture;
        public Material Material => matInt;

        public Rect TexCoordReference => texCoordsReference;

        public TexCoordAnchor TexCoordAnchor
        {
            get => anchor;
            set => anchor = value;
        }

        public bool AttachScript
        {
            get => attachScript; 
            set => attachScript = value;
        }

        public string LayerTag
        {
            get => layerTag;
            set => layerTag = value;
        }

        public string StringBuffer { get; set; } = "0";
        public int LayerIndex
        {
            get => layerIndex;
            set => layerIndex = value;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref materialData, "materialData");
            Scribe_Rect.Look(ref texCoordsReference, "texCoords");
            Scribe_Values.Look(ref attachScript, "attacheScript");
            Scribe_Values.Look(ref layerTag, "layerTag");
            Scribe_Values.Look(ref layerIndex, "layerIndex");
            Scribe_Values.Look(ref anchor, "anchor");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                matInt = materialData.GetMat();
                matInt.SetTextureOffset("_MainTex", texCoordsReference.position);
                matInt.SetTextureScale("_MainTex", texCoordsReference.size);
            }
        }

        public void SetTexCoordReference(Rect texCoords)
        {
            texCoordsReference = texCoords;
        }

        public TextureData(WrappedTexture texture)
        {
            materialData = new MaterialData(texture);
            matInt = materialData.GetMat();
            texCoordsReference = new Rect(0, 0, 1, 1);
        }

        public TextureData(Material material)
        {
            materialData = new MaterialData(material);
            matInt = material;
            texCoordsReference = new Rect(0, 0, 1, 1);
        }
    }
}
