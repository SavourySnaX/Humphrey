using System;
using System.Runtime.InteropServices;

using Humphrey.FrontEnd;
using Humphrey.Backend;
using System.Collections.Generic;
using System.IO;
using LLVMSharp.Interop;

namespace Humphrey.Experiments
{
    unsafe class Program
    {
        enum ExitCodes
        {
            Ok = 0,
            InvalidArguments = 1,
            CompilationFailure = 2,
        }

        struct Options
        {
            public List<string> inputFiles;
            public string outputFileName;
            public string target;
            public bool debugLog;
            public bool infoLog;
            public bool warningsAsErrors;
            public bool emitLLVM;
            public bool optimisations;
            public bool debugInfo;
        }

        static Options options;

        static void InitialiseOptions()
        {
            options.outputFileName = null;
            options.inputFiles = new List<string>();
            options.debugLog = false;
            options.infoLog = true;
            options.warningsAsErrors = false;
            options.target = LLVMTargetRef.DefaultTriple;
            options.emitLLVM = false;
            options.optimisations = true;
            options.debugInfo = false;
        }

        static void ShowOptions()
        {
            Console.WriteLine($"Humphrey.exe [options] inputs");
            Console.WriteLine();
            Console.WriteLine($"Options are case sensistive!");
            Console.WriteLine();
            Console.WriteLine($"-o=<filename>                Output filename and path (Default: compile and dump disassembly)");
            Console.WriteLine($"--output=<filename>");
            Console.WriteLine();
            Console.WriteLine($"--debugLog[=<bool>]          Enable/Disable logging of debug messages (Default: {options.debugLog})");
            Console.WriteLine($"--infoLog[=<bool>]           Enable/Disable logging of information messages (Default: {options.infoLog})");
            Console.WriteLine($"--warningsAsErrors[=<bool>]  Enable/Disable treating warnings as errors (Default: {options.warningsAsErrors})");
            Console.WriteLine();
            Console.WriteLine($"--target=<string>            Set the compilation target triple (Default: \"{options.target}\")");
            Console.WriteLine();
            Console.WriteLine($"--emitLLVM[=<bool>]          Enable/Disable emitting llvm object/asm (Default: {options.emitLLVM})");
            Console.WriteLine($"--optimisations[=<bool>]     Enable/Disable optimisations (Default: {options.optimisations})");
            Console.WriteLine($"--debugInfo[=<bool>]         Enable/Disable debug information (Default: {options.debugInfo})");
            Console.WriteLine();
        }

        static bool ShowOptionError(ExitCodes exitCode, string error)
        {
            ShowOptions();
            ShowError(exitCode, error);
            return false;
        }

        static bool ParseStringOption(string s, string[] split, out string result)
        {
            result = null;
            if (split.Length > 1)
            {
                result = split[1];
            }
            else
            {
                return ShowOptionError(ExitCodes.InvalidArguments, $"Expected filename ${s}");
            }
            return true;
        }
        static bool ParseBoolOption(string s, string[] split, out bool result)
        {
            result = false;
            if (split.Length > 1 && bool.TryParse(split[1], out var p))
                result = p;
            else
                result = true;
            return true;
        }

        delegate bool Assign(string s, string[] split);

        static readonly Dictionary<string, Assign> _optionsParsers = new Dictionary<string, Assign>
        {
            ["-o"] = (s, split) => ParseStringOption(s, split, out options.outputFileName),
            ["--output"] = (s, split) => ParseStringOption(s, split, out options.outputFileName),
            ["--debugLog"] = (s, split) => ParseBoolOption(s, split, out options.debugLog),
            ["--infoLog"] = (s, split) => ParseBoolOption(s, split, out options.infoLog),
            ["--warningsAsErrors"] = (s, split) => ParseBoolOption(s, split, out options.warningsAsErrors),
            ["--target"] = (s, split) => ParseStringOption(s, split, out options.target),
            ["--emitLLVM"] = (s, split) => ParseBoolOption(s, split, out options.emitLLVM),
            ["--optimisations"] = (s, split) => ParseBoolOption(s, split, out options.optimisations),
            ["--debugInfo"] = (s, split) => ParseBoolOption(s, split, out options.debugInfo),
        };

        static bool ParseOptions(string[] args)
        {
            foreach (var s in args)
            {
                var split = s.Split('=');
                if (split[0].StartsWith('-'))
                {
                    if (_optionsParsers.TryGetValue(split[0], out var parser))
                    {
                        if (!parser(s, split))
                            return false;
                    }
                    else
                    {
                        return ShowOptionError(ExitCodes.InvalidArguments, $"Unknown option : {s}");
                    }
                }
                else
                    options.inputFiles.Add(s);
            }

            if (options.inputFiles.Count==0)
            {
                return ShowOptionError(ExitCodes.InvalidArguments, $"Expected at least one input file");
            }
            return true;
        }

        static void ShowError(ExitCodes exitCode, string error)
        {
            Console.WriteLine(error);
            Console.WriteLine();
            Environment.ExitCode = (int)exitCode;
        }

        static void Main(string[] args)
        {
            InitialiseOptions();

            if(!ParseOptions(args))
            {
                return;
            }

            var messages = new CompilerMessages(options.debugLog, options.infoLog, options.warningsAsErrors);

            var tokeniser = new HumphreyTokeniser(messages);

            var tokens = tokeniser.TokenizeFromFile(options.inputFiles[0]);

            if (!messages.HasErrors)
            {
                var parse = new HumphreyParser(tokens, messages).File();

                if (!messages.HasErrors)
                {
                    var compiler = new HumphreyCompiler(messages);
                    var cu = compiler.Compile(parse, options.inputFiles[0], options.target, !options.optimisations, options.debugInfo);

                    if (!messages.HasErrors)
                    {
                        if (options.outputFileName != null)
                        {
                            if (options.emitLLVM)
                                cu.EmitToBitCodeFile(options.outputFileName);
                            else
                                cu.EmitToFile(options.outputFileName);
                        }
                        else
                        {
                            if (options.emitLLVM)
                                Console.WriteLine(cu.Dump());
                            else
                                cu.DumpDisassembly();
                        }
                    }
                }
            }

            Console.WriteLine(messages.Dump());

            if (messages.HasErrors)
            {
                ShowError(ExitCodes.CompilationFailure, $"Failed to compile.");
            }

        }
    }
}
