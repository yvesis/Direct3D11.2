using Common;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace D3D11._1.Application
{
    abstract class PrimitivesRenderer<T>:RendererBase
    {
        // Vertex buffer
        protected Buffer buffer_;
        // Binding structure to the vertex buffer
        protected VertexBufferBinding vertexBinding;

        protected abstract void CreateVertexBinding();

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            // Dispose before creating
            RemoveAndDispose(ref buffer_);

            // The device
            var device = DeviceManager.Direct3DDevice;
                

        }
    }
}
