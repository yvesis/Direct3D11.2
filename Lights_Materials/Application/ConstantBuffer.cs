using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lights_Materials.Application
{
    static class ConstantBuffer
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct PerObject
        {
            // World view projection matrix
            public Matrix MVP;
            // World matrix
            public Matrix M;
            // Normal matrix = Inverse(transpose(M))
            public Matrix N;

            internal void Transpose()
            {
                MVP.Transpose();
                M.Transpose();
                N.Transpose();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public struct PerFrame
        {
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Light Light0;
            public Light Light1;
            public Light Light2;
            //public Light[] Lights;
            public Vector3 CameraPosition;
            float _padding0; // to make 16 bytes for performance
        }

        [StructLayout(LayoutKind.Sequential,Pack=1)]
        public struct Light
        {
            public Color4 Color;
            public Vector3 Direction;
            public uint On;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerMaterial
        {
            public Color4 Ambient;
            public Color4 Diffuse;
            public Color4 Specular;
            public float Shininess;
            public uint HasTexture;
            Vector2 _padding0;

            public Color4 Emissive;
            public Matrix UVTransform;
        }

    }
}
