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
using System.Runtime.InteropServices;
namespace Skybox.Application
{
    public class D3DApp : D3DApplicationDesktop
    {
        ShaderResourceView texture2D;

        #region shaders
        // Vertex shader
        VertexShader vsShader;
        // Simple pixel shader
        PixelShader psShader;
        // Depth pixel shader
        PixelShader depthPixelShader;
        // Lambert pixel shader
        PixelShader diffuseShader;
        // Phong pixel shader
        PixelShader phongShader;
        // Blinn-Phong pixel shader
        PixelShader blinnShader;

        #endregion
        //The vertex layout for IA
        InputLayout vsLayout;


        //A buffer to update the constant buffer
        Buffer perObjectcBuffer;

        //Depth Stencil state
        DepthStencilState depthStencilState;


        private Buffer perFramecBuffer;
        private Buffer perMaterialcBuffer;
        private Buffer perSkyBox;

        bool lightDirOn;
        bool pointLightOn;
        bool spotLightOn;

        // Matricies

        Matrix M, V, P;

        Action UpdateText;
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
            InitializeMatricies();

            #region renderers
            // Create and Initialize the axis lines renderer
            var axisLines = ToDispose(new AxisLinesRenderer());
            axisLines.Initialize(this);

            // Create and Initialize the axis lines renderer
            var cube = ToDispose(new CubeRenderer());
            cube.Initialize(this);
            cube.World = Matrix.Translation(-1, 0, 0);

            //// Create and Initialize the axis lines renderer
            var quad = ToDispose(new QuadRenderer());
            quad.Initialize(this);
            quad.World = Matrix.Scaling(5f);
            quad.World.TranslationVector = new Vector3(0, -.5f, 0);


            // Create and initialize a sphere
            var sphere = ToDispose(new SphereRenderer1());
            sphere.Initialize(this);
            sphere.World = Matrix.Translation(0, 0, 1.1f);

            // Create the skybox cube
            var skyBox = ToDispose(new SkyBox());
            skyBox.Initialize(this);

            #endregion

            # region fps and text
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
            #endregion

            #region events handlers

            Window.Resize += Window_Resize;
            Window.KeyDown += Window_KeyDown; // global events
            Window.KeyDown += (s, e) => // local events
            {
                switch (e.KeyCode)
                {
                    case Keys.NumPad1:
                        axisLines.Show = !axisLines.Show;
                        break;
                    case Keys.NumPad2:
                        quad.Show = !quad.Show;
                        break;
                    case Keys.NumPad3:
                        cube.Show = !cube.Show;
                        break;
                    case Keys.NumPad4:
                        sphere.Show = !sphere.Show;
                        break;
                }
            };
            Window.KeyUp += Window_KeyUp;
            Window.MouseWheel += Window_MouseWheel;
            var lastX = 0;
            var lastY = 0;

            Window.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    lastX = e.X;
                    lastY = e.Y;
                }
            };

            Window.MouseMove += (s, e) =>
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
            };
            #endregion

            var clock = new System.Diagnostics.Stopwatch();
            clock.Start();
            var perFrame = new ConstantBuffer.PerFrame { };
            var skyBoxState= ToDispose(new RasterizerState(DeviceManager.Direct3DDevice, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsFrontCounterClockwise = false
            }));
            var normalState = ToDispose(new RasterizerState(DeviceManager.Direct3DDevice, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = false
            }));
            lightDirOn = pointLightOn = spotLightOn = true;
            RenderLoop.Run(Window, () =>
            {
                #region clear
                // Clear DSV
                DeviceManager.Direct3DContext.ClearDepthStencilView(DepthStencilView,
                                                                    DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
                                                                    1.0f, 0);
                // Clear RTV
                DeviceManager.Direct3DContext.ClearRenderTargetView(RenderTargetView, Color.White);
                #endregion

                #region initialize postions and matricies
                // VP matrix
                var VP = Matrix.Multiply(V, P);
                // Extract camera postion from view matrix
                var camPosition = Matrix.Transpose(Matrix.Invert(V)).Column4;
                var cameraPosition = new Vector3(camPosition.X, camPosition.Y, camPosition.Z);

                var time = clock.ElapsedMilliseconds / 1000.0f;
                if (ctrlKey)
                {
                    VP = Matrix.RotationY(time * 1.8f) * Matrix.RotationX(time * 1f) * Matrix.RotationZ(time * 0.6f) * VP;
                }
                #endregion

                #region Update per frame constant buffer

                perFrame.CameraPosition = cameraPosition;

                // Configure lights
                perFrame.Light0.Color = Color.White;
                perFrame.Light0.On = lightDirOn ? 1u : 0u;

                perFrame.Light1.Color = Color.Yellow;
                perFrame.Light1.On = pointLightOn ? 1u : 0u;
                perFrame.Light2.Color = Color.Fuchsia;
                perFrame.Light2.On = spotLightOn ? 1u : 0u;
                var ligthMat = Matrix.RotationY(360 * time / 1000);
                var lightDir = Vector3.Transform(new Vector3(1f, 1f, 1f), M * ligthMat);
                var lightDir2 = Vector3.Transform(new Vector3(2, 2, -2), M * ligthMat);
                perFrame.Light0.Direction = new Vector3(lightDir.X, lightDir.Y, lightDir.Z);
                perFrame.Light1.Direction = new Vector3(lightDir2.X, lightDir2.Y, lightDir2.Z);
                perFrame.Light2.Direction = new Vector3(lightDir.X, lightDir.Y, lightDir.Z);
                DeviceManager.Direct3DContext.UpdateSubresource(ref perFrame, perFramecBuffer);
                #endregion

                #region Update per Material constant buffer

                var perMaterial = new ConstantBuffer.PerMaterial
                {
                    Ambient = new Color4(.2f),
                    Diffuse = Color.White,
                    Emissive = Color.Black,
                    Specular = Color.White,
                    Shininess = 10f,
                    HasTexture = 0,
                    UVTransform = Matrix.Identity
                };
                DeviceManager.Direct3DContext.UpdateSubresource(ref perMaterial, perMaterialcBuffer);
                #endregion

                // Render our primitives

                // skybox
                var v = Vector3.Transform(V.TranslationVector, V);
                var W = skyBox.World* Matrix.Translation(cameraPosition)* M;
                var perObject = new ConstantBuffer.PerObject
                {
                    MVP = W * VP,
                    M = W,
                    N = Matrix.Transpose(Matrix.Invert(W)),
                };

                perObject.Transpose();
                DeviceManager.Direct3DContext.UpdateSubresource(ref perObject, perObjectcBuffer);
                var drawSkybox = new ConstantBuffer.DrawSkyBox { On = 1 };
                DeviceManager.Direct3DContext.UpdateSubresource(ref drawSkybox, perSkyBox);

                DeviceManager.Direct3DContext.Rasterizer.State = skyBoxState;

                skyBox.Render();

                perMaterial.HasTexture = 0;
                drawSkybox.On = 0;
                DeviceManager.Direct3DContext.UpdateSubresource(ref drawSkybox, perSkyBox);
                DeviceManager.Direct3DContext.Rasterizer.State = normalState;

                // axis lines
                perObject.M = axisLines.World * M;
                perObject.N = Matrix.Transpose(Matrix.Invert(perObject.M));
                perObject.MVP = perObject.M * VP;
                perObject.Transpose();
                DeviceManager.Direct3DContext.UpdateSubresource(ref perObject, perObjectcBuffer);
                axisLines.Render();

                //quad
                perObject.M = quad.World * M;
                perObject.N = Matrix.Transpose(Matrix.Invert(perObject.M));
                perObject.MVP = perObject.M * VP;
                perObject.Transpose();
                DeviceManager.Direct3DContext.UpdateSubresource(ref perObject, perObjectcBuffer);
                quad.Render();

                // cube
                perObject.M = cube.World * M;
                perObject.N = Matrix.Transpose(Matrix.Invert(perObject.M));
                perObject.MVP = perObject.M * VP;
                perObject.Transpose();
                DeviceManager.Direct3DContext.UpdateSubresource(ref perObject, perObjectcBuffer);
                cube.Render();

                // sphere
                perObject.M = sphere.World * M;
                perObject.N = Matrix.Transpose(Matrix.Invert(perObject.M));
                perObject.MVP = perObject.M * VP;
                perObject.Transpose();
                DeviceManager.Direct3DContext.UpdateSubresource(ref perObject, perObjectcBuffer);
                sphere.Render();

                // FPS renderer
                if (fps != null)
                    fps.Render();

                // Text renderer
                if (textRenderer != null)
                    textRenderer.Render();

                Present();
            });



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
                        context.PixelShader.Set(depthPixelShader);
                    }
                    else
                    {
                        context.VertexShader.Set(vsShader);
                        context.PixelShader.Set(psShader);
                    }
                    break;

                case Keys.D1:
                    DeviceManager.Direct3DContext.PixelShader.Set(psShader);
                    break;
                case Keys.D2:
                    DeviceManager.Direct3DContext.PixelShader.Set(diffuseShader);
                    break;
                case Keys.D3:
                    DeviceManager.Direct3DContext.PixelShader.Set(phongShader);
                    break;
                case Keys.D4:
                    DeviceManager.Direct3DContext.PixelShader.Set(blinnShader);
                    break;
                case Keys.D5:
                    lightDirOn = !lightDirOn;
                    break;
                case Keys.D6:
                    pointLightOn = !pointLightOn;
                    break;
                case Keys.D7:
                    spotLightOn = !spotLightOn;
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

            RemoveAndDispose(ref vsShader);
            RemoveAndDispose(ref psShader);
            RemoveAndDispose(ref diffuseShader);
            RemoveAndDispose(ref phongShader);
            RemoveAndDispose(ref blinnShader);
            RemoveAndDispose(ref depthPixelShader);

            RemoveAndDispose(ref vsLayout);

            RemoveAndDispose(ref depthStencilState);
            RemoveAndDispose(ref perObjectcBuffer);
            RemoveAndDispose(ref perFramecBuffer);
            RemoveAndDispose(ref perMaterialcBuffer);
            RemoveAndDispose(ref perSkyBox);

            //            ShaderFlags flag = ShaderFlags.None;
            //#if DEBUG
            //            flag = ShaderFlags.Debug;
            //#endif
            var device = deviceManager.Direct3DDevice;
            var context = deviceManager.Direct3DContext;

            // Compile and create vs shader 
            using (var vsByteCode = HLSLCompiler.CompileFromFile("Shaders/VS.hlsl", "VSMain", "vs_5_0"))
            {
                vsShader = ToDispose(new VertexShader(device, vsByteCode));
                var input = new[]
                { 
                    // Position
                    new InputElement("SV_Position",0,Format.R32G32B32_Float,0,0),
                    // Normal
                    new InputElement("NORMAL",0,Format.R32G32B32_Float,12,0),
                    // Color
                    new InputElement("COLOR",0,Format.R8G8B8A8_UNorm,24,0),
                    // Texture
                    new InputElement("TEXCOORD",0, Format.R32G32_Float,28,0),

                };
                // Initialize vertex layout to match vs input structure
                // Input structure definition

                vsLayout = ToDispose(new InputLayout(device, vsByteCode.GetPart(ShaderBytecodePart.InputSignatureBlob), input));

            }


            using (var psByteCode = HLSLCompiler.CompileFromFile("Shaders/Simple.hlsl", "PSMain", "ps_5_0"))
                psShader = ToDispose(new PixelShader(device, psByteCode));

            using (var psByteCode = HLSLCompiler.CompileFromFile("Shaders/Diffuse.hlsl", "PSMain", "ps_5_0"))
                diffuseShader = ToDispose(new PixelShader(device, psByteCode));

            using (var psByteCode = HLSLCompiler.CompileFromFile("Shaders/PhongSkyBox.hlsl", "PSMain", "ps_5_0"))
                phongShader = ToDispose(new PixelShader(device, psByteCode));

            using (var psByteCode = HLSLCompiler.CompileFromFile("Shaders/BlinnPhong.hlsl", "PSMain", "ps_5_0"))
                blinnShader = ToDispose(new PixelShader(device, psByteCode));


            using (var dsBytecode = HLSLCompiler.CompileFromFile("Shaders/DepthPS.hlsl", "PSMain", "ps_5_0"))
                depthPixelShader = ToDispose(new PixelShader(device, dsBytecode));

            // Create depth stencil state for OM

            depthStencilState = ToDispose(new DepthStencilState(device, new DepthStencilStateDescription
            {
                IsDepthEnabled = true,
                DepthComparison = Comparison.LessEqual,
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

            // Create the constants buffers
            var s = Marshal.SizeOf(typeof(ConstantBuffer.PerFrame));
            var sz = Utilities.SizeOf<ConstantBuffer.DrawSkyBox>();
            perObjectcBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<ConstantBuffer.PerObject>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            perFramecBuffer = ToDispose(new Buffer(device, /*Utilities.SizeOf<ConstantBuffer.PerFrame>()*/s, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            perMaterialcBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<ConstantBuffer.PerMaterial>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            perSkyBox = ToDispose(new Buffer(device, Utilities.SizeOf<ConstantBuffer.DrawSkyBox>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            // Bind buffers to vs
            context.VertexShader.SetConstantBuffer(0, perObjectcBuffer);
            context.VertexShader.SetConstantBuffer(1, perFramecBuffer);
            context.VertexShader.SetConstantBuffer(2, perMaterialcBuffer);
            context.VertexShader.SetConstantBuffer(3, perSkyBox);
            context.PixelShader.SetConstantBuffer(3, perSkyBox);

            // Set vs to run
            context.VertexShader.Set(vsShader);

            // Set pixel shader to run
            context.PixelShader.SetConstantBuffer(1, perFramecBuffer);
            context.PixelShader.SetConstantBuffer(2, perMaterialcBuffer);
            context.PixelShader.Set(diffuseShader);

            // Set depth stencil to OM
            context.OutputMerger.DepthStencilState = depthStencilState;

            // Back face culling

            //context.Rasterizer.State = ToDispose(new RasterizerState(device, new RasterizerStateDescription
            //{
            //    FillMode = FillMode.Solid,
            //    CullMode = CullMode.Back,
            //    IsFrontCounterClockwise = false
            //}));
            InitializeMatricies();

        }
        private void InitializeMatricies()
        {
            // Prepare Matricies

            // World matrix
            M = Matrix.Identity;

            // View matrix
            var camPos = new Vector3(1, 2, -5);
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
