using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace D3D11._1.Application
{
    class SphereRenderer:PrimitivesRenderer
    {
        private Vector3 center_;
        private float radius_;
        private float squaredRadius_;
        private List<Vector4> vertices_ ;
        public SphereRenderer(Vector3 center,float radius)
        {
            center_ = center;
            radius_ = radius;
            squaredRadius_ = radius_ * radius_;
            CreateVertices();
        }
        private void CreateVertices()
        {
            Random rnd = new Random();
            vertices_ =  new List<Vector4>();
            for (var theta = 0; theta <= 180; theta++)
                for (var phi = 0; phi <360 ; phi++)
                {
                    var x=radius_ * Math.Sin(theta * Math.PI / 180) * Math.Cos(phi * Math.PI / 180);
                    var y=radius_ * Math.Sin(theta * Math.PI / 180) * Math.Sin(phi * Math.PI / 180);
                    var z=radius_ * Math.Cos(theta * Math.PI / 180) ;
                    vertices_.AddRange(new[] { new Vector4((float)x, (float)y, (float)z, 1f), new Vector4(rnd.NextFloat(0.1f, 0.5f), rnd.NextFloat(0.1f, 0.5f), rnd.NextFloat(0.1f, 0.5f), 1) }); //new Vector4(phi%2, theta%2, Math.Abs(phi-theta)%2, 1)

                }
            
        }
        protected override void CreateVertexBinding()
        {
            buffer_ = ToDispose(Buffer.Create(DeviceManager.Direct3DDevice,BindFlags.VertexBuffer,vertices_.ToArray()));
            vertexBinding_ = new VertexBufferBinding(buffer_, Utilities.SizeOf<Vector4>(), 0);
            PrimitiveCount = vertices_.Count;
        }
        protected override int PrimitiveCount
        {
            get;
            set;
        }
        protected override SharpDX.Direct3D.PrimitiveTopology PrimitiveTopology
        {
            get { return SharpDX.Direct3D.PrimitiveTopology.PointList; }
        }
        public new Matrix M
        {
            get { return Matrix.RotationZ(RotationAngles.Y* (float)(Math.PI*90)/180); }
        }
    }
}
