using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonogameShaderPlayground.Helpers
{
    /// <summary>
    /// This is a basic Camera with basic movement and rotation.
    /// </summary>
    public class BasicCamera : GameComponent
    {
        public ref Matrix ViewMatrix => ref view;
        public Vector3 Direction { get => Vector3.Forward; }
        public Matrix Projection { get; protected set; }
        public Vector3 Position { get => cameraPosition; set => cameraPosition = value; }
        public Vector3 Target { get => cameraTarget; set => cameraTarget = value; }

        private Matrix view;
        private Vector3 cameraPosition;
        private Vector3 cameraTarget;
        private Vector3 cameraUp;

        public BasicCamera(Game game, Vector3 target, Vector3 up)
            : base(game)
        {
            // Build camera view matrix
            cameraPosition = new Vector3(2, 1, 2);
            cameraUp = up;

            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, Game.Window.ClientBounds.Width / (float)Game.Window.ClientBounds.Height, 1, 1024);
        }

        public override void Update(GameTime gameTime)
        {
            // Mouse logic captive/free
            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                cameraPosition.X = (float)(Math.Sin(Mouse.GetState().X / 100f) * 2f);
                cameraPosition.Z = (float)(Math.Cos(Mouse.GetState().X / 100f) * 2f);
                cameraPosition.Y = (float)(Math.Cos(Mouse.GetState().Y / 100f) * 2f);
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                cameraPosition.X = (float)(Math.Cos(gameTime.TotalGameTime.TotalSeconds) * 2f);
                cameraPosition.Z = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds) * 2f);
                cameraPosition.Y = (float)(Math.Cos(gameTime.TotalGameTime.TotalSeconds * 3) * 2f);
            }

            // Update Camera
            if (Game.IsActive)
            {
                Matrix.CreateLookAt(ref cameraPosition, ref cameraTarget, ref cameraUp, out view);
            }

            base.Update(gameTime);
        }
    }
}