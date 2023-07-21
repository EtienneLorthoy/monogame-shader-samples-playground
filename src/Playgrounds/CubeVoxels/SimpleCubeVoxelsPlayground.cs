using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MonogameShaderPlayground.Helpers;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace MonogameShaderPlayground.Playgrounds.CubeVoxels
{
    public class SimpleCubeVoxelsPlayground : DrawableGameComponent
    {
        private VertexBuffer vertexBuffer;

        private BasicCamera camera;

        private HotReloadShaderManager hotReloadShaderManager;
        private Effect effect;

        private Texture3D texture3D;

        private Gizmo gizmoLight;

        private int vms = 128; // voxelMatrixSize
        private int vmh = 32; // voxelMatrixHeight

        public SimpleCubeVoxelsPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
            hotReloadShaderManager = new HotReloadShaderManager(game, @"Playgrounds\CubeVoxels\SimpleCubeVoxelsShader.fx");

            this.gizmoLight = new Gizmo(game, new Vector3(0, 0, 0), 5.0f);
            Game.Components.Add(gizmoLight);
        }

        public override void Initialize()
        {
            effect = hotReloadShaderManager.Load("Shaders/SimpleCubeVoxelsShader");
            effect.Parameters["ScreenSize"].SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));


            var meshVertices = new List<VertexPositionNormalTexture>();
            meshVertices.AddRange(VertexsBuilderHelper.ConstructVertexPositionNormalTextureCube(Vector3.Zero, 100f));
            // for (int i = -1; i < 2; i++)
            //     for (int j = -1; j < 2; j++)
            //         for (int k = -1; k < 2; k++)
            //         {
            //             meshVertices.AddRange(VertexsBuilderHelper.ConstructVertexPositionNormalTextureCube(new Vector3(2f * i, 2f * j, 2f * k), 8f));
            //         }
            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), meshVertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(meshVertices.ToArray());

            this.texture3D = new Texture3D(GraphicsDevice, vms, vmh, vms, false, SurfaceFormat.Color);
            var data = new Color[vms * vmh * vms];
            for (int x = 0; x < vms; x++)
                for (int y = 0; y < vmh; y++)
                    for (int z = 0; z < vms; z++)
                    {
                        // var index = x * vms + y * vmh + z;
                        var index = x + vms * (y + vmh * z);
                        data[index] = index % 7 == 0 ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(0f, 0f, 0f, 0f);
                        // data[index] = new Color(1f, 0f, 0f, 1f);
                        // data[index] = index % 2 == 0 ? new Color(1f, 0f, 0f, 1f) : new Color(0, 0, 1f, 1f);

                        if (y == 0) data[index] = new Color(1f, 0.5f, 0.5f, 1f);
                        if (y > 1) data[index] = new Color(0f, 0f, 0f, 0f);
                        // data[index] = new Color(x / vms, y / vmh, z / vms, 1f);

                    }
            texture3D.SetData(data);
            effect.Parameters["voxelData"]?.SetValue(texture3D);

            camera.Position = new Vector3(180, 30f, 180f);
            camera.Target = new Vector3(vms / 2, 0, vms / 2);
            camera.SwingFactor = 100f;
        }

        protected override void OnEnabledChanged(object sender, EventArgs args)
        {
            if (Enabled == false) gizmoLight.Enabled = false;
            else gizmoLight.Enabled = true;

            base.OnEnabledChanged(sender, args);
        }

        public override void Update(GameTime gameTime)
        {
            if (hotReloadShaderManager.CheckForChanges()) Initialize();

            effect.Parameters["CameraPosition"].SetValue(camera.Position);
            effect.Parameters["CameraTarget"].SetValue(camera.Target);
            effect.Parameters["WorldViewProjection"].SetValue(Matrix.Identity * camera.ViewMatrix * camera.ProjectionMatrix);

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
            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount / 3);
            }
        }
    }
}
