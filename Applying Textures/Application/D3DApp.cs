using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//SharpDX namespaces

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

// resolve using conflicts
using Buffer = SharpDX.Direct3D11.Buffer;

using Common;
using System.Windows.Forms;
using SharpDX.Windows;
namespace Applying_Textures.Application
{
    public class D3DApp : D3DApplicationDesktop
    {
        Texture2D texture2D;

        //Vertex shader
        ShaderBytecode vsByteCode;
        VertexShader vsShader;

        //Pixel shader

        ShaderBytecode psByteCode;
        PixelShader psShader;

        //The vertex layout for IA
        InputLayout vsLayout;


        //A buffer to update the constant buffer
        Buffer mvpBuffer;

        //Depth Stencil state
        DepthStencilState depthStencilState;

        // A vertex shader that gives depth info to pixel shader
        ShaderBytecode depthVertexShaderBytecode;
        VertexShader depthVertexShader;

        // A pixel shader that renders the depth (black closer, white further away)
        ShaderBytecode depthPixelShaderBytecode;
        PixelShader depthPixelShader;


        // Matricies

        Matrix M, V, P;

        Action UpdateText;
        int lastX, lastY;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="window">The winform</param>
        public D3DApp(Form window, bool showFps = true, bool showText = true)
            : base(window)
        {
            ShowFPS = showFps;
            ShowText = showText;
        }
        public bool ShowFPS
        {
            get;
            private set;
        }
        public bool ShowText
        {
            get;
            private set;
        }
        public override void Run()
        {
            // Create and Initialize the axis lines renderer
            var axisLines = ToDispose(new AxisLinesRenderer { TextureName = "Texture.png" });
            axisLines.Initialize(this);

            // Create and Initialize the axis lines renderer
            var triangle = ToDispose(new TriangleRenderer { TextureName = "Texture2.png" });
            triangle.Initialize(this);

            //// Create and Initialize the axis lines renderer
            var quad = ToDispose(new QuadRenderer { TextureName = "Texture.png" });
            quad.Initialize(this);

            //// Create and Initialize the axis lines renderer
            var sphere = ToDispose(new SphereRenderer(Vector3.Zero, .25f) { TextureName = "Texture.png" });
            sphere.Initialize(this);

            //// FPS renderer
            FpsRenderer fps = null;
            if (ShowFPS)
            {
                fps = ToDispose(new Common.FpsRenderer("Calibri", Color.CornflowerBlue, new Point(8, 8), 16));
                fps.Initialize(this);
            }

            //// Text renderer
            Common.TextRenderer textRenderer = null;
            if (ShowText)
            {
                textRenderer = ToDispose(new Common.TextRenderer("Calibri", Color.CornflowerBlue, new Point(8, 30), 12));
                textRenderer.Initialize(this);

                UpdateText = () =>
                {
                    textRenderer.Text =
                        String.Format("World rotation ({0}) (Up/Down Left/Right Wheel+-)\nView ({1}) (A/D, W/S, Shift+Wheel+-)"
                        + "\nPress X to reinitialize the device and resources (device ptr: {2})"
                        + "\nPress Z to show/hide depth buffer",
                            rotation,
                            V.TranslationVector,
                            DeviceManager.Direct3DDevice.NativePointer);
                };

                UpdateText();

            }

            InitializeMatricies();
            Window.Resize += Window_Resize;
            Window.KeyDown += Window_KeyDown;
            Window.KeyUp += Window_KeyUp;
            Window.MouseWheel += Window_MouseWheel;
            Window.MouseDown += Window_MouseDown;
            Window.MouseMove += Window_MouseMove;
            //sphere.Show = false;
            RenderLoop.Run(Window, () =>
            {
                // Clear DSV
                DeviceManager.Direct3DContext.ClearDepthStencilView(DepthStencilView,
                                                                    DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
                                                                    1.0f, 0);
                // Clear RTV
                DeviceManager.Direct3DContext.ClearRenderTargetView(RenderTargetView, Color.White);

                // VP matrix
                var VP = Matrix.Multiply(V, P);

                // MVP
                var MVP = M * VP;

                // Must transpose to use in HLSL
                MVP.Transpose();

                // Write MVP to constant buffer
                DeviceManager.Direct3DContext.UpdateSubresource(ref MVP, mvpBuffer);

                // Render our primitives
                axisLines.Render();
                quad.Render();
                MVP = sphere.M * VP;
                MVP.Transpose();
                DeviceManager.Direct3DContext.UpdateSubresource(ref MVP, mvpBuffer);
                sphere.Render();
                var v = sphere.RotationAngles;
                v.Y += 0.016f;
                sphere.RotationAngles = v;
                triangle.Render();

                // FPS renderer
                if (fps != null)
                    fps.Render();

                // Text renderer
                if (textRenderer != null)
                    textRenderer.Render();

                Present();
            });



        }

        void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var yRotate = lastX - e.X;
                var xRotate = lastY - e.Y;
                lastY = e.Y;
                lastX = e.X;

                // Mouse move changes 
                V *= Matrix.RotationX(xRotate * moveFactor);
                V *= Matrix.RotationY(yRotate * moveFactor);

                UpdateText();
            }
        }

        void Window_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lastX = e.X;
                lastY = e.Y;
            }
        }
        void Window_MouseWheel(object sender, MouseEventArgs e)
        {
            if (shiftKey)
            {
                // Zoom in/out
                V.TranslationVector -= new Vector3(0f, 0f, (e.Delta / 120f) * moveFactor * 2);
            }
            else
            {
                // rotate around Z-axis
                V *= Matrix.RotationZ((e.Delta / 120f) * moveFactor);
                rotation += new Vector3(0f, 0f, (e.Delta / 120f) * moveFactor);
            }
            if (ShowText)
                UpdateText();
        }

        void Window_KeyUp(object sender, KeyEventArgs e)
        {
            // Clear the shift/ctrl keys so they aren't sticky
            if (e.KeyCode == Keys.ShiftKey)
                shiftKey = false;
            if (e.KeyCode == Keys.ControlKey)
                ctrlKey = false;
        }
        float moveFactor = 0.02f; // how much to change on each keypress
        bool shiftKey = false;
        bool ctrlKey = false;
        bool useDepthShaders = false;

        Vector3 rotation = Vector3.Zero;

        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            shiftKey = e.Shift;
            ctrlKey = e.Control;

            switch (e.KeyCode)
            {
                // WASD -> pans view
                case Keys.A:
                    V.TranslationVector += new Vector3(moveFactor * 2, 0f, 0f);
                    break;
                case Keys.D:
                    V.TranslationVector -= new Vector3(moveFactor * 2, 0f, 0f);
                    break;
                case Keys.S:
                    V.TranslationVector += new Vector3(0f, shiftKey ? moveFactor * 2 : 0, shiftKey ? 0f : moveFactor * 2);
                    break;
                case Keys.W:
                    V.TranslationVector -= new Vector3(0f, shiftKey ? moveFactor * 2 : 0, shiftKey ? 0f : moveFactor * 2);
                    break;
                // Up/Down and Left/Right - rotates around X / Y respectively
                // (Mouse wheel rotates around Z)
                case Keys.Down:
                    M *= Matrix.RotationX(-moveFactor);
                    rotation -= new Vector3(moveFactor, 0f, 0f);
                    break;
                case Keys.Up:
                    M *= Matrix.RotationX(moveFactor);
                    rotation += new Vector3(moveFactor, 0f, 0f);
                    break;
                case Keys.Left:
                    M *= Matrix.RotationY(-moveFactor);
                    rotation -= new Vector3(0f, moveFactor, 0f);
                    break;
                case Keys.Right:
                    M *= Matrix.RotationY(moveFactor);
                    rotation += new Vector3(0f, moveFactor, 0f);
                    break;

                case Keys.X:
                    // To test for correct resource recreation
                    // Simulate device reset or lost.
                    System.Diagnostics.Debug.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
                    DeviceManager.Initialize(DeviceManager.Dpi);
                    System.Diagnostics.Debug.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
                    break;
                case Keys.Z:
                    var context = DeviceManager.Direct3DContext;
                    useDepthShaders = !useDepthShaders;
                    if (useDepthShaders)
                    {
                        context.VertexShader.Set(depthVertexShader);
                        context.PixelShader.Set(depthPixelShader);
                    }
                    else
                    {
                        context.VertexShader.Set(vsShader);
                        context.PixelShader.Set(psShader);
                    }
                    break;
            }
        }

        private void Window_Resize(object sender, EventArgs e)
        {
            //Maintain correct aspect ratio
            P = Matrix.PerspectiveFovLH((float)Math.PI / 3f, Width / (float)Height, 0.5f, 100f);
        }

        protected override SwapChainDescription1 CreateSwapChainDescription()
        {
            // Multi sample anti aliasing MSAA
            var sc = base.CreateSwapChainDescription();
            sc.SampleDescription.Count = 4;
            sc.SampleDescription.Quality = 0;
            return sc;
        }

        // Handler for DeviceManager.OnInitialize
        protected override void CreateDeviceDependentResources(DeviceManager deviceManager)
        {
            base.CreateDeviceDependentResources(deviceManager);

            // First release all ressources
            RemoveAndDispose(ref texture2D);
            RemoveAndDispose(ref vsByteCode);
            RemoveAndDispose(ref vsShader);
            RemoveAndDispose(ref psByteCode);
            RemoveAndDispose(ref psShader);
            RemoveAndDispose(ref vsLayout);
            RemoveAndDispose(ref depthStencilState);
            RemoveAndDispose(ref mvpBuffer);

            ShaderFlags flag = ShaderFlags.None;
#if DEBUG
            flag = ShaderFlags.Debug;
#endif
            var device = deviceManager.Direct3DDevice;
            var context = deviceManager.Direct3DContext;

            // Compile and create vs shader 
            vsByteCode = ToDispose(ShaderBytecode.CompileFromFile("Shaders/SimpleTexture.hlsl", "VSMain", "vs_5_0", flag));
            vsShader = ToDispose(new VertexShader(device, vsByteCode));

            // Compile and create ps shader 
            psByteCode = ToDispose(ShaderBytecode.CompileFromFile("Shaders/SimpleTexture.hlsl", "PSMain", "ps_5_0", flag));
            psShader = ToDispose(new PixelShader(device, psByteCode));

            // Compile and create the depth vertex and pixel shaders
            // These shaders are for checking what the depth buffer should look like
            //depthVertexShaderBytecode = ToDispose(ShaderBytecode.CompileFromFile("Depth.hlsl", "VSMain", "vs_5_0", flag));
            //depthVertexShader = ToDispose(new VertexShader(device, depthVertexShaderBytecode));
            //depthPixelShaderBytecode = ToDispose(ShaderBytecode.CompileFromFile("Depth.hlsl", "PSMain", "ps_5_0", flag));
            //depthPixelShader = ToDispose(new PixelShader(device, depthPixelShaderBytecode));

            // Initialize vertex layout to match vs input structure
            // Input structure definition
            var input = new[]
            { 
                // Position
                new InputElement("SV_Position",0,Format.R32G32B32A32_Float,0,0),
                // Color
                //new InputElement("COLOR",0,Format.R32G32B32A32_Float,16,0),
                // Texture coords
                new InputElement("TEXCOORD", 0,Format.R32G32_Float,16,0)
            };
            vsLayout = ToDispose(new InputLayout(device, vsByteCode.GetPart(ShaderBytecodePart.InputSignatureBlob), input));

            // Create the constant buffer to store the MVP matrix

            mvpBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            // Create depth stencil state for OM

            depthStencilState = ToDispose(new DepthStencilState(device, new DepthStencilStateDescription
            {
                IsDepthEnabled = true,
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.All,
                IsStencilEnabled = false,
                StencilReadMask = 0xff, // no mask
                StencilWriteMask = 0xff,
                // Face culling
                FrontFace = new DepthStencilOperationDescription
                {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Increment,
                    FailOperation = StencilOperation.Keep
                },

                BackFace = new DepthStencilOperationDescription
                {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Decrement,
                    FailOperation = StencilOperation.Keep
                },


            }));

            // Tell IA what vertices will look like
            context.InputAssembler.InputLayout = vsLayout;

            // Bind buffers to vs
            context.VertexShader.SetConstantBuffer(0, mvpBuffer);

            // Set vs to run
            context.VertexShader.Set(vsShader);

            // Set pixel shader to run
            context.PixelShader.Set(psShader);

            // Set depth stencil to OM
            context.OutputMerger.DepthStencilState = depthStencilState;

            InitializeMatricies();

        }
        private void InitializeMatricies()
        {
            // Prepare Matricies

            // World matrix
            M = Matrix.Identity;

            // View matrix
            var camPos = new Vector3(1, 1, -2);
            var camLookAt = Vector3.Zero;
            var camUp = Vector3.UnitY;

            V = Matrix.LookAtLH(camPos, camLookAt, camUp);


            // Projection matrix
            P = Matrix.PerspectiveFovLH((float)Math.PI / 3f, Width / (float)Height, 0.5f, 100f);



        }
        // Handler for DeviceManager.OnSizeChanged
        protected override void CreateSizeDependentResources(D3DApplicationBase app)
        {
            base.CreateSizeDependentResources(app);
            InitializeMatricies();

        }
    }
}
