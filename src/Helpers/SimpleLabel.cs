using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonogameShaderPlayground.Helpers
{
    public class SimpleLabel : DrawableGameComponent
    {
        public string Text = "";

        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;

        public SimpleLabel(Game game) : base(game) { }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = Game.Content.Load<SpriteFont>("Fonts/Font");
            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, Text, new Vector2(33, 60), Color.Red);
            
            spriteBatch.End();
        }
    }
}