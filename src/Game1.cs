using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonogameShaderPlayground.Helpers;
using MonogameShaderPlayground.Playgrounds.CubeVoxels;
using MonogameShaderPlayground.Playgrounds.HologramIridescence;
using MonogameShaderPlayground.Playgrounds.MultipleImportanceSampling;
using MonogameShaderPlayground.Playgrounds.RayMarching;
using MonogameShaderPlayground.Playgrounds.RayMarchingShadows;
using MonogameShaderPlayground.Playgrounds.RayMarchingSoftShadows;
using MonogameShaderPlayground.Primitives;

namespace MonogameShaderPlayground
{
    public class Game1 : Game
    {
        public BasicCamera Camera;
        public SimpleLabel playgroundInfolabel;
        public SimpleLabel cameraInfolabel;

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
            // TargetElapsedTime = TimeSpan.FromSeconds(1d / 30d);
            Window.AllowAltF4 = true;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Camera = new BasicCamera(this, Vector3.Zero, Vector3.Up);
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.ApplyChanges();

            this.Components.Add(Camera);

            // Labels
            this.playgroundInfolabel = new SimpleLabel(this, new Vector2(33, 60));
            this.Components.Add(playgroundInfolabel);

            this.cameraInfolabel = new SimpleLabel(this, new Vector2(33, 180));
            this.Components.Add(cameraInfolabel);

            // Shadertoy exports
            playgrounds.Add(new RaymarchingShaderBlock(this, Camera));
            playgrounds.Add(new VoroShaderBlock(this, Camera));
            playgrounds.Add(new ShadertoyCubeProjectedPlayground(this, Camera));
            playgrounds.Add(new HologramIridescenceShaderPlayground(this, Camera));

            // Rendering to texture techniques
            playgrounds.Add(new RenderToTexturePlayground(this, Camera));
            playgrounds.Add(new ImageFromClipBoardPlayground(this, Camera));

            // Lighting techniques
            playgrounds.Add(new AlphaAmbiantDiffuseSpecularLightingShaderPlayground(this, Camera));
            playgrounds.Add(new SimpleAmbiantDiffuseSpecularLightingShaderPlayground(this, Camera));
            playgrounds.Add(new SimpleNormalMappingShaderPlayground(this, Camera));
            playgrounds.Add(new SimpleParallaxMappingShaderPlayground(this, Camera));

            // Raymarching techniques
            playgrounds.Add(new SimpleRayMarchingShaderPlayground(this, Camera));
            playgrounds.Add(new RayMarchingShadowsShaderPlayground(this, Camera));
            playgrounds.Add(new RayMarchingSoftShadowsPlayground(this, Camera));

            // Multiple Importance Sampling techniques
            playgrounds.Add(new MultipleImportanceSamplingPlayground(this, Camera));

            // Datastructures
            playgrounds.Add(new SimpleCubeVoxelsPlayground(this, Camera));

            // Monogame or C# interop specific techniques
            playgrounds.Add(new CustomVertexDeclarationPlayground(this, Camera));

            // Starting playground
            var startingIndex = 14;
            this.Components.Add(playgrounds[startingIndex]);
            this.playgroundInfolabel.Text = BuildDebugOutputString(playgrounds[startingIndex].GetType().Name);

            // Utils
            this.Components.Add(new Gizmo(this, Vector3.Zero));
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
                    // Remove/reseting playground
                    playground.Enabled = false;
                    playground.Visible = false;
                    if (Components.Contains(playground)) Components.Remove(playground);
                    Camera.Target = Vector3.Zero;
                    Camera.Position = Vector3.One * 2;
                    Camera.SwingFactor = 2f;
                }

                playgroundInfolabel.Text = BuildDebugOutputString(playgrounds[index].GetType().Name);
                playgrounds[index].Enabled = true;
                playgrounds[index].Visible = true;

                Components.Add(playgrounds[index]);
            }

            cameraInfolabel.Text = $"Camera position (x:{Camera.Position.X:0.0}, y:{Camera.Position.Y:0.0}, z:{Camera.Position.Z:0.0})";
            cameraInfolabel.Text += $"\nTarget position (x:{Camera.Target.X:0.0}, y:{Camera.Target.Y:0.0}, z:{Camera.Target.Z:0.0})";

            lastKeyboardState = keyboardState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            base.Draw(gameTime);
        }

        private string BuildDebugOutputString(string name)
        {
            return name + "\nSPACE: switch playgrounds, \nR: auto-rotate\nCamera: mouse click";
        }
    }
}
