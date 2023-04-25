using MonogameShaderPlayground.Cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonogameShaderPlayground.Helpers;

namespace MonogameShaderPlayground.Primitives
{
    /// <summary>
    /// Based on https://www.shadertoy.com/view/XlcBR7
    /// </summary>
    public class HologramIridiscenceRayMarchingPlayground : DrawableGameComponent
    {
        private VertexPositionTexture[] meshVertices;
        private BasicCamera camera;

        private SpriteBatch spriteBatch;

        private Effect effect;

        private HotReloadShaderManager hotReloadShaderManager;

        public HologramIridiscenceRayMarchingPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;

            this.hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\ShadertoyExports\HologramIridiscenceRayMarching\HologramIridiscenceRayMarchingShader.fx");
        }

        public override void Initialize()
        {
            if (effect == null)
            {
                effect = hotReloadShaderManager.Load("Shaders/HologramIridiscenceRayMarchingShader");
                // effect = Game.Content.Load<Effect>("Shaders/HologramIridiscenceRayMarchingShader");
                effect.Parameters["iChannel0"]?.SetValue( Game.Content.Load<Texture2D>("Textures/iridescence"));
            }

            meshVertices = VertexsBuilderHelper.ConstructVertexPositionTextureCube(new Vector3(-0.5f, -0.5f, -0.5f), 1f);

            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Update(GameTime gameTime)
        {
            if (hotReloadShaderManager.CheckForChanges())
            {
                effect = hotReloadShaderManager.Load("Shaders/HologramIridiscenceRayMarchingShader");
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
