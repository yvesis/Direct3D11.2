using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using Common;

namespace LoadMeshes.Application
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;
        public Vector2 UV;
        public Mesh.SkinningVertex Skin;
        public Vertex(Vector3 position, Vector3 normal, Color color)
        {
            this.Position = position;
            this.Normal = normal;
            this.Color = color;
            this.UV = Vector2.Zero;
            this.Skin = new Mesh.SkinningVertex();
        }
        public Vertex(Vector3 position, Vector3 normal, Color color, Vector2 uv)
            : this(position, normal, color)
        {
            this.UV = uv;
        }

        public Vertex(Vector3 position, Color color)
            : this(position, Vector3.Normalize(position), color)
        {
        }
        public Vertex(Vector3 position)
            : this(position, Vector3.Normalize(position), SharpDX.Color.White)
        {
        }

        public Vertex(Vector3 position, Vector3 normal, Color color, Vector2 uv, Mesh.SkinningVertex skin)
        {
            this.Position = position;
            this.Normal = normal;
            this.Color = color;
            this.UV = uv;
            this.Skin = skin;
        }
    }
}
