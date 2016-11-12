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
            public Vector3 CameraPosition;
            float _padding0; // to make 16 bytes for performance
        }

    }
}
