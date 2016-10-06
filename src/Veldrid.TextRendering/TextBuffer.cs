using SharpFont;
using System.Drawing;
using System.Numerics;
using Veldrid.Graphics;
using System;

namespace Veldrid.TextRendering
{
    public unsafe class TextBuffer
    {
        private IndexBuffer _ib;
        private VertexBuffer _vb;
        private Material _material;
        private int _count;
        private readonly RenderContext _rc;
        private ShaderConstantBindings _constantBindings;

        public TextBuffer(RenderContext rc, int capacity)
        {
            _rc = rc;
            var indicesArray = new ushort[capacity * 6];
            fixed (ushort* indicesPtr = indicesArray)
            {
                ushort* indices = indicesPtr;
                for (int i = 0, v = 0; i < capacity; i++, v += 4)
                {
                    *indices++ = (ushort)(v + 0);
                    *indices++ = (ushort)(v + 2);
                    *indices++ = (ushort)(v + 1);
                    *indices++ = (ushort)(v + 2);
                    *indices++ = (ushort)(v + 0);
                    *indices++ = (ushort)(v + 3);
                }
            }

            _ib = rc.ResourceFactory.CreateIndexBuffer(indicesArray, IndexFormat.UInt16, false);
            _material = CreateMaterial(rc);
        }

        public unsafe void Append(TextAnalyzer analyzer, FontFace font, string text, float fontSize, int atlasWidth, RectangleF drawRect)
        {
            Append(analyzer, font, text, fontSize, atlasWidth, drawRect, RgbaByte.White);
        }

        public unsafe void Append(TextAnalyzer analyzer, FontFace font, string text, float fontSize, int atlasWidth, RectangleF drawRect, RgbaByte color)
        {
            var layout = new TextLayout();
            var format = new TextFormat
            {
                Font = font,
                Size = fontSize
            };

            analyzer.AppendText(text, format);
            analyzer.PerformLayout(drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height, layout);

            var memBlock = new TextVertex[text.Length * 6];
            fixed (TextVertex* memBlockPtr = memBlock)
            {
                TextVertex* mem = memBlockPtr;
                foreach (var thing in layout.Stuff)
                {
                    var width = thing.Width;
                    var height = thing.Height;
                    var region = new Vector4(thing.SourceX, thing.SourceY, width, height) / atlasWidth;
                    var origin = new Vector2(thing.DestX, thing.DestY);
                    *mem++ = new TextVertex(origin + new Vector2(0, height), new Vector2(region.X, region.Y + region.W), color);
                    *mem++ = new TextVertex(origin + new Vector2(width, height), new Vector2(region.X + region.Z, region.Y + region.W), color);
                    *mem++ = new TextVertex(origin + new Vector2(width, 0), new Vector2(region.X + region.Z, region.Y), color);
                    *mem++ = new TextVertex(origin, new Vector2(region.X, region.Y), color);
                    _count++;
                }
            }

            _vb?.Dispose();
            _vb = _rc.ResourceFactory.CreateVertexBuffer(memBlock, new VertexDescriptor(TextVertex.SizeInBytes, 3), false);
        }

        public void Render(TextureAtlas atlas, ConstantBufferDataProvider atlasInfo)
        {
            var previousBlendState = _rc.BlendState;
            _rc.VertexBuffer = _vb;
            _rc.IndexBuffer = _ib;
            _rc.Material = _material;
            _constantBindings.ApplyPerObjectInput(atlasInfo);
            _rc.SetTexture(0, atlas.TextureBinding);
            _rc.BlendState = _rc.AlphaBlend;
            _rc.DrawIndexedPrimitives(_count * 6, 0);
            _rc.BlendState = previousBlendState;
        }

        private Material CreateMaterial(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            Shader vs = factory.CreateShader(ShaderType.Vertex, "text-vertex");
            Shader fs = factory.CreateShader(ShaderType.Fragment, "text-fragment");
            VertexInputLayout inputLayout = factory.CreateInputLayout(vs, new MaterialVertexInput(TextVertex.SizeInBytes,
                new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float2),
                new MaterialVertexInputElement("in_texCoords", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2),
                new MaterialVertexInputElement("in_color", VertexSemanticType.Color, VertexElementFormat.Byte4)));
            ShaderSet shaderSet = factory.CreateShaderSet(inputLayout, vs, fs);
            _constantBindings = factory.CreateShaderConstantBindings(rc, shaderSet,
                new MaterialInputs<MaterialGlobalInputElement>(new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, "ProjectionMatrix")),
                new MaterialInputs<MaterialPerObjectInputElement>(
                    new MaterialPerObjectInputElement("AtlasInfo", MaterialInputType.Custom, 16)));
            ShaderTextureBindingSlots textureSlots = factory.CreateShaderTextureBindingSlots(shaderSet, new MaterialTextureInputs(new ManualTextureInput("FontAtlas")));
            Material material = new Material(rc, shaderSet, _constantBindings, textureSlots);

            return material;
        }
    }

    internal struct TextVertex
    {
        public const byte SizeInBytes = 20;

        public Vector2 Position;
        public Vector2 TexCoords;
        public RgbaByte Color;

        public TextVertex(Vector2 position, Vector2 texcoords, RgbaByte color)
        {
            Position = position;
            TexCoords = texcoords;
            Color = color;
        }

        public TextVertex(Vector2 position, Vector2 texcoords, int color)
        {
            Position = position;
            TexCoords = texcoords;
            Color = new RgbaByte((uint)color);
        }
    }
}
