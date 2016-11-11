using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace D3D11._1.Application
{
    class AxisLinesRenderer : Common.RendererBase
    {
        // Vertex buffer for axis lines
        Buffer axisLinesVertices;

        // Binding structure to the vertex buffer
        VertexBufferBinding axisLinesBinding;

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            // Dispose before creating
            RemoveAndDispose(ref axisLinesBinding);

            // The device
            var device = DeviceManager.Direct3DDevice;

            // Create vertex buffer for IA

            // Create xyz axis : X red, Y green , Z blue
            // data
            var data = new []{

            /*  Vertex Position                       Vertex Color */
                new Vector4(-1f, 0f, 0f, 1f), (Vector4)Color.Red, // - x-axis 
                new Vector4(1f, 0f, 0f, 1f), (Vector4)Color.Red,  // + x-axis
                new Vector4(0.9f, -0.05f, 0f, 1f), (Vector4)Color.Red,// arrow head start
                new Vector4(1f, 0f, 0f, 1f), (Vector4)Color.Red,
                new Vector4(0.9f, 0.05f, 0f, 1f), (Vector4)Color.Red,
                new Vector4(1f, 0f, 0f, 1f), (Vector4)Color.Red,  // arrow head end
                    
                new Vector4(0f, -1f, 0f, 1f), (Vector4)Color.Lime, // - y-axis
                new Vector4(0f, 1f, 0f, 1f), (Vector4)Color.Lime,  // + y-axis
                new Vector4(-0.05f, 0.9f, 0f, 1f), (Vector4)Color.Lime,// arrow head start
                new Vector4(0f, 1f, 0f, 1f), (Vector4)Color.Lime,
                new Vector4(0.05f, 0.9f, 0f, 1f), (Vector4)Color.Lime,
                new Vector4(0f, 1f, 0f, 1f), (Vector4)Color.Lime,  // arrow head end
                    
                new Vector4(0f, 0f, -1f, 1f), (Vector4)Color.Blue, // - z-axis
                new Vector4(0f, 0f, 1f, 1f), (Vector4)Color.Blue,  // + z-axis
                new Vector4(0f, -0.05f, 0.9f, 1f), (Vector4)Color.Blue,// arrow head start
                new Vector4(0f, 0f, 1f, 1f), (Vector4)Color.Blue,
                new Vector4(0f, 0.05f, 0.9f, 1f), (Vector4)Color.Blue,
                new Vector4(0f, 0f, 1f, 1f), (Vector4)Color.Blue,  // arrow head end

            };
            axisLinesVertices = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, data));
            axisLinesBinding = new VertexBufferBinding(axisLinesVertices, Utilities.SizeOf<Vector4>() * 2, 0);
        }
        protected override void DoRender()
        {
            var context = DeviceManager.Direct3DContext;

            // Tell IA to use lines
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;

            // Pass the lines vertices
            context.InputAssembler.SetVertexBuffers(0, axisLinesBinding);

            // Draw our xyz axis : 18 vertices
            context.Draw(18, 0);
        }
    }
}
