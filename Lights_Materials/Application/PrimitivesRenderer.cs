using Common;
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
    interface Itransformable
    {
        Vector3 RotationAngles { get; set; }
        Matrix M { get; }
        Matrix V { get; }
        Matrix P { get; }
        Matrix N { get; }
    }
    abstract class PrimitivesRenderer : RendererBase, Itransformable
    {
        // Vertex buffer
        protected Buffer buffer_;
        // Binding structure to the vertex buffer
        protected VertexBufferBinding vertexBinding_;

        protected abstract void CreateVertexBinding();
        protected abstract SharpDX.Direct3D.PrimitiveTopology PrimitiveTopology { get; }
        protected abstract int PrimitiveCount { get; set; }
        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            // Dispose before creating
            RemoveAndDispose(ref buffer_);
            RemoveAndDispose(ref vertexBinding_);

            // Create buffer and binding
            CreateVertexBinding();

        }

        protected override void DoRender()
        {
            var context = DeviceManager.Direct3DContext;

            // Tell IA to use lines
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology;

            // Pass the lines vertices
            context.InputAssembler.SetVertexBuffers(0, vertexBinding_);

            // Draw our xyz axis : 18 vertices
            context.Draw(PrimitiveCount, 0);
        }

        /// <summary>
        /// World or model matrix
        /// </summary>
        public Matrix M
        {
            get { return Matrix.Identity; }
        }
        /// <summary>
        /// View matrix
        /// </summary>
        public Matrix V
        {
            get { return Matrix.Identity; }
        }
        /// <summary>
        /// Projection Matrix
        /// </summary>
        public Matrix P
        {
            get { return Matrix.Identity; }
        }
        /// <summary>
        /// Normal matrix
        /// </summary>
        public Matrix N
        {
            get { return Matrix.Identity; }
        }

        public Vector3 RotationAngles { get; set; }
    }
}
