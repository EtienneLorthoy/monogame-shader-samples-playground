using MonogameShaderPlayground.Cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Helpers;
using System;

namespace MonogameShaderPlayground.Primitives
{
    public class AlphaAmbiantDiffuseSpecularLightingShaderPlayground : DrawableGameComponent
    {
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private VertexBuffer cubeVertexBuffer;

        private BasicCamera camera;

        private Effect effect;

        public AlphaAmbiantDiffuseSpecularLightingShaderPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = Game.Content.Load<Effect>("Shaders/AlphaAmbiantDiffuseSpecularLightingShader");
            }

            var t = VertexsBuilderHelper.GenerateSphereVertices(1, 16);
            var meshVertices = t.Item1;
            var indices = t.Item2;

            var cubeVertices = VertexsBuilderHelper.ConstructVertexPositionColorNormalCube(new Vector3(0, 0, -1), 1f);
            cubeVertexBuffer = new VertexBuffer(this.GraphicsDevice,
                                            typeof(VertexPositionColorNormal),
                                            cubeVertices.Length, BufferUsage.None);
            cubeVertexBuffer.SetData(cubeVertices);

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
            // Set depth tencil 
            this.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            // Cube to show the transparency
            GraphicsDevice.SetVertexBuffer(cubeVertexBuffer);
            effect.CurrentTechnique = effect.Techniques["TechniqueOpaque"];
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3);
            }

            // Sphere to show the transparency
            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            this.GraphicsDevice.Indices = indexBuffer;

            effect.CurrentTechnique = effect.Techniques["Technique0"];
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                this.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexBuffer.IndexCount / 3);
            }
        }
    }
}
