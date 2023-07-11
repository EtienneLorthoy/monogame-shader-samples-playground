using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MonogameShaderPlayground.Helpers;
using System.Collections.Generic;

namespace MonogameShaderPlayground.Playgrounds.RayMarchingSoftShadows
{
    public class RayMarchingSoftShadowsPlayground : DrawableGameComponent
    {
        private VertexBuffer vertexBuffer;

        private BasicCamera camera;

        private HotReloadShaderManager hotReloadShaderManager;
        private Effect effect;

        private Texture2D baseColorTexture;

        private Gizmo gizmoLight;

        public RayMarchingSoftShadowsPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
            hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\RayMarchingSoftShadows\RayMarchingSoftShadowsShader.fx");
            gizmoLight = new Gizmo(game, new Vector3(0, 0, 0), 0.2f);
            Game.Components.Add(gizmoLight);
        }

        public override void Initialize()
        {
            effect = hotReloadShaderManager.Load("Shaders/RayMarchingSoftShadowsShader");
            effect.Parameters["ViewportSize"].SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

            baseColorTexture = Game.Content.Load<Texture2D>("Textures/MetalPanel/basecolor");
            effect.Parameters["ColorMap"]?.SetValue(baseColorTexture);

            // Build cubes
            // those are redefined in the shader, so you would need to somehow pass them to the shader or just update the shader code 
            var meshVertices = new List<VertexPositionNormalTexture>();
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                    for (int k = -1; k < 2; k++)
                    {
                        meshVertices.AddRange(VertexsBuilderHelper.ConstructVertexPositionNormalTextureCube(new Vector3(2f * i, 2f * j, 2f * k), 1f));
                    }
            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), meshVertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(meshVertices.ToArray());

            base.Initialize();
        }

        protected override void OnVisibleChanged(object sender, EventArgs args)
        {
            if (Visible == false) gizmoLight.Visible = false;
            else gizmoLight.Visible = true;
            base.OnVisibleChanged(sender, args);
        }

        public override void Update(GameTime gameTime)
        {
            if (Enabled == false) gizmoLight.Enabled = false;
            else gizmoLight.Enabled = true;

            if (hotReloadShaderManager.CheckForChanges()) Initialize();

            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["CameraTarget"].SetValue(camera.Target);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.ProjectionMatrix);

            // Light direction randomness can be fixed by commenting the following lines
            float x = (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds /2) * 2;
            float y = 1.5f;//((float)Math.Tan(gameTime.TotalGameTime.TotalSeconds) + 1) * 3;
            float z = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds /2) * 2;
            effect.Parameters["LightPosition"].SetValue(new Vector3(x, y, z));
            gizmoLight.UpdatePosition(new Vector3(x, y, z));

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetVertexBuffer(vertexBuffer);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3);
            }
        }
    }
}
