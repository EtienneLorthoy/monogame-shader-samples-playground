using System;

namespace MonogameShaderPlayground
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
           using var game = new MonogameShaderPlayground.Game1();
            game.Run();
        }
    }
}
