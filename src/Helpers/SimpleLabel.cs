using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            this.DrawOrder = 1;
            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, Text, new Vector2(33, 60), Color.Red);
            
            spriteBatch.End();
        }

        public void WriteLine(string line)
        {
            const int maxLineLength = 100;

            // Split the input string into words
            var words = line.Split(' ');

            var currentLine = string.Empty;
            var result = new StringBuilder();

            foreach (var word in words)
            {
                // If the word is longer than the maximum line length,
                // split it into chunks of maxLineLength characters
                if (word.Length > maxLineLength)
                {
                    var chunks = SplitLongWord(word, maxLineLength);
                    foreach (var chunk in chunks)
                    {
                        // If adding the current chunk to the current line
                        // would make the line too long, add the current line
                        // to the result and start a new line
                        if (currentLine.Length + chunk.Length > maxLineLength)
                        {
                            result.AppendLine(currentLine.TrimEnd());
                            currentLine = string.Empty;
                        }

                        // Add the current chunk to the current line
                        currentLine += chunk;
                    }
                }
                else
                {
                    // If adding the current word to the current line
                    // would make the line too long, add the current line
                    // to the result and start a new line
                    if (currentLine.Length + word.Length + 1 > maxLineLength)
                    {
                        result.AppendLine(currentLine.TrimEnd());
                        currentLine = string.Empty;
                    }

                    // Add the current word to the current line
                    currentLine += word + " ";
                }
            }

            // Add the final line to the result
            result.AppendLine(currentLine.TrimEnd());

            // Convert the StringBuilder to a string and return it
            Text = result.ToString().TrimEnd();
        }

        private static IEnumerable<string> SplitLongWord(string word, int maxLength)
        {
            for (int i = 0; i < word.Length; i += maxLength)
            {
                var chunkLength = Math.Min(maxLength, word.Length - i);
                yield return word.Substring(i, chunkLength);
            }
        }
    }
}