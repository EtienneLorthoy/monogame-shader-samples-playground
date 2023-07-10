using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonogameShaderPlayground.Helpers
{
    /// <summary>
    /// Hot reload shader manager! It will reload the shader if the source shader file is changed.
    /// Tremendously useful for shader development as you don't have to restart the game every time you change the shader.
    /// It will also show the compilation errors in the game window if any (while keeping the render from previous shader)
    /// </summary>
    public class HotReloadShaderManager
    {
        private static readonly string BinPath = System.Reflection
                                                        .Assembly
                                                        .GetEntryAssembly()
                                                        .Location
                                                        .Substring(0, System.Reflection
                                                                            .Assembly
                                                                            .GetEntryAssembly()
                                                                            .Location
                                                                            .LastIndexOf('\\') + 1);

        private static readonly string SrcPath = new DirectoryInfo(Path.Combine(BinPath, "..\\..\\..\\")).FullName;

        private string fXFilePath;
        private string shaderIdentifier;

        private ContentManager TempContent;
        private DateTime LastUpdate;

        private Effect Shader;
        private ContentManager content;
        private GraphicsDevice device;
        private SimpleLabel label;

        public HotReloadShaderManager(Game game, string relativeFxFilePath)
        {
            this.fXFilePath = SrcPath + relativeFxFilePath;

            if (!File.Exists(fXFilePath)) throw new ArgumentException("HotReloadShaderManager: Can't find " + fXFilePath);

            var game1 = game as Game1;
            this.content = game.Content;
            this.device = game.GraphicsDevice;
            this.label = game1.playgroundInfolabel;
            this.Shader = null;
            this.TempContent = new ContentManager(content.ServiceProvider, content.RootDirectory);
            this.LastUpdate = DateTime.Now;
        }

        public bool CheckForChanges()
        {
            var isChanges = false;
            var file = new FileInfo(fXFilePath);
            if (file.LastWriteTime > LastUpdate)
            {
                ShaderChanged();
                LastUpdate = file.LastWriteTime;
                isChanges = true;
            }
            return isChanges;
        }

        private void ShaderChanged()
        {
            string filename = Path.GetFileNameWithoutExtension(fXFilePath);

            // Folder and files check
            var shaderSourceFolder = new DirectoryInfo(Path.GetDirectoryName(fXFilePath));
            if (shaderSourceFolder == null) label.Text = "HotReloadShaderManager: Can't find Shaders source folder";

            // Prepare the build output folder (for building the shader)
            var shaderBuildOutputFolder = Path.Combine(new DirectoryInfo(SrcPath).GetDirectories("bin").FirstOrDefault().FullName, "hotreloadshaders");
            if (Directory.Exists(shaderBuildOutputFolder)) Directory.Delete(shaderBuildOutputFolder, true);
            Directory.CreateDirectory(shaderBuildOutputFolder);

            // Shader output folder (for the actual Monogame runtime)
            var shaderOutputFolder = new DirectoryInfo(BinPath + "Content//Shaders");
            if (shaderOutputFolder == null) label.Text = "HotReloadShaderManager: Can't find Shaders output folder";

            var mgcbBuildArguments = " mgcb /platform:Windows /config: /profile:HiDef /compress:False /importer:EffectImporter /r" 
                + " /processor:EffectProcessor /processorParam:DebugMode=Auto /build:" + fXFilePath
                + " /outputDir:" + shaderBuildOutputFolder;

            // Prepare the process to run
            Process pProcess = new Process
            {
                StartInfo =
                {
                    FileName = "dotnet",
                    Arguments = mgcbBuildArguments,
                    CreateNoWindow = true,
                    WorkingDirectory = SrcPath,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            //Get program output
            string stdError = null;
            StringBuilder stdOutput = new StringBuilder();
            pProcess.OutputDataReceived += (sender, args) => stdOutput.Append(args.Data);

            // Execute the process
            try
            {
                pProcess.Start();
                pProcess.BeginOutputReadLine();
                stdError = pProcess.StandardError.ReadToEnd();
                pProcess.WaitForExit();

                if (pProcess.ExitCode != 0)
                {
                    label.WriteLine("HotReloadShaderManager: Error while building shader " + filename + ".fx"
                        + stdError
                        + stdOutput.ToString()); 
                    return;
                }
                pProcess.Dispose();

                // Copy the xnb to the output folder
                string builtPath = new DirectoryInfo(shaderBuildOutputFolder).GetFiles(filename + ".xnb", SearchOption.AllDirectories)
                                                                              .FirstOrDefault().FullName;
                string movePath = shaderOutputFolder + "\\" + filename + ".xnb";
                File.Move(builtPath, movePath, true);

                ContentManager newTemp = new ContentManager(TempContent.ServiceProvider, TempContent.RootDirectory);

                TempContent.Unload();
                TempContent.Dispose();
                TempContent = newTemp;
                Shader = newTemp.Load<Effect>(shaderIdentifier);
                label.Text = "Shader " + filename + " reloaded";
            }
            catch (Exception e)
            {
                label.WriteLine(e.Message);
            }
            finally
            {

            }
        }

        public Effect Load(string name)
        {
            this.shaderIdentifier = name;
            if (Shader == null) Shader = TempContent.Load<Effect>(name);
            return Shader;
        }
    }
}