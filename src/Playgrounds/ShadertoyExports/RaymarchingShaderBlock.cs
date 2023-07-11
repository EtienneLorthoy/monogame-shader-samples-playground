using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Primitives
{
    public class RaymarchingShaderBlock : DrawableGameComponent
    {
        private VertexPosition[] meshVertices;
        private BasicCamera camera;

        private SpriteBatch spriteBatch;
        private int screenWidth;
        private int screenHeight;

        private Effect effect;
        private Texture2D texture;

        public RaymarchingShaderBlock(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = Game.Content.Load<Effect>("Shaders/Raymarching");

                screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
                screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
                Vector2 screenResolution = new Vector2(screenWidth, screenHeight);
                effect.Parameters["iResolution"].SetValue(screenResolution);
            }

            if (texture == null)
            {
                texture = Game.Content.Load<Texture2D>("Shaders/RaymarchingTexture");
                effect.Parameters["iChannel0"].SetValue(texture);
            }

            meshVertices = VertexsBuilderHelper.ConstructVertexPositionCube(new Vector3(-0.5f, -0.5f, -0.5f), 1);

            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Update(GameTime gameTime)
        {
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.ProjectionMatrix);
            effect.Parameters["iTime"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);

            Vector2 mousePosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            effect.Parameters["iMouse"].SetValue(mousePosition);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            // Using a sprite batch to render on screen
            // spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, effect, null);
            // spriteBatch.Draw(texture, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            // spriteBatch.End();

            // or using a vertex buffer to render on screen
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, meshVertices, 0, meshVertices.Length / 3);
            }
        }
    }
}
