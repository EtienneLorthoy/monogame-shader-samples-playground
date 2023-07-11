using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonogameShaderPlayground.Helpers
{
    /// <summary>
    /// Draws a simple gizmo with the X, Y and Z axis. to help figure out where the camera is looking.
    /// Quite useful when you are trying to figure out why your shaders/meshes aren't looking how/where you think they should.
    /// </summary>
    public class Gizmo : DrawableGameComponent
    {
        private VertexPositionColor[] vertices;
        private VertexBuffer buffer;
        private BasicEffect effect;
        private float axisLength;
        private Vector3 position;

        public Gizmo(Game game, Vector3 position, float axisLength = 1000f) : base(game)
        {
            this.axisLength = axisLength;
            this.position = position;
        }

        protected override void LoadContent()
        {
            // Vertices
            UpdatePosition(position);

            // Shader
            effect = new BasicEffect(Game.GraphicsDevice);
            effect.VertexColorEnabled = true;
            effect.World = Matrix.Identity;
            effect.LightingEnabled = false;
            effect.Projection = (Game as Game1).Camera.ProjectionMatrix;

            base.LoadContent();
        }

        public void UpdatePosition(Vector3 position)
        {
            this.position = position;
            var p = position;
            
            vertices = new VertexPositionColor[6];

            // X
            vertices[0] = new VertexPositionColor(p, Color.Red);
            vertices[1] = new VertexPositionColor(new Vector3(axisLength + p.X, p.Y, p.Z), Color.Red);

            // Y 
            vertices[2] = new VertexPositionColor(p, Color.Green);
            vertices[3] = new VertexPositionColor(new Vector3(p.X, axisLength + p.Y, p.Z), Color.Green);

            // Z
            vertices[4] = new VertexPositionColor(p, Color.Blue);
            vertices[5] = new VertexPositionColor(new Vector3(p.X, p.Y, axisLength + p.Z), Color.Blue);

            // Vertex Buffer
            buffer = new VertexBuffer(Game.GraphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            buffer.SetData(vertices);
        }

        public override void Update(GameTime gameTime)
        {
            effect.View = (Game as Game1).Camera.ViewMatrix;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Game.GraphicsDevice.SetVertexBuffer(buffer);
                Game.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, vertices.Length / 2);
            }

            base.Draw(gameTime);
        }
    }
}
