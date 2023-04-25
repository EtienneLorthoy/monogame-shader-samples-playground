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
        VertexPositionColor[] vertices;
        VertexBuffer buffer;
        BasicEffect effect;

        public Gizmo(Game game) : base(game)
        {
        }

        protected override void LoadContent()
        {
            vertices = new VertexPositionColor[6];

            // X
            vertices[0] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Red);
            vertices[1] = new VertexPositionColor(new Vector3(1000, 0, 0), Color.Red);

            // Y 
            vertices[2] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Green);
            vertices[3] = new VertexPositionColor(new Vector3(0, 1000, 0), Color.Green);

            // Z
            vertices[4] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Blue);
            vertices[5] = new VertexPositionColor(new Vector3(0, 0, 1000), Color.Blue);

            // Vertex Buffer
            buffer = new VertexBuffer(Game.GraphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
            buffer.SetData(vertices);

            // Shader
            effect = new BasicEffect(Game.GraphicsDevice);
            effect.VertexColorEnabled = true;
            effect.World = Matrix.Identity;
            effect.LightingEnabled = false;
            effect.Projection = (Game as Game1).Camera.Projection;

            base.LoadContent();
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
