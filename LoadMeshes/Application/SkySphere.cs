using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace LoadMeshes.Application
{
    class SkySphere : PrimitivesRenderer
    {
        Buffer indexBuffer;
        ShaderResourceView textureCube;
        SamplerState sampler;

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();
            RemoveAndDispose(ref textureCube);
            RemoveAndDispose(ref sampler);

            textureCube = ToDispose(ShaderResourceView.FromFile(DeviceManager.Direct3DDevice, "Textures/Sunset.dds"));
            sampler = ToDispose(new SamplerState(DeviceManager.Direct3DDevice, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = new Color4(0, 0, 0, 0),
                ComparisonFunction = Comparison.Less,
                Filter = Filter.MinMagMipLinear,
                MaximumLod = 9, // Our cube map has 10 mip map levels (0-9)
                MinimumLod = 0,
                MipLodBias = 0.0f
            }));
            World = Matrix.Scaling(512f);
        }

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
            context.PixelShader.SetShaderResource(0, textureCube);
            context.PixelShader.SetSampler(0, sampler);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBinding_);
            context.DrawIndexed(PrimitiveCount, 0, 0);
        }

    }
}
