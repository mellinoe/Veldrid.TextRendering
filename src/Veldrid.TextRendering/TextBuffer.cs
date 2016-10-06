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
        private int _filledIndexCount;
        private int _count;
        private readonly RenderContext _rc;
        private ShaderConstantBindings _constantBindings;

        public TextBuffer(RenderContext rc)
        {
            _rc = rc;
            _ib = rc.ResourceFactory.CreateIndexBuffer(600, false);
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

            TextVertex[] memBlock = new TextVertex[text.Length * 4];
            int index = 0;
            foreach (var thing in layout.Stuff)
            {
                var width = thing.Width;
                var height = thing.Height;
                var region = new Vector4(thing.SourceX, thing.SourceY, width, height) / atlasWidth;
                var origin = new Vector2(thing.DestX, thing.DestY);
                memBlock[index++] = new TextVertex(origin + new Vector2(0, height), new Vector2(region.X, region.Y + region.W), color);
                memBlock[index++] = new TextVertex(origin + new Vector2(width, height), new Vector2(region.X + region.Z, region.Y + region.W), color);
                memBlock[index++] = new TextVertex(origin + new Vector2(width, 0), new Vector2(region.X + region.Z, region.Y), color);
                memBlock[index++] = new TextVertex(origin, new Vector2(region.X, region.Y), color);
                _count++;
            }

            _vb?.Dispose();
            _vb = _rc.ResourceFactory.CreateVertexBuffer(memBlock, new VertexDescriptor(TextVertex.SizeInBytes, 3), false);
            EnsureIndexCapacity(_count);
        }

        public void Clear()
        {
            _count = 0;
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
                    new MaterialPerObjectInputElement("AtlasInfoBuffer", MaterialInputType.Custom, 16)));
            ShaderTextureBindingSlots textureSlots = factory.CreateShaderTextureBindingSlots(shaderSet, new MaterialTextureInputs(new ManualTextureInput("FontAtlas")));
            Material material = new Material(rc, shaderSet, _constantBindings, textureSlots);

            return material;
        }

        private unsafe void EnsureIndexCapacity(int capacity)
        {
            int needed = capacity - _filledIndexCount;
            if (needed > 0)
            {
                ushort[] indices = new ushort[needed * 6];
                for (int i = 0, v = _filledIndexCount * 4; i < needed; i++, v += 4)
                {
                    indices[i * 6 + 0] = (ushort)(v + 0);
                    indices[i * 6 + 1] = (ushort)(v + 2);
                    indices[i * 6 + 2] = (ushort)(v + 1);
                    indices[i * 6 + 3] = (ushort)(v + 2);
                    indices[i * 6 + 4] = (ushort)(v + 0);
                    indices[i * 6 + 5] = (ushort)(v + 3);
                }

                _ib.SetIndices(indices, IndexFormat.UInt16, 0, _filledIndexCount * 6);
                _filledIndexCount = capacity;
            }
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
