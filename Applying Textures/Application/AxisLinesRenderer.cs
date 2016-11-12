using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Applying_Textures.Application
{
    class AxisLinesRenderer : PrimitivesRenderer
    {
        protected override void CreateVertexBinding()
        {
            var device = DeviceManager.Direct3DDevice;

            // Create vertex buffer for IA

            // Create xyz axis : X red, Y green , Z blue
            // data
            var data = new[]{

            /*  Vertex Position         Texture UV */
                                        // ~45x10
                -1f, 0f, 0f, 1f,        0.1757f, 0.039f, // - x-axis 
                1f, 0f, 0f, 1f,         0.1757f, 0.039f,  // + x-axis
                0.9f, -0.05f, 0f, 1f,   0.1757f, 0.039f,// arrow head start
                1f, 0f, 0f, 1f,         0.1757f, 0.039f,
                0.9f, 0.05f, 0f, 1f,    0.1757f, 0.039f,
                1f, 0f, 0f, 1f,         0.1757f, 0.039f,  // arrow head end
                                        // ~135x35
                0f, -1f, 0f, 1f,        0.5273f, 0.136f, // - y-axis
                0f, 1f, 0f, 1f,         0.5273f, 0.136f,  // + y-axis
                -0.05f, 0.9f, 0f, 1f,   0.5273f, 0.136f,// arrow head start
                0f, 1f, 0f, 1f,         0.5273f, 0.136f,
                0.05f, 0.9f, 0f, 1f,    0.5273f, 0.136f,
                0f, 1f, 0f, 1f,         0.5273f, 0.136f,  // arrow head end
                                        // ~220x250
                0f, 0f, -1f, 1f,        0.859f, 0.976f, // - z-axis
                0f, 0f, 1f, 1f,         0.859f, 0.976f,  // + z-axis
                0f, -0.05f, 0.9f, 1f,   0.859f, 0.976f,// arrow head start
                0f, 0f, 1f, 1f,         0.859f, 0.976f,
                0f, 0.05f, 0.9f, 1f,    0.859f, 0.976f,
                0f, 0f, 1f, 1f,         0.859f, 0.976f,  // arrow head end

            };
            buffer_ = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, data));
            vertexBinding_ = new VertexBufferBinding(buffer_, Utilities.SizeOf<Vector4>() * 2, 0);
            PrimitiveCount = data.Length / 2;
        }
        protected override int PrimitiveCount
        {
            get;
            set;
        }
        protected override SharpDX.Direct3D.PrimitiveTopology PrimitiveTopology
        {
            get { return SharpDX.Direct3D.PrimitiveTopology.LineList; }
        }
        //protected override void DoRender()
        //{
        //    var context = DeviceManager.Direct3DContext;

        //    // Tell IA to use lines
        //    context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;

        //    // Pass the lines vertices
        //    context.InputAssembler.SetVertexBuffers(0, vertexBinding_);

        //    // Draw our xyz axis : 18 vertices
        //    context.Draw(18, 0);
        //}
    }
}
