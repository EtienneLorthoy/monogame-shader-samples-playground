using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Helpers;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonogameShaderPlayground.Playgrounds.MISCubeVoxelsDenoise
{
    public class MISCubeVoxelsDenoisePlayground : DrawableGameComponent
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

        private Texture3D texture3D;

        private Gizmo gizmoLight;

        private int vms = 128; // voxelMatrixSize
        private int vmh = 32; // voxelMatrixHeight

        public MISCubeVoxelsDenoisePlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
            hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\MISCubeVoxelsDenoise\MISCubeVoxelsDenoiseShader.fx");
        
            this.gizmoLight = new Gizmo(game, new Vector3(0, 0, 0), 5.0f);
            Game.Components.Add(gizmoLight);
        }

        protected override void OnEnabledChanged(object sender, EventArgs args)
        {
            if (Enabled) 
            {     
                // Cool looking angle
                camera.Position = new Vector3(-21, 24f, 42f);
                camera.Target = new Vector3(-20, 23.6f, 42.2f);
                camera.Target.Normalize();
                camera.PotatoMode = false;
                camera.SwingFactor = 100f;
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
            effect = hotReloadShaderManager.Load("Shaders/MISCubeVoxelsDenoiseShader");
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

            var meshVertices = new List<VertexPositionNormalTexture>();
            meshVertices.AddRange(VertexsBuilderHelper.ConstructVertexPositionNormalTextureCube(Vector3.Zero, 100f));
            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), meshVertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(meshVertices.ToArray());

            this.texture3D = new Texture3D(GraphicsDevice, vms, vmh, vms, false, SurfaceFormat.Color);
            var data = new Color[vms * vmh * vms];
            // Generated data
            for (int x = 0; x < vms; x++)
                for (int y = 0; y < vmh; y++)
                    for (int z = 0; z < vms; z++)
                    {
                        // var index = x * vms + y * vmh + z;
                        var index = x + vms * (y + vmh * z);
                        data[index] = index % 7 == 0 ? new Color(0.8f, 0.2f, 0.5f) : new Color(0f, 0f, 0f, 0f);
                        // data[index] = new Color(1f, 0f, 0f, 1f);
                        // data[index] = index % 2 == 0 ? new Color(1f, 0f, 0f, 1f) : new Color(0, 0, 1f, 1f);

                        // Floor
                        if (y == 0) data[index] = new Color(0.8f,0.8f,0.3f, 1f);
                        if (y > 2) data[index] = new Color(0f, 0f, 0f, 0f);

                        // The T shape to the right
                        if (y < 5 && x < 5 && z == 10) data[index] = new Color(0.2f, 0.2f, 1.0f, 1f);
                        if (y == 4 && x < 5 && z > 5 && z < 15) data[index] = new Color(1f, 1f, 0.2f, 1f);
                    }
            texture3D.SetData(data);
            effect.Parameters["voxelData"]?.SetValue(texture3D);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (hotReloadShaderManager.CheckForChanges()) Initialize();

            if (Mouse.GetState().RightButton == ButtonState.Pressed) lerpBalance = 0.5f;
            else lerpBalance += 0.15f;

            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["CameraTarget"].SetValue(camera.Target);
            effect.Parameters["iTime"].SetValue((float)gameTime.TotalGameTime.TotalMilliseconds);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.ProjectionMatrix);
            effect.Parameters["LerpBalance"].SetValue(lerpBalance);

            // Light direction randomness can be fixed by commenting the following lines
            float x = (float)Math.Cos(gameTime.TotalGameTime.TotalSeconds) * 20 + vms / 2;
            float y = 10;//((float)Math.Tan(gameTime.TotalGameTime.TotalSeconds) + 1) * 3;
            float z = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds) * 20 + vms / 2;
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
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Final composition from buffer A 
            effect.CurrentTechnique = effect.Techniques["Composition"];
            effect.Parameters["bufferA"]?.SetValue(diffRenderTarget);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3);
            }
        }
    }
}
