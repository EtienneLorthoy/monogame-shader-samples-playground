using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Helpers;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using System;

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

        public MultipleImportanceSamplingPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
            hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\MultipleImportanceSampling\MultipleImportanceSamplingShader.fx");
        }

        protected override void OnEnabledChanged(object sender, EventArgs args)
        {
            if (Enabled) 
            {
                // Cool looking angle
                camera.Position = new Vector3(-1.3f, 0.7f, -1.6f);
            }
            else
            {
                if (diffRenderTarget != null) diffRenderTarget.Dispose();
                if (lastFrameTexture != null) lastFrameTexture.Dispose();
                if (vertexBuffer != null) vertexBuffer.Dispose();
            }

            base.OnEnabledChanged(sender, args);
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

            // TODO: I could use spritebatch and calculate rays from the screen, but I'm lazy, so I'll just use vertex cube to give
            // the shader the ray it needs to reconstruct the scene, keep in mind that those geometries are not used in the shader.
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

        public override void Update(GameTime gameTime)
        {
            if (hotReloadShaderManager.CheckForChanges()) Initialize();

            if (Mouse.GetState().RightButton == ButtonState.Pressed) lerpBalance = 0.5f;
            else lerpBalance += 0.2f;

            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["CameraTarget"].SetValue(camera.Target);
            effect.Parameters["iTime"].SetValue((float)gameTime.TotalGameTime.TotalMilliseconds);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.ProjectionMatrix);
            effect.Parameters["LerpBalance"].SetValue(lerpBalance);

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
