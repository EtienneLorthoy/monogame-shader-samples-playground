using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonogameShaderPlayground.Cameras;
using MonogameShaderPlayground.Helpers;
using MonogameShaderPlayground.Primitives;

namespace MonogameShaderPlayground
{
    public class Game1 : Game
    {
        public BasicCamera Camera;
        public SimpleLabel label;

        private GraphicsDeviceManager graphics;
        private KeyboardState lastKeyboardState;
        private List<DrawableGameComponent> playgrounds = new List<DrawableGameComponent>();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            graphics.IsFullScreen = false;
            graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;
            Window.AllowAltF4 = true;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Camera = new BasicCamera(this, Vector3.Zero, Vector3.Up);
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();

            this.Components.Add(Camera);
            this.label = new SimpleLabel(this);

            // Shadertoy exports
            playgrounds.Add(new RaymarchingShaderBlock(this, Camera));
            playgrounds.Add(new VoroShaderBlock(this, Camera));
            playgrounds.Add(new ShadertoyCubeProjectedPlayground(this, Camera));
            playgrounds.Add(new HologramIridiscenceRayMarchingPlayground(this, Camera));

            // Rendering to texture techniques
            playgrounds.Add(new RenderToTexturePlayground(this, Camera));
            playgrounds.Add(new ImageFromClipBoardPlayground(this, Camera));

            // Lighting techniques
            playgrounds.Add(new AlphaAmbiantDiffuseSpecularLightingShaderPlayground(this, Camera));
            playgrounds.Add(new SimpleAmbiantDiffuseSpecularLightingShaderPlayground(this, Camera));
            playgrounds.Add(new SimpleNormalMappingShaderPlayground(this, Camera));
            playgrounds.Add(new SimpleParallaxMappingShaderPlayground(this, Camera));

            // Starting playground
            var startingIndex = 3;
            this.label.Text = playgrounds[startingIndex].GetType().Name + " - SPACE: switch playgrounds, R: auto-rotate";
            this.Components.Add(label);
            this.Components.Add(playgrounds[startingIndex]);

            // Utils
            this.Components.Add(new Gizmo(this));
            this.Components.Add(new FrameRateCounter(this));

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape) && lastKeyboardState.IsKeyUp(Keys.Escape)) Exit();

            // Switch playgrounds
            if (keyboardState.IsKeyDown(Keys.Space) && lastKeyboardState.IsKeyUp(Keys.Space))
            {
                var index = playgrounds.FindIndex(x => x.Enabled);
                if (index == -1) index = 0;
                else index = (index + 1) % playgrounds.Count;

                foreach (var playground in playgrounds)
                {
                    playground.Enabled = false;
                    if (Components.Contains(playground)) Components.Remove(playground);
                }

                playgrounds[index].Enabled = true;
                label.Text = playgrounds[index].GetType().Name;
                Components.Add(playgrounds[index]);
            }

            lastKeyboardState = keyboardState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
        }
    }
}
