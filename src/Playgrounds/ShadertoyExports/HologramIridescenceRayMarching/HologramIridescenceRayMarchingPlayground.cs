using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Playgrounds.ShadertoyExports.HologramIridescenceRayMarching
{
    /// <summary>
    /// Based on https://www.shadertoy.com/view/XlcBR7
    /// </summary>
    public class HologramIridescenceRayMarchingPlayground : DrawableGameComponent
    {
        private VertexPositionNormalTexture[] meshVertices;
        private BasicCamera camera;

        private SpriteBatch spriteBatch;

        private Effect effect;

        private HotReloadShaderManager hotReloadShaderManager;

        public HologramIridescenceRayMarchingPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;

            this.hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\ShadertoyExports\HologramIridescenceRayMarching\HologramIridescenceRayMarchingShader.fx");
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = hotReloadShaderManager.Load("Shaders/HologramIridescenceRayMarchingShader");
                // effect = Game.Content.Load<Effect>("Shaders/HologramIridescenceRayMarchingShader");
                effect.Parameters["iChannel0"]?.SetValue( Game.Content.Load<Texture2D>("Textures/iridescence"));
            }

            meshVertices = VertexsBuilderHelper.ConstructVertexPositionNormalTextureCube(new Vector3(-0.5f, -0.5f, -0.5f), 1f);

            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Update(GameTime gameTime)
        {
            if (hotReloadShaderManager.CheckForChanges())
            {
                effect = hotReloadShaderManager.Load("Shaders/HologramIridescenceRayMarchingShader");
                effect.Parameters["iChannel0"]?.SetValue( Game.Content.Load<Texture2D>("Textures/iridescence"));
            }

            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.Projection);
            effect.Parameters["iTime"]?.SetValue((float)gameTime.TotalGameTime.TotalSeconds);
            effect.Parameters["CameraPosition"]?.SetValue(camera.Position);

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
