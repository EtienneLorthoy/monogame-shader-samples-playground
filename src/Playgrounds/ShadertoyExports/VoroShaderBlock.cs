using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Primitives
{
    public class VoroShaderBlock : DrawableGameComponent
    {
        private VertexPosition[] meshVertices;
        private Effect effect;
        private BasicCamera camera;

        public VoroShaderBlock(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = Game.Content.Load<Effect>("Shaders/Voro");

                int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
                int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
                Vector2 screenResolution = new Vector2(screenWidth, screenHeight);
                effect.Parameters["iResolution"].SetValue(screenResolution);
            }

            meshVertices = VertexsBuilderHelper.ConstructVertexPositionCube(new Vector3(-0.5f, -0.5f, -0.5f), 1);
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
                Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, meshVertices, 0, meshVertices.Length / 3);
            }
        }
    }
}
