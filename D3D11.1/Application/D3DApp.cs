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
namespace D3D11._1.Application
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


        // Matricies

        Matrix M, V, P;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="window">The winform</param>
        public D3DApp(Form window):base(window)
        {

        }

        public override void Run()
        {
            // Create and Initialize the axis lines renderer
            var axisLines = ToDispose(new AxisLinesRenderer());
            axisLines.Initialize(this);

            // Create and Initialize the axis lines renderer
            var triangle = ToDispose(new TriangleRenderer());
            triangle.Initialize(this);

            //// Create and Initialize the axis lines renderer
            var quad = ToDispose(new QuadRenderer());
            quad.Initialize(this);

            //// Create and Initialize the axis lines renderer
            var sphere = ToDispose(new SphereRenderer(Vector3.Zero, .25f));
            sphere.Initialize(this);

            //// FPS renderer
            //var fps = ToDispose(new FpsRenderer());

            //// Text renderer
            //var textRenderer = ToDispose(new Common.TextRenderer());
            InitializeMatricies();
            Window.Resize+=Window_Resize;

            RenderLoop.Run(Window, () =>
            {
                // Clear DSV
                DeviceManager.Direct3DContext.ClearDepthStencilView(DepthStencilView, 
                                                                    DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
                                                                    1.0f,0);
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

                //// FPS renderer
                //fps.Render();

                // Text renderer

                //textRenderer.Render();

                Present();
            });



        }

        private void Window_Resize(object sender, EventArgs e)
        {
            //Maintain correct aspect ratio
            P = Matrix.PerspectiveFovLH((float)Math.PI / 3f, Width / (float)Height, 0.5f, 100f);
        }

        protected override SwapChainDescription1 CreateSwapChainDescription()
        {
            return base.CreateSwapChainDescription();
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
            vsByteCode = ToDispose(ShaderBytecode.CompileFromFile("Shaders/Simple.hlsl", "VSMain", "vs_5_0", flag));
            vsShader = ToDispose(new VertexShader(device, vsByteCode));

            // Compile and create ps shader 
            psByteCode = ToDispose(ShaderBytecode.CompileFromFile("Shaders/Simple.hlsl", "PSMain", "ps_5_0", flag));
            psShader = ToDispose(new PixelShader(device, psByteCode));

            // Initialize vertex layout to match vs input structure
            // Input structure definition
            var input = new[]
            { 
                // Position
                new InputElement("SV_Position",0,Format.R32G32B32A32_Float,0,0),
                // Color
                new InputElement("COLOR",0,Format.R32G32B32A32_Float,16,0),
            };
            vsLayout = ToDispose(new InputLayout(device, vsByteCode.GetPart(ShaderBytecodePart.InputSignatureBlob), input));

            // Create the constant buffer to store the MVP matrix

            mvpBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            // Create depth stencil state for OM

            depthStencilState = ToDispose(new DepthStencilState(device, new DepthStencilStateDescription
            {
                IsDepthEnabled=true,
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.All,
                IsStencilEnabled =false,
                StencilReadMask =0xff, // no mask
                StencilWriteMask =0xff,
                // Face culling
                FrontFace = new DepthStencilOperationDescription { 
                    Comparison = Comparison.Always, 
                    PassOperation = StencilOperation.Keep, 
                    DepthFailOperation = StencilOperation.Increment, 
                    FailOperation= StencilOperation.Keep},

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
