using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Buffer = SharpDX.Direct3D11.Buffer;
using Common;
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

        // Create and allow access to a timer
        System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        public System.Diagnostics.Stopwatch Clock
        {
            get { return clock; }
            set { clock = value; }
        }

        public Common.Mesh.Animation? CurrentAnimation { get; set; }

        public bool PlayOnce { get; set; }
        // Loaded mesh

        Common.Mesh mesh;
        public Common.Mesh Mesh { get { return mesh; } }

        // Per material buffer to use so that the mesh parameters can be used
        public Buffer PerMaterialBuffer { get; set; }

        public Buffer PerArmatureBuffer { get; set; }
        public MeshRenderer(Common.Mesh mesh)
        {
            this.mesh = mesh;
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

            for (int i = 0; i < mesh.VertexBuffers.Count; i++)
                {
                    var vb = mesh.VertexBuffers[i];
                    Vertex[] vertices = new Vertex[vb.Length];
                    for (var j = 0; j < vb.Length; j++)
                    {
                        // Retrieve skinning info for vertex
                        Common.Mesh.SkinningVertex skin = new Common.Mesh.SkinningVertex();
                        if (mesh.SkinningVertexBuffers.Count > 0)
                            skin = mesh.SkinningVertexBuffers[i][j];
                        // create vertex
                        vertices[j] = new Vertex(vb[j].Position, vb[j].Normal, vb[j].Color, vb[j].UV, skin);
                        
                    }
                    vertexBuffers.Add(ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, vertices.ToArray())));
                    vertexBuffers[vertexBuffers.Count - 1].DebugName = "VertexBuffer_" + vertexBuffers.Count.ToString();
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

        //protected override void DoRender()
        //{
        //    var time = clock.ElapsedMilliseconds / 1000.0f;
        //    var context = DeviceManager.Direct3DContext;

        //    //Calculate skin matrices for each bone
        //    ConstantBuffer.PerArmature skinMatrices = new ConstantBuffer.PerArmature();
        //    if(mesh.Bones !=null)
        //    {
        //        // Retrieve bone local transform
        //        for(var i=0;i<mesh.Bones.Count;i++)
        //        {
        //            skinMatrices.Bones[i]=mesh.Bones[i].BoneLocalTransform;
        //        }
        //        if(CurrentAnimation.HasValue)
        //        {
        //            Common.Mesh.Keyframe?[] lastKeyForBones = new Common.Mesh.Keyframe?[mesh.Bones.Count];
        //            bool[] lerpedBones = new bool[mesh.Bones.Count];

        //            for (var i = 0; i < CurrentAnimation.Value.Keyframes.Count; i++) 
        //            {
        //                var frame = CurrentAnimation.Value.Keyframes[i];
        //                if (frame.Time <= time)
        //                {
        //                    skinMatrices.Bones[frame.BoneIndex] = frame.Transform;

        //                    lastKeyForBones[frame.BoneIndex] = frame;
        //                }
        //                else
        //                {
        //                    //perform future frame interpolation
        //                    if(!lerpedBones[frame.BoneIndex])
        //                    {
        //                        Common.Mesh.Keyframe prevFrame;
        //                        if (lastKeyForBones[frame.BoneIndex] != null)
        //                        {
        //                            prevFrame = lastKeyForBones[frame.BoneIndex].Value;
        //                        }
        //                        else
        //                            continue;

        //                        lerpedBones[frame.BoneIndex] = true;

        //                        var frameLength = frame.Time - prevFrame.Time;
        //                        var timeDiff = time - prevFrame.Time;
        //                        var amount = timeDiff / frameLength;

        //                        Vector3 t1, t2;
        //                        Quaternion q1, q2;
        //                        float s1, s2;

        //                        prevFrame.Transform.DecomposeUniformScale(out s1, out q1, out t1);
        //                        frame.Transform.DecomposeUniformScale(out s2, out q2, out t2);

        //                        skinMatrices.Bones[frame.BoneIndex] = Matrix.Scaling(MathUtil.Lerp(s1, s2, amount)) *
        //                                                              Matrix.RotationQuaternion(Quaternion.Slerp(q1, q2, amount)) *
        //                                                              Matrix.Translation(Vector3.Lerp(t1, t2, amount));
        //                    }
        //                }
        //            }
        //        }
        //        for(var i=0;i<mesh.Bones.Count;i++)
        //        {
        //            var bone = mesh.Bones[i];
        //            if(bone.ParentIndex>-1)
        //            {
        //                var parentTransform = skinMatrices.Bones[bone.ParentIndex];
        //                skinMatrices.Bones[i] = skinMatrices.Bones[i] * parentTransform;
        //            }
        //        }
        //        for (var i = 0; i < mesh.Bones.Count; i++)
        //        {
        //            skinMatrices.Bones[i] = Matrix.Transpose(mesh.Bones[i].InvBindPose* skinMatrices.Bones[i]);
        //        }

        //        // Check need to loop animation
        //        if (!PlayOnce && CurrentAnimation.HasValue && CurrentAnimation.Value.EndTime <= time)
        //        {
        //            this.Clock.Restart();
        //        }
        //    }


        //    // Update constant buffer
        //    context.UpdateSubresource(skinMatrices.Bones, PerArmatureBuffer);
        //        // Draw sub-meshes grouped by material
        //        for (var m = 0; m < mesh.Materials.Count; m++)
        //        {
        //            //var subMeshesForMaterial = mesh.SubMeshes;

        //            var subMeshesForMaterial =
        //                (from sm in mesh.SubMeshes
        //                 where sm.MaterialIndex == m
        //                 select sm).ToArray();
        //            // if material buffer is assigned 
        //            if (PerMaterialBuffer != null && subMeshesForMaterial.Length > 0)
        //            {
        //                // update the PerMaterialBuffer constant buffer
        //                var material = new ConstantBuffer.PerMaterial()
        //                {
        //                    Ambient = new Color4(mesh.Materials[m].Ambient),
        //                    Diffuse = new Color4(mesh.Materials[m].Diffuse),
        //                    Emissive = new Color4(mesh.Materials[m].Emissive),
        //                    Specular = new Color4(mesh.Materials[m].Specular),
        //                    Shininess = mesh.Materials[m].SpecularPower,
        //                    UVTransform = mesh.Materials[m].UVTransform,
        //                };

        //                // Bind textures to the pixel shader
        //                int texIndxOffset = m * Common.Mesh.MaxTextures;
        //                material.HasTexture = (uint)(textureViews[texIndxOffset] != null ? 1 : 0); // 0=false
        //                context.PixelShader.SetShaderResources(0, textureViews.GetRange(texIndxOffset, Common.Mesh.MaxTextures).ToArray());
        //                // Set texture sampler state
        //                context.PixelShader.SetSampler(0, samplerState);
        //                // Update material buffer
        //                context.UpdateSubresource(ref material, PerMaterialBuffer);
        //            }


        //            // for each submeshes
        //            foreach (var subMesh in subMeshesForMaterial)
        //            {
        //                // render each submesh
        //                // Ensure the vertex buffer and index buffers are in range
        //                if (subMesh.VertexBufferIndex < vertexBuffers.Count && subMesh.IndexBufferIndex < indexBuffers.Count)
        //                {
        //                    // Retrieve and set the vertex and index buffers
        //                    var vertexBuffer = vertexBuffers[(int)subMesh.VertexBufferIndex];
        //                    context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
        //                    context.InputAssembler.SetIndexBuffer(indexBuffers[(int)subMesh.IndexBufferIndex], Format.R16_UInt, 0);
        //                    // Set topology
        //                    context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
        //                }

        //                // Draw the sub-mesh (includes Primitive count which we multiply by 3)
        //                // The submesh also includes a start index into the vertex buffer
        //                context.DrawIndexed((int)subMesh.PrimCount * 3, (int)subMesh.StartIndex, 0);
        //            }
        //        }
 
        //}
        protected override void DoRender()
        {
            // Calculate elapsed seconds
            var time = clock.ElapsedMilliseconds / 1000.0f;

            // Retrieve device context
            var context = this.DeviceManager.Direct3DContext;

            // Calculate skin matrices for each bone
            ConstantBuffer.PerArmature skinMatrices = new ConstantBuffer.PerArmature();
            if (mesh.Bones != null)
            {
                // Retrieve each bone's local transform
                for (var i = 0; i < mesh.Bones.Count; i++)
                {
                    skinMatrices.Bones[i] = mesh.Bones[i].BoneLocalTransform;
                }

                // Load bone transforms from animation frames
                if (CurrentAnimation.HasValue)
                {
                    // Keep track of the last key-frame used for each bone
                    Mesh.Keyframe?[] lastKeyForBones = new Mesh.Keyframe?[mesh.Bones.Count];
                    // Keep track of whether a bone has been interpolated
                    bool[] lerpedBones = new bool[mesh.Bones.Count];
                    for (var i = 0; i < CurrentAnimation.Value.Keyframes.Count; i++)
                    {
                        // Retrieve current key-frame
                        var frame = CurrentAnimation.Value.Keyframes[i];

                        // If the current frame is not in the future
                        if (frame.Time <= time)
                        {
                            // Keep track of last key-frame for bone
                            lastKeyForBones[frame.BoneIndex] = frame;
                            // Retrieve transform from current key-frame
                            skinMatrices.Bones[frame.BoneIndex] = frame.Transform;
                        }
                        // Frame is in the future, check if we should interpolate
                        else
                        {
                            // Only interpolate a bone's key-frames ONCE
                            if (!lerpedBones[frame.BoneIndex])
                            {
                                // Retrieve the previous key-frame if exists
                                Mesh.Keyframe prevFrame;
                                if (lastKeyForBones[frame.BoneIndex] != null)
                                    prevFrame = lastKeyForBones[frame.BoneIndex].Value;
                                else
                                    continue; // nothing to interpolate
                                // Make sure we only interpolate with 
                                // one future frame for this bone
                                lerpedBones[frame.BoneIndex] = true;

                                // Calculate time difference between frames
                                var frameLength = frame.Time - prevFrame.Time;
                                var timeDiff = time - prevFrame.Time;
                                var amount = timeDiff / frameLength;

                                // Interpolation using Lerp on scale and translation, and Slerp on Rotation (Quaternion)
                                Vector3 t1, t2;   // Translation
                                Quaternion q1, q2;// Rotation
                                float s1, s2;     // Scale
                                // Decompose the previous key-frame's transform
                                prevFrame.Transform.DecomposeUniformScale(out s1, out q1, out t1);
                                // Decompose the current key-frame's transform
                                frame.Transform.DecomposeUniformScale(out s2, out q2, out t2);

                                // Perform interpolation and reconstitute matrix
                                skinMatrices.Bones[frame.BoneIndex] =
                                    Matrix.Scaling(MathUtil.Lerp(s1, s2, amount)) *
                                    Matrix.RotationQuaternion(Quaternion.Slerp(q1, q2, amount)) *
                                    Matrix.Translation(Vector3.Lerp(t1, t2, amount));
                            }
                        }

                    }
                }

                // Apply parent bone transforms
                // We assume here that the first bone has no parent
                // and that each parent bone appears before children
                for (var i = 1; i < mesh.Bones.Count; i++)
                {
                    var bone = mesh.Bones[i];
                    if (bone.ParentIndex > -1)
                    {
                        var parentTransform = skinMatrices.Bones[bone.ParentIndex];
                        skinMatrices.Bones[i] = (skinMatrices.Bones[i] * parentTransform);
                    }
                }

                // Change the bone transform from rest pose space into bone space (using the inverse of the bind/rest pose)
                for (var i = 0; i < mesh.Bones.Count; i++)
                {
                    skinMatrices.Bones[i] = Matrix.Transpose(mesh.Bones[i].InvBindPose * skinMatrices.Bones[i]);
                }

                // Check need to loop animation
                if (!PlayOnce && CurrentAnimation.HasValue && CurrentAnimation.Value.EndTime <= time)
                {
                    this.Clock.Restart();
                }
            }

            // Update the constant buffer with the skin matrices for each bone
            context.UpdateSubresource(skinMatrices.Bones, PerArmatureBuffer);

            // Draw sub-meshes grouped by material
            for (var mIndx = 0; mIndx < mesh.Materials.Count; mIndx++)
            {
                // Retrieve sub meshes for this material
                var subMeshesForMaterial =
                    (from sm in mesh.SubMeshes
                     where sm.MaterialIndex == mIndx
                     select sm).ToArray();

                // If the material buffer is available and there are submeshes
                // using the material update the PerMaterialBuffer
                if (PerMaterialBuffer != null && subMeshesForMaterial.Length > 0)
                {
                    // update the PerMaterialBuffer constant buffer
                    var material = new ConstantBuffer.PerMaterial()
                    {
                        Ambient = new Color4(mesh.Materials[mIndx].Ambient),
                        Diffuse = new Color4(mesh.Materials[mIndx].Diffuse),
                        Emissive = new Color4(mesh.Materials[mIndx].Emissive),
                        Specular = new Color4(mesh.Materials[mIndx].Specular),
                        Shininess = mesh.Materials[mIndx].SpecularPower,
                        UVTransform = mesh.Materials[mIndx].UVTransform,
                    };

                    // Bind textures to the pixel shader
                    int texIndxOffset = mIndx * Common.Mesh.MaxTextures;
                    material.HasTexture = (uint)(textureViews[texIndxOffset] != null ? 1 : 0); // 0=false
                    context.PixelShader.SetShaderResources(0, textureViews.GetRange(texIndxOffset, Common.Mesh.MaxTextures).ToArray());

                    // Set texture sampler state
                    context.PixelShader.SetSampler(0, samplerState);

                    // Update material buffer
                    context.UpdateSubresource(ref material, PerMaterialBuffer);
                }

                // For each sub-mesh
                foreach (var subMesh in subMeshesForMaterial)
                {
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
