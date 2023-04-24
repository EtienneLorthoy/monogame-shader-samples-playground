using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonogameShaderPlayground.Helpers
{
    #if DEBUG
    public static class HotReloadShaderManager
    {
        private static readonly string BasePath = System.Reflection
                                                        .Assembly
                                                        .GetEntryAssembly()
                                                        .Location
                                                        .Substring(0, System.Reflection
                                                                            .Assembly
                                                                            .GetEntryAssembly()
                                                                            .Location
                                                                            .LastIndexOf('\\') + 1);

        private static ContentManager TempContent;
        private static DateTime LastUpdate;
        private static string FXFilesFolderPath = BasePath + "../../Content/Shaders/";

        private static Dictionary<string, Effect> Shaders;
        private static ContentManager content;
        private static GraphicsDevice device;
        private static SimpleLabel label;

        public static void Initialize(Game game)
        {
            var game1 = game as Game1;
            HotReloadShaderManager.content = game.Content;
            HotReloadShaderManager.device = game.GraphicsDevice;
            HotReloadShaderManager.label = game1.label;
            HotReloadShaderManager.Shaders = new Dictionary<string, Effect>();
            HotReloadShaderManager.TempContent = new ContentManager(content.ServiceProvider, content.RootDirectory);
            HotReloadShaderManager.LastUpdate = DateTime.Now;
        }

        public static bool CheckForChanges()
        {
            var isChanges = false;
            var files = Directory.GetFiles(FXFilesFolderPath, "*.fx");
            foreach (var file in files)
            {
                var t = File.GetLastWriteTime(file);
                if (t > LastUpdate)
                {
                    ShaderChanged(file);
                    LastUpdate = t;
                    isChanges = true;
                }
            }
            return isChanges;
        }

        private static void ShaderChanged(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);

            //Folder and files check
            var baseFolder = new DirectoryInfo(BasePath).Parent.Parent.Parent;
            var shadersSourceFolder = baseFolder.GetDirectories("Shaders", SearchOption.AllDirectories).FirstOrDefault();
            var packagesFolder = baseFolder.GetDirectories("packages", SearchOption.AllDirectories).FirstOrDefault(); 
            var shadersOutputFolder = new DirectoryInfo(BasePath).GetDirectories("Shaders", SearchOption.AllDirectories).FirstOrDefault();

            if (shadersSourceFolder == null) label.Text = "HotReloadShaderManager: Can't find Shaders source folder";
            if (packagesFolder == null) label.Text = "HotReloadShaderManager: Can't find packages folder";
            if (shadersOutputFolder == null) label.Text = "HotReloadShaderManager: Can't find shaders output folder";

            var mgcbPath = Path.Combine(packagesFolder.FullName, "dotnet-mgcb", "3.8.1.303", "tools", "net6.0", "any", "mgcb.dll");
            if (!File.Exists(mgcbPath)) label.Text = "HotReloadShaderManager: Can't find mgcb.dll";

            var mgcbBuildArguments = " /platform:Windows /config: /profile:HiDef /compress:False /importer:EffectImporter /processor:EffectProcessor /processorParam:DebugMode=Auto /build:"+name+".fx";

            // Prepare the process to run
            Process pProcess = new Process
            {
                StartInfo =
                {
                    FileName = "dotnet",
                    Arguments = mgcbPath + mgcbBuildArguments,
                    CreateNoWindow = true,
                    WorkingDirectory = shadersSourceFolder.ToString(),
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
                    label.Text = "HotReloadShaderManager: Error while building shader " + name + ".fx";
                    label.Text += stdError;
                    label.Text += stdOutput.ToString();
                    return;
                }
                pProcess.Dispose();

                // Copy the xnb to the output folder
                string builtPath = shadersSourceFolder + "\\" + name + ".xnb";
                string movePath = shadersOutputFolder + "\\" + name + ".xnb";
                File.Copy(builtPath, movePath, true);

                ContentManager newTemp = new ContentManager(TempContent.ServiceProvider, TempContent.RootDirectory);
                var newShaders = new Dictionary<string, Effect>();
                foreach (var shaderName in Shaders.Keys)
                {
                    var effect = newTemp.Load<Effect>(shaderName);
                    newShaders.Add(shaderName.ToLower(), effect);
                }

                TempContent.Unload();
                TempContent.Dispose();
                TempContent = newTemp;
                Shaders = newShaders;
                label.Text = "Shader " + name + " reloaded";
            }
            catch (Exception e)
            {
                label.Text = e.Message;
            }
            finally
            {

            }
        }

        public static Effect Load(string name)
        {
            if (!Shaders.ContainsKey(name.ToLower()))
            {
                var shader = content.Load<Effect>(name);
                Shaders.Add(name.ToLower(), shader);
            }
            else
            {
                Shaders[name.ToLower()] = TempContent.Load<Effect>(name);
            }

            return Shaders[name.ToLower()];
        }
    }
    #endif
}