using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonogameShaderPlayground.Cameras;
using MonogameShaderPlayground.Helpers;
using System.Drawing;
using System.Windows.Forms;
using Color = Microsoft.Xna.Framework.Color;

namespace MonogameShaderPlayground.Primitives
{
    public class ImageFromClipBoardPlayground : DrawableGameComponent
    {
        private VertexPositionTexture[] meshVertices;
        private BasicCamera camera;
        private BasicEffect effect;

        public ImageFromClipBoardPlayground(Game game, BasicCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Initialize()
        {
            effect = new BasicEffect(Game.GraphicsDevice);
            effect.World = Matrix.Identity;
            effect.Projection = camera.Projection;
            effect.LightingEnabled = false;
            effect.VertexColorEnabled = false;

            effect.TextureEnabled = true;
            effect.Texture = Game.Content.Load<Texture2D>("Shaders/RaymarchingTexture");;

            meshVertices = VertexsBuilderHelper.ConstructVertexPositionTextureCube(new Vector3(-0.5f, -0.5f, -0.5f), 1f);
        }

        public override void Update(GameTime gameTime)
        {
            effect.View = camera.ViewMatrix;

            if (Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.V))
            {
                effect.Texture = GetTexture2DFromClipboard(GraphicsDevice);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            // Set depth tencil 
            this.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            // Cube to texture from clipboard
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                // GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, cubeVertexBuffer.VertexCount / 3);
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, meshVertices, 0, meshVertices.Length / 3);
            }
        }

        public static Texture2D GetTexture2DFromClipboard(GraphicsDevice graphicsDevice)
        {
            if (!Clipboard.ContainsImage())
            {
                return null;
            }

            using (Image clipboardImage = Clipboard.GetImage())
            {
                using (Bitmap bmp = new Bitmap(clipboardImage))
                {
                    int width = bmp.Width;
                    int height = bmp.Height;
                    Texture2D texture = new Texture2D(graphicsDevice, width, height);
                    Color[] data = new Color[width * height];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            System.Drawing.Color pixel = bmp.GetPixel(x, y);
                            data[x + y * width] = new Color(pixel.R, pixel.G, pixel.B, pixel.A);
                        }
                    }

                    texture.SetData(data);
                    return texture;
                }
            }
        }
    }
}
