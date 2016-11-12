using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Lights_Materials.Application
{
    class SphereRenderer1 : PrimitivesRenderer
    {
        Buffer indexBuffer;

        protected override void CreateVertexBinding()
        {
            RemoveAndDispose(ref indexBuffer);

            Vertex[] vertices;
            int[] indices;
            GeometricPrimitives.GenerateSphere(out vertices, out indices, Color.Gray);

            var device = DeviceManager.Direct3DDevice;
            buffer_ = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, vertices));
            vertexBinding_ = new VertexBufferBinding(buffer_, Utilities.SizeOf<Vertex>(), 0);

            indexBuffer = ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, indices));
            PrimitiveCount = indices.Length;
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
            context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBinding_);
            context.DrawIndexed(PrimitiveCount, 0, 0);
        }

    }
}
