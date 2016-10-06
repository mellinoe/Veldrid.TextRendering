using SharpFont;
using System;
using Veldrid.Graphics;

namespace Veldrid.TextRendering
{
    public class TextureAtlas : IGlyphAtlas, IDisposable
    {
        private readonly DeviceTexture2D _texture;
        private readonly ShaderTextureBinding _textureBinding;
        private readonly RenderContext _rc;

        private int _size;

        public int Width => _size;
        public int Height => _size;
        public DeviceTexture2D Texture => _texture;
        public ShaderTextureBinding TextureBinding => _textureBinding;

        public TextureAtlas(RenderContext rc, int size)
        {
            _size = size;
            _rc = rc;
            _texture = rc.ResourceFactory.CreateTexture(IntPtr.Zero, size, size, 1, PixelFormat.R8_UInt);
            _textureBinding = rc.ResourceFactory.CreateShaderTextureBinding(_texture);
        }

        public void Dispose() => _texture.Dispose();

        public void Insert(int page, int x, int y, int width, int height, IntPtr data)
        {
            if (page > 0)
            {
                throw new NotImplementedException();
            }

            _texture.SetTextureData(x, y, width, height, data, width * height);
        }
    }
}
