using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using System.Linq;
using System;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Primitives
{
    public class SimpleNormalMappingShaderPlayground : DrawableGameComponent
    {
        private VertexBuffer vertexBuffer;

        private BasicCamera camera;

        private Effect effect;

        private Texture2D baseColorTexture;
        private Texture2D normalMapTexture;
        private Texture2D heightMapTexture;

        public SimpleNormalMappingShaderPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = Game.Content.Load<Effect>("Shaders/SimpleNormalMappingShader");

                effect.Parameters["World"].SetValue(Matrix.Identity);
            }

            if (baseColorTexture == null)
            {
                baseColorTexture = Game.Content.Load<Texture2D>("Textures/MetalPanel/basecolor");
                effect.Parameters["ColorMap"]?.SetValue(baseColorTexture);
            }

            if (normalMapTexture == null)
            {
                normalMapTexture = Game.Content.Load<Texture2D>("Textures/MetalPanel/normal");
                effect.Parameters["NormalMap"]?.SetValue(normalMapTexture);
            }

            var meshVertices = NormalMappingVertexsBuilderHelper.ConstructVertexPositionNormalTextureTangentBinormalCube(new Vector3(-0.5f, -0.5f, -0.5f), 1);
            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTextureTangentBinormal), meshVertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(meshVertices.ToArray());
        }

        public override void Update(GameTime gameTime)
        {
            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.Projection);

            // Light direction randomnes can be fixed by commenting the following lines
            float x = (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds);
            float y = (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds);
            float z = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds);
            effect.Parameters["LightDirection"].SetValue(new Vector3(x, y, z));

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetVertexBuffer(vertexBuffer);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3);
            }
        }
    }

    public struct VertexPositionNormalTextureTangentBinormal : IVertexType
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

        public VertexPositionNormalTextureTangentBinormal(Vector3 position, Vector3 normal, Vector2 textureCoordinate, Vector3 tangent, Vector3 binormal)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            Tangent = tangent;
            Binormal = binormal;
        }
    }

    public static class NormalMappingVertexsBuilderHelper
    {
        public static VertexPositionNormalTextureTangentBinormal[] ConstructVertexPositionNormalTextureTangentBinormalCube(Vector3 pos, float size)
        {
            var vertices = new VertexPositionNormalTextureTangentBinormal[36];

            // Face Up
            vertices[0] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[1] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[2] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

            vertices[3] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[4] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[5] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

            // Face Front
            vertices[6] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            vertices[7] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            vertices[8] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));

            vertices[9] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            vertices[10] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            vertices[11] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));

            // Face Down
            vertices[12] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[13] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[14] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

            vertices[15] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[16] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[17] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

            // Face Left
            vertices[18] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            vertices[19] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            vertices[20] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));

            vertices[21] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            vertices[22] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            vertices[23] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));

            // Face Back
            vertices[24] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
            vertices[25] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
            vertices[26] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));

            vertices[27] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
            vertices[28] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
            vertices[29] = new VertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));

            // Face Right
            vertices[30] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 1), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            vertices[31] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 1), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            vertices[32] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));

            vertices[33] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            vertices[34] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 1), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            vertices[35] = new VertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));

            return vertices;
        }
    }
}
