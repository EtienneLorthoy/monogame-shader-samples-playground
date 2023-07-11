using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Playgrounds.RayMarching
{
    public class SimpleRayMarchingShaderPlayground : DrawableGameComponent
    {
        private VertexPosition[] meshVertices;

        private BasicCamera camera;

        private HotReloadShaderManager hotReloadShaderManager;
        private Effect effect;

        public SimpleRayMarchingShaderPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
            hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\RayMarching\SimpleRayMarchingShader.fx");
        }

        public override void Initialize()
        {
            effect = hotReloadShaderManager.Load("Shaders/SimpleRayMarchingShader");
            effect.Parameters["World"].SetValue(Matrix.Identity);
            effect.Parameters["ScreenSize"].SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

            this.camera.Target = Vector3.Zero;
            this.camera.Position = new Vector3(-1.4f, 1, 1.4f);

            meshVertices = VertexsBuilderHelper.ConstructVertexPositionCube(new Vector3(-0.5f, -0.5f, -0.5f), 1f);
        }

        public override void Update(GameTime gameTime)
        {
            if (hotReloadShaderManager.CheckForChanges()) Initialize();

            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["CameraTarget"].SetValue(camera.Target);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.ProjectionMatrix);

            // Light direction randomness can be fixed by commenting the following lines
            float x = (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds) * 2;
            float y = 3;//((float)Math.Tan(gameTime.TotalGameTime.TotalSeconds) + 1) * 3;
            float z = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds) * 2;
            effect.Parameters["LightPosition"].SetValue(new Vector3(x, y, z));

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, meshVertices, 0, meshVertices.Length / 3);
            }
        }
    }
}
