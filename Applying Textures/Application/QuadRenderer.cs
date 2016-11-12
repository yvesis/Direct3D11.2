using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffer = SharpDX.Direct3D11.Buffer;
namespace Applying_Textures.Application
{
    class QuadRenderer : PrimitivesRenderer
    {
        // The quad index buffer
        Buffer quadIndices;
        protected override void CreateVertexBinding()
        {
            var data = new[]
            {
            /*  Vertex Position         texture UV */
                //0.25f, 0.5f, -0.5f, 1.0f,   0.0f, 0.0f, // Top-left
                //0.75f, 0.5f, -0.5f, 1.0f,   2.0f, 0.0f, // Top-right
                //0.75f, 0.0f, -0.5f, 1.0f,   2.0f, 2.0f, // Base-right
                //0.25f, 0.0f, -0.5f, 1.0f,   0.0f, 2.0f, // Base-left
                -0.75f, 0.75f, 0f, 1.0f,     0.0f, 0.0f, // Top-left
                0.75f, 0.75f, 0f, 1.0f,      2.0f, 0.0f, // Top-right
                0.75f, -0.75f, 0f, 1.0f,     2.0f, 2.0f, // Base-right
                -0.75f, -0.75f, 0f, 1.0f,    0.0f, 2.0f, // Base-left
            };

            buffer_ = ToDispose(Buffer.Create(DeviceManager.Direct3DDevice, BindFlags.VertexBuffer, data));
            vertexBinding_ = new VertexBufferBinding(buffer_, Utilities.SizeOf<float>() * 6, 0);

            // v0    v1
            // |-----|
            // | \ A |
            // | B \ |
            // |-----|
            // v3    v2
            quadIndices = ToDispose(Buffer.Create(DeviceManager.Direct3DDevice, BindFlags.IndexBuffer, new ushort[] {
                0, 1, 2, // A
                2, 3, 0  // B
            }));
            PrimitiveCount = data.Length / 2;
        }
        protected override void CreateDeviceDependentResources()
        {
            // Remove first our own resources
            RemoveAndDispose(ref quadIndices);

            // Call base implementation
            base.CreateDeviceDependentResources();
        }
        protected override int PrimitiveCount
        {
            get;
            set;
        }
        protected override SharpDX.Direct3D.PrimitiveTopology PrimitiveTopology
        {
            get { return SharpDX.Direct3D.PrimitiveTopology.TriangleList; }
        }

        protected override void DoRender()
        {
            var context = this.DeviceManager.Direct3DContext;

            // Render a quad

            // Tell the IA we are using triangles
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            // Set the index buffer
            context.InputAssembler.SetIndexBuffer(quadIndices, Format.R16_UInt, 0);
            // Pass in the quad vertices (note: only 4 vertices)
            context.InputAssembler.SetVertexBuffers(0, vertexBinding_);
            // Draw the 6 vertices that make up the two triangles in the quad
            // using the vertex indices
            context.DrawIndexed(6, 0, 0);
            // Note: we have called DrawIndexed so that the index buffer will be used
        }
    }
}
