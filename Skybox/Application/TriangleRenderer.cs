using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffer = SharpDX.Direct3D11.Buffer;
namespace Skybox.Application
{
    class TriangleRenderer : PrimitivesRenderer
    {
        protected override void CreateVertexBinding()
        {
            var data = new[]
            {
            /*  Vertex Position                       Vertex Color */
                new Vector4(0.0f, 0.0f, 0.5f, 1.0f),  new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Base-right
                new Vector4(-0.5f, 0.0f, 0.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Base-left
                new Vector4(-0.25f, 1f, 0.25f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // Apex
            };
            buffer_ = ToDispose(Buffer.Create(DeviceManager.Direct3DDevice, BindFlags.VertexBuffer, data));
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
            get { return SharpDX.Direct3D.PrimitiveTopology.TriangleList; }
        }
        public new Matrix M
        {
            get { return Matrix.RotationY(90 * 0.016f); }
        }
    }
}
