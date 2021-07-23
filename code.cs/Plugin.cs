using Microsoft.Build.BuildEngine;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace code.cs
{
    enum Mode
    {
        Edit,
        Run,
        Once,
        List,
        Delete
    }

    public class Plugin : ok.cmd.IOkPlugin
    {
        public string Command => "code.cs";
        public string Description => "Compiles reusable C# code on the fly.";
        public string Syntax => "code.cs edit/run/once scriptName (args if running...)";
        public double Version => 1.0;

        public bool PauseOnComplete => false;
        public bool RunFromSourceDirectory => false;
        public string SourceDirectory { get; set; }

        public bool Execute(params string[] args)
        {
            var mode = Mode.Edit;
            if(args.Length > 0)
            {
                if (!Enum.TryParse(args[0], true, out Mode modeOut))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Got `{args[0]}` but expected `edit`, `run` or `once`");
                    Console.ResetColor();
                    return false;
                }
                mode = modeOut;
            }
            if(args.Length < 2 && mode < Mode.Once )
            {
                return false;
            }

            var scriptFileName = "None";
            if(args.Length > 1)
                scriptFileName = args[1].ToFilename();

            var pascalName = scriptFileName.ToPascal();
            var projectFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "code.cs", "scripts", scriptFileName);
            if (mode < Mode.Once)
            {
                Directory.CreateDirectory(projectFolder);
                var scriptPath = Path.Combine(projectFolder, scriptFileName + ".cs");
                switch (mode)
                {
                    case Mode.Edit:
                        {

                            if (!File.Exists(scriptPath))
                            {
                                File.WriteAllText(scriptPath,
                                    "using System;\n" +
                                    "using System.IO;\n" +
                                    "using System.Linq;\n" +
                                    "using System.Text;\n" +
                                    "using System.Threading;\n" +
                                    "using System.Diagnostics;\n" +
                                    "using System.Collections.Generic;\n\n" +
                                    $"namespace {pascalName}\n" +
                                    "{\n" +
                                    "\tpublic class Script\n" +
                                    "\t{\n" +
                                    "\t\tpublic static void Main(string[] args)\n" +
                                    "\t\t{\n" +
                                    "\t\t\t\n" +
                                    "\t\t}\n" +
                                    "\t}\n" +
                                    "}");
                                CreateProject(projectFolder, Guid.NewGuid().ToString(), scriptFileName + ".cs", pascalName);
                            }
                            try
                            {
                                Process.Start("code", projectFolder);
                            }
                            catch
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"VSCode not installed, please download and install it.");
                                Console.ResetColor();
                                return true;
                            }
                        }
                        break;
                    case Mode.Run:
                        {
                            var binPath = Path.Combine(projectFolder, "bin", "Debug", scriptFileName.ToPascal() + ".exe");
                            if (!File.Exists(scriptPath))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Script with name `{args[1]}` does not exist. Use the `edit` switch instead of `run` to create it");
                                Console.ResetColor();
                                return true;
                            }
                            bool needToBuild = true;
                            var cachePath = Path.Combine(projectFolder, "cache.dat");
                            var newCache = "";
                            if (File.Exists(cachePath))
                            {
                                var check = "";
                                foreach (var codeFile in Directory.GetFiles(projectFolder, "*.cs"))
                                {
                                    check += File.ReadAllText(codeFile);
                                }
                                if(File.ReadAllText(cachePath) == check)
                                {
                                    needToBuild = false;
                                }
                                newCache = check;
                            }
                            var script = File.ReadAllText(scriptPath);
                            if (needToBuild || !File.Exists(binPath))
                            {
                                try
                                {
                                    BuildProject(Path.Combine(projectFolder, pascalName + ".csproj"));
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Script with name `{args[1]}` failed to build\n{ex}");
                                    Console.ResetColor();
                                    return true;
                                }
                            }
                            if (!File.Exists(binPath))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Script with name `{args[1]}` failed to build");
                                Console.ResetColor();
                                return true;
                            }
                            else
                            {
                                File.WriteAllText(cachePath, newCache);
                                var scriptProcess = new Process()
                                {
                                    StartInfo = new ProcessStartInfo(binPath, string.Join(" ", args.Skip(2).ToArray()))
                                    {
                                        UseShellExecute = false
                                    }
                                };
                                scriptProcess.Start();
                                scriptProcess.WaitForExit();
                            }
                        }
                        break;
                }
                return true;
            }
            else
            {
                switch (mode)
                {
                    case Mode.Once:
                        {
                            List<string> code = new List<string>();
                            string codeString;
                            while (true)
                            {
                                var line = Console.ReadLine();
                                if (line.ToLower() == "end")
                                {
                                    codeString =
                                        "using System;\n" +
                                        "using System.IO;\n" +
                                        "using System.Linq;\n" +
                                        "using System.Text;\n" +
                                        "using System.Threading;\n" +
                                        "using System.Diagnostics;\n" +
                                        "using System.Collections.Generic;\n\n" +
                                        $"namespace FireAndForget\n" +
                                        "{\n" +
                                        "\tpublic class Script\n" +
                                        "\t{\n" +
                                        "\t\tpublic static void Run()\n" +
                                        "\t\t{\n" +
                                        "\t\t\t\n" + string.Join("\n\t\t\t", code) +
                                        "\t\t}\n" +
                                        "\t}\n" +
                                        "}";
                                    break;
                                }
                                else if (line.ToLower() == "del")
                                {
                                    ClearLastLine(code);
                                }
                                else
                                {
                                    code.Add(line.Trim());
                                }
                            }
                            Console.WriteLine("----------------------------------");
                            var asm = BuildAssembly(codeString);
                            var runMethod = asm.GetType("FireAndForget.Script").GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
                            runMethod.Invoke(null, null);
                            Console.WriteLine("----------------------------------");
                            return true;
                        }
                    case Mode.List:
                        {
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine("Scripts:");
                            Console.ForegroundColor = ConsoleColor.White;
                            foreach(var dir in Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "code.cs", "scripts")))
                            {
                                Console.WriteLine(Path.GetFileName(dir));
                            }
                            Console.ResetColor();
                            return true;
                        }
                    case Mode.Delete:
                        {
                            if(args.Length < 2 || !Directory.Exists(projectFolder))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Got `{pascalName}` but expected the name of a script to delete.\n" +
                                                  $"Use `list` to view script names.");
                                Console.ResetColor();
                                return true;
                            }
                            else
                            {
                                Directory.Delete(projectFolder, true);
                                Console.WriteLine($"Deleted `{pascalName}`.");
                                return true;
                            }
                        }
                }
                return true;
            }
        }

        public static void ClearLastLine(List<string> code)
        {
            if(code.Count == 0)
            {
                return;
            }
            code.Remove(code.Last());
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - code.Count - 3);
            foreach(var line in code)
            {
                Console.WriteLine(line + new string(' ', Console.BufferWidth - line.Length - 1));
            }
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 2);
        }

        private void BuildProject(string projectFile)
        {
            Microsoft.Build.Evaluation.Project p = new Microsoft.Build.Evaluation.Project(projectFile);
            p.SetGlobalProperty("Configuration", "Debug");
            p.Build();
        }

        private Assembly BuildAssembly(string code)
        {
            var compiler = CodeDomProvider.CreateProvider("CSharp");
            CompilerParameters compilerparams = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
            };
            compilerparams.ReferencedAssemblies.AddRange( new string[] {
                            "System.dll",
                            "System.IO.dll",
                            "System.Linq.dll",
                            "System.Threading.dll", });
            CompilerResults results = compiler.CompileAssemblyFromSource(compilerparams, code);
            if (results.Errors.HasErrors)
            {
                StringBuilder errors = new StringBuilder("Compiler Errors :\r\n");
                foreach (CompilerError error in results.Errors)
                {
                    errors.AppendFormat("Line {0},{1}\t: {2}\n", error.Line, error.Column, error.ErrorText);
                }
                throw new Exception(errors.ToString());
            }
            else
            {
                return results.CompiledAssembly;
            }
        }

        private void CreateProject(string projectFolder, string guid, string codeFile, string pascalName)
        {
            var projText =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Project ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n" +
                "\t<Import Project=\"$(MSBuildExtensionsPath)\\$(MSBuildToolsVersion)\\Microsoft.Common.props\" Condition=\"Exists('$(MSBuildExtensionsPath)\\$(MSBuildToolsVersion)\\Microsoft.Common.props')\" />\n" +
                "\t<PropertyGroup>\n" +
                "\t\t<Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>\n" +
                "\t\t<Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>\n" +
                "\t\t<ProjectGuid>{" + guid + "}</ProjectGuid>\n" +
                "\t\t<OutputType>Exe</OutputType>\n" +
                "\t\t<RootNamespace>" + pascalName + "</RootNamespace>\n" +
                "\t\t<AssemblyName>" + pascalName + "</AssemblyName>\n" +
                "\t\t<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>\n" +
                "\t\t<FileAlignment>512</FileAlignment>\n" +
                "\t\t<AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>\n" +
                "\t\t<Deterministic>true</Deterministic>\n" +
                "\t</PropertyGroup>\n" +
                "\t<PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \">\n" +
                "\t\t<PlatformTarget>AnyCPU</PlatformTarget>\n" +
                "\t\t<DebugSymbols>true</DebugSymbols>\n" +
                "\t\t<DebugType>full</DebugType>\n" +
                "\t\t<Optimize>false</Optimize>\n" +
                "\t\t<OutputPath>bin\\Debug\\</OutputPath>\n" +
                "\t\t<DefineConstants>DEBUG;TRACE</DefineConstants>\n" +
                "\t\t<ErrorReport>prompt</ErrorReport>\n" +
                "\t\t<WarningLevel>4</WarningLevel>\n" +
                "\t</PropertyGroup>\n" +
                "\t<PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' \">\n" +
                "\t\t<PlatformTarget>AnyCPU</PlatformTarget>\n" +
                "\t\t<DebugType>pdbonly</DebugType>\n" +
                "\t\t<Optimize>true</Optimize>\n" +
                "\t\t<OutputPath>bin\\Release\\</OutputPath>\n" +
                "\t\t<DefineConstants>TRACE</DefineConstants>\n" +
                "\t\t<ErrorReport>prompt</ErrorReport>\n" +
                "\t\t<WarningLevel>4</WarningLevel>\n" +
                "\t</PropertyGroup>\n" +
                "\t<ItemGroup>\n" +
                "\t\t<Reference Include=\"System\" />\n" +
                "\t\t<Reference Include=\"System.Core\" />\n" +
                "\t\t<Reference Include=\"System.Xml.Linq\" />\n" +
                "\t\t<Reference Include=\"System.Data.DataSetExtensions\" />\n" +
                "\t\t<Reference Include=\"Microsoft.CSharp\" />\n" +
                "\t\t<Reference Include=\"System.Data\" />\n" +
                "\t\t<Reference Include=\"System.Net.Http\" />\n" +
                "\t\t<Reference Include=\"System.Xml\" />\n" +
                "\t</ItemGroup>\n" +
                "\t<ItemGroup>\n" +
                "\t\t<Compile Include=\"" + codeFile + "\" />\n" +
                "\t</ItemGroup>\n" +
                "\t<ItemGroup>\n" +
                "\t\t<None Include=\"App.config\" />\n" +
                "\t</ItemGroup>\n" +
                "\t<Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />\n" +
                "</Project>";
            File.WriteAllText(Path.Combine(projectFolder, pascalName + ".csproj"), projText);
            File.WriteAllText(Path.Combine(projectFolder, "App.config"),
                "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" +
                "<configuration>\n" +
                "\t\t<startup>\n" +
                "\t\t\t<supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.8\" />\n" +
                "\t\t</startup>\n" +
                "</configuration>");
        }
    }
}
