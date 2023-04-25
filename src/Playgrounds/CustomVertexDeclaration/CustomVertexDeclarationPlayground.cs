using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Primitives
{
    public class CustomVertexDeclarationPlayground : DrawableGameComponent
    {
        private VertexBuffer vertexBuffer;
        private BasicCamera camera;
        private Effect effect;
        private Texture2D baseColorTexture;
        private Texture2D normalMapTexture;

        public CustomVertexDeclarationPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = Game.Content.Load<Effect>("Shaders/CustomVertexDeclarationShader");
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
            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(MyCustomVertexPositionNormalTextureTangentBinormal), meshVertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(meshVertices.ToArray());
        }

        public override void Update(GameTime gameTime)
        {
            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.Projection);

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
}
