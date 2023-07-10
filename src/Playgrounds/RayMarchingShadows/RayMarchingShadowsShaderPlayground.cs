using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Playgrounds.RayMarchingShadows
{
    public class RayMarchingShadowsShaderPlayground : DrawableGameComponent
    {
        private VertexBuffer vertexBuffer;

        private BasicCamera camera;

        private HotReloadShaderManager hotReloadShaderManager;
        private Effect effect;

        private Texture2D baseColorTexture;

        public RayMarchingShadowsShaderPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
            hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\RayMarchingShadows\RayMarchingShadowsShader.fx");
        }

        public override void Initialize()
        {
            effect = hotReloadShaderManager.Load("Shaders/RayMarchingShadowsShader");
            effect.Parameters["World"].SetValue(Matrix.Identity);
            effect.Parameters["ScreenSize"].SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

            baseColorTexture = Game.Content.Load<Texture2D>("Textures/MetalPanel/basecolor");
            effect.Parameters["ColorMap"]?.SetValue(baseColorTexture);

            var meshVertices = VertexsBuilderHelper.ConstructVertexPositionTextureCube(new Vector3(-0.5f, -0.5f, -0.5f), 1f);
            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionTexture), meshVertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(meshVertices.ToArray());
        }

        public override void Update(GameTime gameTime)
        {
            if (hotReloadShaderManager.CheckForChanges()) Initialize();

            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["CameraTarget"].SetValue(camera.Target);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.Projection);

            // Light direction randomness can be fixed by commenting the following lines
            float x = (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds) * 2;
            float y = 3;//((float)Math.Tan(gameTime.TotalGameTime.TotalSeconds) + 1) * 3;
            float z = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds) * 2;
            effect.Parameters["LightPosition"].SetValue(new Vector3(x, y, z));

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
