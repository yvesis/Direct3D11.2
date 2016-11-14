using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace LoadMeshes.Application
{
    class MeshRenderer: Common.RendererBase
    {
        // Vertex buffer
        List<Buffer> vertexBuffers = new List<Buffer>();
        // Index Buffer
        List<Buffer> indexBuffers = new List<Buffer>();
        // Texture resources
        List<ShaderResourceView> textureViews = new List<ShaderResourceView>();

        // Sampler state
        SamplerState samplerState;

        // Loaded mesh

        Common.Mesh mesh;
        Common.Mesh[] meshes;
        public Common.Mesh Mesh { get { return mesh; } }

        // Per material buffer to use so that the mesh parameters can be used
        public Buffer PerMaterialBuffer { get; set; }

        public MeshRenderer(Common.Mesh mesh)
        {
            this.meshes = new Common.Mesh[] { mesh };
        }
        public MeshRenderer(Common.Mesh[] meshes)
        {
            this.meshes = meshes;
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            // release resources
            vertexBuffers.ForEach(b => RemoveAndDispose(ref b));
            vertexBuffers.Clear();
            indexBuffers.ForEach(b => RemoveAndDispose(ref b));
            indexBuffers.Clear();
            textureViews.ForEach(t => RemoveAndDispose(ref t));
            textureViews.Clear();
            RemoveAndDispose(ref samplerState);

            var device = DeviceManager.Direct3DDevice;
            // Create the vertex buffers
            foreach(var mesh in meshes)
            {
                for (int i = 0; i < mesh.VertexBuffers.Count; i++)
                {
                    var vb = mesh.VertexBuffers[i];
                    Vertex[] vertices = new Vertex[vb.Length];
                    for (var j = 0; j < vb.Length; j++)
                    {
                        // create vertex
                        vertices[j] = new Vertex(vb[j].Position, vb[j].Normal, vb[j].Color, vb[j].UV);
                    }
                    vertexBuffers.Add(ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, vertices.ToArray())));
                    vertexBuffers[vertexBuffers.Count - 1].DebugName = "VertexBuffer_" + i.ToString();
                }
                // Create the index buffers
                foreach (var ib in mesh.IndexBuffers)
                {
                    indexBuffers.Add(ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, ib)));
                    indexBuffers[indexBuffers.Count - 1].DebugName = "IndexBuffer_" + (indexBuffers.Count - 1).ToString();
                }

                // Create the textures resources views
                // The CMO file format supports up to 8 per material
                foreach (var m in mesh.Materials)
                {
                    // Diffuse color
                    for (var i = 0; i < m.Textures.Length; i++)
                    {
                        textureViews.Add(SharpDX.IO.NativeFile.Exists(m.Textures[i]) ?
                            ToDispose(ShaderResourceView.FromFile(device, m.Textures[i])) : null);
                    }
                }
            }

            // Create the sampler state

            samplerState = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = Color.Black,
                ComparisonFunction = Comparison.Never,
                Filter = Filter.MinMagMipLinear,
                MaximumAnisotropy = 16,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0.0f

            }));
        }

        protected override void DoRender()
        {
            var context = DeviceManager.Direct3DContext;

            foreach(var mesh in meshes)
            {
                // Draw sub-meshes grouped by material
                for (var m = 0; m < mesh.Materials.Count; m++)
                {
                    var subMeshesForMaterial =
                        (from sm in mesh.SubMeshes
                         where sm.MaterialIndex == m
                         select sm).ToArray();
                    // if material buffer is assigned 
                    if (PerMaterialBuffer != null && subMeshesForMaterial.Length > 0)
                    {
                        // update the PerMaterialBuffer constant buffer
                        var material = new ConstantBuffer.PerMaterial()
                        {
                            Ambient = new Color4(mesh.Materials[m].Ambient),
                            Diffuse = new Color4(mesh.Materials[m].Diffuse),
                            Emissive = new Color4(mesh.Materials[m].Emissive),
                            Specular = new Color4(mesh.Materials[m].Specular),
                            Shininess = mesh.Materials[m].SpecularPower,
                            UVTransform = mesh.Materials[m].UVTransform,
                        };

                        // Bind textures to the pixel shader
                        int texIndxOffset = m * Common.Mesh.MaxTextures;
                        material.HasTexture = (uint)(textureViews[texIndxOffset] != null ? 1 : 0); // 0=false
                        context.PixelShader.SetShaderResources(0, textureViews.GetRange(texIndxOffset, Common.Mesh.MaxTextures).ToArray());
                        // Set texture sampler state
                        context.PixelShader.SetSampler(0, samplerState);
                        // Update material buffer
                        context.UpdateSubresource(ref material, PerMaterialBuffer);
                    }


                    // for each submeshes
                    foreach (var subMesh in subMeshesForMaterial)
                    {
                        // render each submesh
                        // Ensure the vertex buffer and index buffers are in range
                        if (subMesh.VertexBufferIndex < vertexBuffers.Count && subMesh.IndexBufferIndex < indexBuffers.Count)
                        {
                            // Retrieve and set the vertex and index buffers
                            var vertexBuffer = vertexBuffers[(int)subMesh.VertexBufferIndex];
                            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
                            context.InputAssembler.SetIndexBuffer(indexBuffers[(int)subMesh.IndexBufferIndex], Format.R16_UInt, 0);
                            // Set topology
                            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
                        }

                        // Draw the sub-mesh (includes Primitive count which we multiply by 3)
                        // The submesh also includes a start index into the vertex buffer
                        context.DrawIndexed((int)subMesh.PrimCount * 3, (int)subMesh.StartIndex, 0);
                    }
                }
 
            }
        }
    }
}
