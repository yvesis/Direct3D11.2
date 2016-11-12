using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.Direct3D11;
namespace Lights_Materials.Application
{
    class CubeRenderer : PrimitivesRenderer
    {
        Buffer indexBuffer;
        protected override void CreateVertexBinding()
        {
            RemoveAndDispose(ref indexBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            var data = new Vertex[] {
                    /*  Vertex Position    Color */
            new Vertex(new Vector3(-0.5f, 0.5f, -0.5f), Color.Gray),  // 0-Top-left
            new Vertex(new Vector3(0.5f, 0.5f, -0.5f),  Color.Gray),  // 1-Top-right
            new Vertex(new Vector3(0.5f, -0.5f, -0.5f),  Color.Gray), // 2-Base-right
            new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), Color.Gray), // 3-Base-left

            new Vertex(new Vector3(-0.5f, 0.5f, 0.5f),  Color.Gray),  // 4-Top-left
            new Vertex(new Vector3(0.5f, 0.5f, 0.5f),   Color.Gray),  // 5-Top-right
            new Vertex(new Vector3(0.5f, -0.5f, 0.5f),  Color.Gray),  // 6-Base-right
            new Vertex(new Vector3(-0.5f, -0.5f, 0.5f), Color.Gray),  // 7-Base-left
            };
            // Create vertex buffer for cube
            buffer_ = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, data));
            vertexBinding_ = new VertexBufferBinding(buffer_, Utilities.SizeOf<Vertex>(), 0);

            // Front    Right    Top      Back     Left     Bottom  
            // v0    v1 v1    v5 v1    v0 v5    v4 v4    v0 v3    v2
            // |-----|  |-----|  |-----|  |-----|  |-----|  |-----|
            // | \ A |  | \ A |  | \ A |  | \ A |  | \ A |  | \ A |
            // | B \ |  | B \ |  | B \ |  | B \ |  | B \ |  | B \ |
            // |-----|  |-----|  |-----|  |-----|  |-----|  |-----|
            // v3    v2 v2    v6 v5    v4 v6    v7 v7    v3 v7    v6
            indexBuffer = ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, new ushort[] {
                0, 1, 2, // Front A
                0, 2, 3, // Front B
                1, 5, 6, // Right A
                1, 6, 2, // Right B
                1, 0, 4, // Top A
                1, 4, 5, // Top B
                5, 4, 7, // Back A
                5, 7, 6, // Back B
                4, 0, 3, // Left A
                4, 3, 7, // Left B
                3, 2, 6, // Bottom A
                3, 6, 7, // Bottom B
            }));

            PrimitiveCount = Utilities.SizeOf<Vertex>();

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
            var context = DeviceManager.Direct3DContext;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBinding_);
            context.DrawIndexed(36/*PrimitiveCount*/, 0, 0);

        }
    }
}
