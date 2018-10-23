using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.ES20;
using OpenTK.Platform.Tizen;
using Tizen.Applications;
using SkiaSharp;

namespace OpentkClock
{
    class Program : TizenGameApplication
    {
        private int ProgramHandle;
        private int BitMapHeight;
        private int BitMapWidth;
        private IntPtr BitMap;

        public Program()
        {
        }

        protected override void OnCreate()
        {
            Window.RenderFrame += OnRenderFrame;

            OnLoad();

            base.OnCreate();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }

        protected override void OnAppControlReceived(AppControlReceivedEventArgs e)
        {
            base.OnAppControlReceived(e);
        }

        protected override void OnTerminate()
        {
            base.OnTerminate();

            OnUnload();
        }

        // Load resources here.
        private void OnLoad()
        {
            InitShader();
            CreateBitmap();
        }

        private void OnUnload()
        {
            FreeBitmap();
        }

        // Called when it is time to render the next frame. Add your rendering code here.
        private void OnRenderFrame(Object sender, FrameEventArgs e)
        {
            float [] vertices = {
                1.0f, 1.0f, 0.5f,
                -1.0f, -1.0f, 0.5f,
                1.0f, -1.0f, 0.5f,
                1.0f, 1.0f, 0.5f,
                -1.0f, 1.0f, 0.5f,
                -1.0f, -1.0f, 0.5f,
            };
            float [] textures =
            {
                1.0f, 0.0f,
                0.0f, 1.0f,
                1.0f, 1.0f,
                1.0f, 0.0f,
                0.0f, 0.0f,
                0.0f, 1.0f,
            };

            DrawSkia();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Enable(All.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int a_position = GL.GetAttribLocation(ProgramHandle, "a_position");
            int a_texCoord = GL.GetAttribLocation(ProgramHandle, "a_texCoord");

            int s_texture = GL.GetUniformLocation(ProgramHandle, "s_texture");
            GL.Uniform1(s_texture, 0);

            GL.UseProgram(ProgramHandle);

            unsafe
            {
                fixed (float* pvertices = vertices)
                {
                    GL.VertexAttribPointer(a_position, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), new IntPtr(pvertices));
                    GL.EnableVertexAttribArray(a_position);
                }
                fixed (float* ptextures = textures)
                {
                    GL.VertexAttribPointer(a_texCoord, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), new IntPtr(ptextures));
                    GL.EnableVertexAttribArray(a_texCoord);
                }
            }

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            GL.Finish();

            GL.DisableVertexAttribArray(a_position);
            GL.DisableVertexAttribArray(a_texCoord);

            Window.SwapBuffers();
        }

        private int LoadShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            if (shader == 0)
                throw new InvalidOperationException("Unable to create shader:" + type.ToString());

            GL.ShaderSource(shader, 1, new string[] { source }, (int[])null);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int compiled);
            if (compiled == 0)
            {
                GL.GetShader(shader, ShaderParameter.InfoLogLength, out int length);
                if (length > 0)
                {
                    GL.GetShaderInfoLog(shader, length, out length, out string log);
                }
                GL.DeleteShader(shader);
                throw new InvalidOperationException("Unable to compile shader of type : " + type.ToString());
            }
            return shader;
        }

        private void InitShader()
        {
            string vertextSrc = 
                    "attribute vec4 a_position;                             \n" +
                    "attribute vec2 a_texCoord;                             \n" +
                    "varying vec2 v_texCoord;                               \n" +
                    "void main()                                            \n" +
                    "{                                                      \n" +
                    "   gl_Position = a_position;                           \n" +
                    "   v_texCoord = a_texCoord;                            \n" +
                    "}                                                      \n";

            string fragmentSrc =
                    "precision mediump float;                               \n" +
                    "varying vec2 v_texCoord;                               \n" +
                    "uniform sampler2D s_texture;                           \n" +
                    "void main()                                            \n" +
                    "{                                                      \n" +
                    "   gl_FragColor = texture2D(s_texture, v_texCoord);    \n" +
                    "}                                                      \n";

            int vertexShader = LoadShader(ShaderType.VertexShader, vertextSrc);
            int fragmentShader = LoadShader(ShaderType.FragmentShader, fragmentSrc);

            ProgramHandle = GL.CreateProgram();
            if (ProgramHandle == 0)
                throw new InvalidOperationException("Unable to create program");

            GL.AttachShader(ProgramHandle, vertexShader);
            GL.AttachShader(ProgramHandle, fragmentShader);

            GL.BindAttribLocation(ProgramHandle, 0, "a_position");
            GL.BindAttribLocation(ProgramHandle, 1, "a_texCoord");

            GL.LinkProgram(ProgramHandle);

            GL.Viewport(0, 0, Window.Width, Window.Height);
        }

        private void CreateBitmap()
        {
            BitMapHeight = Window.Height;
            BitMapWidth = Window.Width;
            BitMap = Marshal.AllocHGlobal(BitMapHeight * BitMapWidth * 4);
        }

        private void CreateTexture()
        {
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);

            GL.TexImage2D(All.Texture2D, 0, All.BgraExt, BitMapWidth, BitMapHeight, 0, All.BgraExt, All.UnsignedByte, BitMap);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)All.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)All.Linear);

            GL.GenerateMipmap(TextureTarget.Texture2D);
        }

        private void FreeBitmap()
        {
            if (BitMap != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(BitMap);
                BitMap = IntPtr.Zero;
            }
        }

        private void DrawSkia()
        {
            var info = new SKImageInfo(BitMapWidth, BitMapHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            using (var surface = SKSurface.Create(info, BitMap, BitMapWidth * 4))
            {
                if (surface == null)
                {
                    return;
                }

                DrawClock(surface.Canvas, BitMapWidth, BitMapHeight);
                surface.Canvas.Flush();
            }

            CreateTexture();
        }

        private void DrawClock(SKCanvas canvas, float w, float h)
        {
            float x = w / 2;
            float y = h / 2;
            DateTime now = DateTime.Now;

            SKPaint paintRect = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = 0x33F5F5DC,
            };
            canvas.DrawRect(0, 0, w, h, paintRect);

            SKPaint paintCircle = new SKPaint
            {
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 0.7f),
                Style = SKPaintStyle.Fill,
                Color = SKColors.Black,
                StrokeWidth = 1
            };
            canvas.DrawCircle(x, y, 300, paintCircle);
            
            // Hour
            paintCircle.Color = SKColors.White;
            canvas.Save();
            canvas.RotateDegrees(30 * now.Hour + (float)0.5 * now.Minute, x, y);
            canvas.DrawCircle(x, y - 210, 20, paintCircle);
            canvas.Restore();

            // Minute
            paintCircle.Color = SKColors.Orange;
            canvas.Save();
            canvas.RotateDegrees(6 * now.Minute + (float)0.1 * now.Second, x, y);
            canvas.DrawCircle(x, y - 250, 15, paintCircle);
            canvas.Restore();

            // Second
            paintCircle.Color = SKColors.OrangeRed;
            canvas.Save();
            canvas.RotateDegrees(6 * now.Second + (float)0.006 * now.Millisecond, x, y);
            canvas.DrawCircle(x, y - 280, 10, paintCircle);
            canvas.Restore();

            SKPaint paintText = new SKPaint
            {
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 0.7f),
                Style = SKPaintStyle.Fill,
                Color = SKColors.Beige,
                TextAlign = SKTextAlign.Center,
                TextSize = 100
            };
            canvas.DrawText(now.Hour.ToString("D2"), x, y - 40, paintText);
            canvas.DrawText(now.Minute.ToString("D2"), x, y + 50, paintText);
            canvas.DrawText(now.Second.ToString("D2"), x, y + 140, paintText);
        }

        [STAThread]
        static void Main(string[] args)
        {
            using (Program app = new Program() { GLMajor = 2, GLMinor = 0 })
            {
                app.Run(args);
            }
        }
    }
}
