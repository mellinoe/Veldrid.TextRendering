using SharpFont;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Graphics;
using Veldrid.Graphics.Direct3D;
using Veldrid.Graphics.OpenGL;
using Veldrid.Platform;

namespace Veldrid.TextRendering
{
    public class Program
    {
        private const int AtlasWidth = 2048;
        private static DynamicDataProvider<Matrix4x4> _projection = new DynamicDataProvider<Matrix4x4>(Matrix4x4.Identity);
        private static DynamicDataProvider<Vector4> _fontAtlasInfo = new DynamicDataProvider<Vector4>(new Vector4(AtlasWidth));

        public static void Main(string[] args)
        {
            OpenTKWindowBase window = new DedicatedThreadWindow(960, 540, WindowState.Normal);
            RenderContext rc = CreateRenderContext(window);
            rc.RegisterGlobalDataProvider("ProjectionMatrix", _projection);
            FontFace fontFace;
            using (var fs = File.OpenRead(@"C:\Windows\Fonts\segoeui.ttf"))
            {
                fontFace = new FontFace(fs);
            }

            TextureAtlas atlas = new TextureAtlas(rc, AtlasWidth);
            TextBuffer textBuffer = new TextBuffer(rc);
            TextAnalyzer textAnalyzer = new TextAnalyzer(atlas);
            textBuffer.Append(textAnalyzer, fontFace, "Hello Veldrid.TextRenderer", 36, AtlasWidth, new RectangleF(50, 50, 400, 400));

            while (window.Exists)
            {
                window.GetInputSnapshot();
                Render(rc, window, textBuffer, atlas);
            }
        }

        private static void Render(RenderContext rc, Window window, TextBuffer textBuffer, TextureAtlas atlas)
        {
            rc.Viewport = new Viewport(0, 0, window.Width, window.Height);
            _projection.Data = Matrix4x4.CreateOrthographicOffCenter(0, window.Width, window.Height, 0, -1f, 1f);
            rc.ClearBuffer();
            textBuffer.Render(atlas, _fontAtlasInfo);
            rc.SwapBuffers();
        }

        private static RenderContext CreateRenderContext(OpenTKWindowBase window)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SharpDX.Direct3D11.DeviceCreationFlags creationFlags = SharpDX.Direct3D11.DeviceCreationFlags.None;
#if DEBUG
                creationFlags |= SharpDX.Direct3D11.DeviceCreationFlags.Debug;
#endif
                return new D3DRenderContext(window, creationFlags);
            }
            else
            {
                return new OpenGLRenderContext(window);
            }
        }
    }
}