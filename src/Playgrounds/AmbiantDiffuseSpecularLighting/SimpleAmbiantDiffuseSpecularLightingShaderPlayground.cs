using MonogameShaderPlayground.Cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Helpers;
using System;

namespace MonogameShaderPlayground.Primitives
{
    public class SimpleAmbiantDiffuseSpecularLightingShaderPlayground : DrawableGameComponent
    {
        public VertexBuffer vertexBuffer { get; private set; }
        public IndexBuffer indexBuffer { get; private set; }

        private BasicCamera camera;
        private Effect effect;

        public SimpleAmbiantDiffuseSpecularLightingShaderPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = Game.Content.Load<Effect>("Shaders/AmbiantDiffuseSpecularLightingShader");
            }

            var t = VertexsBuilderHelper.GenerateSphereVertices(1, 16);
            var meshVertices = t.Item1;
            var indices = t.Item2;

            vertexBuffer = new VertexBuffer(this.GraphicsDevice,
                                            typeof(VertexPositionColorNormal),
                                            meshVertices.Length, BufferUsage.None);

            vertexBuffer.SetData(meshVertices);

            // Create an index buffer, and copy our index data into it.
            indexBuffer = new IndexBuffer(this.GraphicsDevice, typeof(int),
                                          indices.Length, BufferUsage.None);

            indexBuffer.SetData(indices);
        }

        public override void Update(GameTime gameTime)
        {
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.Projection);
            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            
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
            this.GraphicsDevice.Indices = indexBuffer;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                this.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexBuffer.IndexCount / 3);
            }
        }
    }
}
