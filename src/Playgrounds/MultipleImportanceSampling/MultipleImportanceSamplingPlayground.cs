using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MonogameShaderPlayground.Helpers;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace MonogameShaderPlayground.Playgrounds.MultipleImportanceSampling
{
    public class MultipleImportanceSamplingPlayground : DrawableGameComponent
    {
        private VertexBuffer vertexBuffer;

        private BasicCamera camera;

        private HotReloadShaderManager hotReloadShaderManager;
        private Effect effect;
        private RenderTarget2D diffRenderTarget;
        private Texture2D lastFrameTexture;
        private Color[] lastFrameTextureData;
        private SpriteBatch spriteBatch;

        private float lerpBalance = 0f;

        private Gizmo gizmoLight;

        public MultipleImportanceSamplingPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
            hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\MultipleImportanceSampling\MultipleImportanceSamplingShader.fx");
            gizmoLight = new Gizmo(game, new Vector3(0, 0, 0), 0.2f);
            Game.Components.Add(gizmoLight);
        }

        public override void Initialize()
        {
            effect = hotReloadShaderManager.Load("Shaders/MultipleImportanceSamplingShader");
            effect.Parameters["ViewportSize"].SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            effect.Parameters["LerpBalance"].SetValue(0.5f);

            lastFrameTexture = new Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            lastFrameTextureData = new Color[GraphicsDevice.Viewport.Width * GraphicsDevice.Viewport.Height];
            
            spriteBatch = new SpriteBatch(GraphicsDevice);
            if (diffRenderTarget != null) diffRenderTarget.Dispose();
            diffRenderTarget = new RenderTarget2D(GraphicsDevice, 
                                                    GraphicsDevice.Viewport.Width, 
                                                    GraphicsDevice.Viewport.Height, 
                                                    false, 
                                                    GraphicsDevice.PresentationParameters.BackBufferFormat, 
                                                    DepthFormat.None, 
                                                    0, 
                                                    RenderTargetUsage.PlatformContents);

            // Build cubes
            // those are redefined in the shader, so you would need to somehow pass them to the shader or just update the shader code 
            var meshVertices = new List<VertexPositionNormalTexture>();
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                    for (int k = -1; k < 2; k++)
                    {
                        meshVertices.AddRange(VertexsBuilderHelper.ConstructVertexPositionNormalTextureCube(new Vector3(2f * i, 2f * j, 2f * k), 2f));
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

            if (Mouse.GetState().RightButton == ButtonState.Pressed) lerpBalance = 0.5f;
            else lerpBalance += 0.2f;

            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["CameraTarget"].SetValue(camera.Target);
            effect.Parameters["iTime"].SetValue((float)gameTime.TotalGameTime.TotalMilliseconds);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.ProjectionMatrix);

            effect.Parameters["LerpBalance"].SetValue(lerpBalance);

            // Light direction randomness can be fixed by commenting the following lines
            float x = (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds /2);
            float y = 0.5f;
            float z = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds /2);
            effect.Parameters["LightPosition"].SetValue(new Vector3(x, y, z));
            gizmoLight.UpdatePosition(new Vector3(x, y, z));

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            // Convert the old frame in a texture
            diffRenderTarget.GetData<Color>(lastFrameTextureData);
            lastFrameTexture.SetData<Color>(lastFrameTextureData);

            // Draw the scene in buffer A
            GraphicsDevice.SetRenderTarget(diffRenderTarget);
            effect.CurrentTechnique = effect.Techniques["BufferA"];
            effect.Parameters["iChannel0"]?.SetValue(lastFrameTexture);
            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3);
            }
            GraphicsDevice.SetRenderTarget(null);

            // Final composition from buffer A 
            effect.CurrentTechnique = effect.Techniques["Composition"];
            effect.Parameters["bufferA"]?.SetValue(diffRenderTarget);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, effect, null);
            spriteBatch.Draw(diffRenderTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}