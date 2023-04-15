using MonogameShaderPlayground.Cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Primitives
{
    public class RenderToTexturePlayground : DrawableGameComponent
    {
        private VertexPositionTexture[] meshVertices;
        private Effect effect;
        private BasicCamera camera;

        private RenderTarget2D renderTarget;        
        private Texture2D renderTexture;

        private SpriteBatch spriteBatch;

        public RenderToTexturePlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = Game.Content.Load<Effect>("VoroShader");

                int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
                int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
                Vector2 screenResolution = new Vector2(screenWidth, screenHeight);
                effect.Parameters["iResolution"].SetValue(screenResolution);
            }

            renderTarget = new RenderTarget2D(GraphicsDevice, 1024, 1024, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            meshVertices = VertexsBuilderHelper.ConstructVertexPositionTextureCube(new Vector3(-0.5f, -0.5f, -0.5f), 1);

            spriteBatch = new SpriteBatch(GraphicsDevice);
            renderTexture = new Texture2D(GraphicsDevice, 512, 512);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            effect.Parameters["WorldViewProjection"].SetValue(camera.ViewMatrix * camera.Projection);
            effect.Parameters["iTime"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            effect.CurrentTechnique = effect.Techniques["Technique0"];
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, effect, null);
            spriteBatch.Draw(renderTexture, new Rectangle(0, 0, renderTarget.Width, renderTarget.Height), Color.White);
            spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);

            effect.CurrentTechnique = effect.Techniques["Technique1"];
            effect.Parameters["ColorMap"]?.SetValue(renderTarget);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, meshVertices, 0, meshVertices.Length / 3);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (renderTarget != null) renderTarget.Dispose();
            renderTexture.Dispose();

            base.Dispose(disposing);
        }
    }
}
