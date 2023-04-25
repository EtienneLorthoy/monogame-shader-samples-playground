using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace MonogameShaderPlayground.Primitives
{
    public struct MyCustomVertexPositionNormalTextureTangentBinormal : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Vector3 Tangent;
        public Vector3 Binormal;

        private static int Vector3SizeInBytes = Marshal.SizeOf<Vector3>();
        private static int Vector2SizeInBytes = Marshal.SizeOf<Vector2>();

        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(Vector3SizeInBytes, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(Vector3SizeInBytes*2, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(Vector3SizeInBytes*2+Vector2SizeInBytes, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
            new VertexElement(Vector3SizeInBytes*3+Vector2SizeInBytes, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0),
        });
        
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public MyCustomVertexPositionNormalTextureTangentBinormal(Vector3 position, Vector3 normal, Vector2 textureCoordinate, Vector3 tangent, Vector3 binormal)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            Tangent = tangent;
            Binormal = binormal;
        }
    }
}
