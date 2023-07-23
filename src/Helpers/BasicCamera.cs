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
        public ref Matrix ProjectionMatrix => ref projection;

        public Vector3 Direction { get => cameraDirection; }
        public Vector3 Position { get => cameraPosition; set => cameraPosition = value; }
        public Vector3 Target { get => cameraTarget; set => cameraTarget = value; }

        public float SwingFactor;
        public bool PotatoMode = true;

        private Matrix view;
        private Matrix projection;
        private Vector3 cameraPosition;
        private Vector3 cameraTarget;
        private Vector3 cameraDirection;
        private Vector3 cameraUp;

        private double speed;
        private MouseState lastMouseState;
        private KeyboardState lastKeyboardState;

        public BasicCamera(Game game, Vector3 target, Vector3 up)
            : base(game)
        {
            // Build camera view matrix
            cameraPosition = new Vector3(2, 1, 2);
            cameraTarget = target;
            cameraUp = up;
            SwingFactor = 2f;
        }

        public override void Initialize()
        {
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, Game.Window.ClientBounds.Width / (float)Game.Window.ClientBounds.Height, 1, 1024);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // Mouse logic captive/free
            if (PotatoMode)
            {
                if (Mouse.GetState().RightButton == ButtonState.Pressed)
                {
                    cameraPosition.X = (float)(Math.Sin(Mouse.GetState().X / 100f) * SwingFactor);
                    cameraPosition.Z = (float)(Math.Cos(Mouse.GetState().X / 100f) * SwingFactor);
                    cameraPosition.Y = (float)(Math.Cos(Mouse.GetState().Y / 100f) * SwingFactor);
                }
                if (Mouse.GetState().LeftButton == ButtonState.Pressed && Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                {
                    cameraTarget.X = (float)(Math.Sin(Mouse.GetState().X / 100f) * 1f);
                    cameraTarget.Z = (float)(Math.Cos(Mouse.GetState().X / 100f) * 1f);
                    cameraTarget.Y = (float)(Math.Sin(Mouse.GetState().Y / 100f) + 1f) * 2;
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.R))
                {
                    cameraPosition.X = (float)(Math.Cos(gameTime.TotalGameTime.TotalSeconds) * SwingFactor);
                    cameraPosition.Z = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds) * SwingFactor);
                    cameraPosition.Y = (float)(Math.Cos(gameTime.TotalGameTime.TotalSeconds * 3) * SwingFactor);
                }
                cameraDirection = Vector3.Normalize(cameraTarget - cameraPosition);
                cameraTarget = Vector3.Zero;
            }
            else
            {
                // Speed Shift 
                if (Keyboard.GetState().IsKeyDown(Keys.LeftShift)) speed = 30f;
                else speed = 10f;

                var displacementAmount = (float)(speed * gameTime.ElapsedGameTime.TotalSeconds);

                // Move forward and backward
                if (Keyboard.GetState().IsKeyDown(Keys.W)) cameraPosition += Direction * displacementAmount;
                if (Keyboard.GetState().IsKeyDown(Keys.S)) cameraPosition -= Direction * displacementAmount;

                if (Keyboard.GetState().IsKeyDown(Keys.A)) cameraPosition += Vector3.Cross(Vector3.UnitY, cameraDirection) * displacementAmount;
                if (Keyboard.GetState().IsKeyDown(Keys.D)) cameraPosition -= Vector3.Cross(Vector3.UnitY, cameraDirection) * displacementAmount;

                if (Mouse.GetState().RightButton == ButtonState.Pressed)
                {
                    var mouseDeltaX = (Mouse.GetState().X - lastMouseState.X);
                    var mouseDeltaY = (Mouse.GetState().Y - lastMouseState.Y);
                    cameraDirection = Vector3.Normalize(cameraTarget - cameraPosition);
                    cameraDirection = Vector3.Transform(cameraDirection, Matrix.CreateFromAxisAngle(cameraUp, (-MathHelper.PiOver4 / 150) * (mouseDeltaX)));
                    cameraDirection = Vector3.Transform(cameraDirection, Matrix.CreateFromAxisAngle(Vector3.Cross(cameraUp, cameraDirection), (MathHelper.PiOver4 / 100) * (mouseDeltaY)));
                }
                
                cameraTarget = cameraPosition + cameraDirection;
            }

            if (Keyboard.GetState().IsKeyUp(Keys.F) && lastKeyboardState.IsKeyDown(Keys.F)) PotatoMode = !PotatoMode;


            // Update Camera
            Matrix.CreateLookAt(ref cameraPosition, ref cameraTarget, ref cameraUp, out view);

            lastMouseState = Mouse.GetState();
            lastKeyboardState = Keyboard.GetState();

            base.Update(gameTime);
        }
    }
}