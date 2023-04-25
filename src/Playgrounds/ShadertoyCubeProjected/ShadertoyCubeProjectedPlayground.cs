using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Primitives
{
    public class ShadertoyCubeProjectedPlayground : DrawableGameComponent
    {
        private VertexPositionTexture[] meshVertices;
        private BasicCamera camera;
        private Effect effect;

        public ShadertoyCubeProjectedPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = Game.Content.Load<Effect>("Shaders/VoroShaderProjected");
                int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
                int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
                Vector2 screenResolution = new Vector2(screenWidth, screenHeight);
                effect.Parameters["iResolution"].SetValue(screenResolution);
            }

            meshVertices = VertexsBuilderHelper.ConstructVertexPositionTextureCube(new Vector3(-0.5f, -0.5f, -0.5f), 1f);
        }

        public override void Update(GameTime gameTime)
        {
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.Projection);
            effect.Parameters["iTime"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, meshVertices, 0, meshVertices.Length / 3);
            }
        }
    }
}
