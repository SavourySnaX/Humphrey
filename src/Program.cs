using System;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;
using sly.lexer;
using sly.parser.generator;
using static Extensions.Helpers;

namespace Humphrey.Experiments
{
    unsafe class Program
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Add(int a, int b);

        // Append new line, or else csly crashes parsing the comments!
        public static readonly string testProgram = "bit Buffer (bit a) { return a; } #single line comment" + Environment.NewLine;

        public enum Tokens
        {
            [Lexeme(GenericToken.Identifier, IdentifierType.Custom, "_a-zA-Z","_a-zA-Z0-9")]
            Identifier,
            [Lexeme(GenericToken.KeyWord, "bit")]
            Bit,
            [Lexeme(GenericToken.KeyWord, "return")]
            Return,
            [Lexeme(GenericToken.SugarToken, "+")]
            Plus,
            [Lexeme(GenericToken.SugarToken, ";")]
            SemiColon,
            [Lexeme(GenericToken.SugarToken, "{")]
            OpenCurlyBrace,
            [Lexeme(GenericToken.SugarToken, "}")]
            CloseCurlyBrace,
            [Lexeme(GenericToken.SugarToken, "(")]
            OpenParanthesis,
            [Lexeme(GenericToken.SugarToken, ")")]
            CloseParanthesis,
            [Lexeme(GenericToken.SugarToken, ",")]
            Comma,
            [SingleLineComment("#")]
            SingleComment
        }

        public class Parser
        {
            [Production("return_list : Bit")]
            public string ReturnList(Token<Tokens> bit)
            {
                return "ReturnList";
            }

            [Production("param_list : Bit Identifier")]
            public string ParamList(Token<Tokens> bit, Token<Tokens> identifier)
            {
                return "ParamList";
            }
            
            [Production("block : OpenCurlyBrace [d] statement CloseCurlyBrace [d]")]
            public string Block(string statement)
            {
                return "Block";
            }

            [Production("statement : Return [d] Identifier SemiColon [d]")]
            public string Statement(Token<Tokens> statement)
            {
                return "Statement";
            }

            [Production("function : return_list Identifier OpenParanthesis [d] param_list CloseParanthesis [d] block")]
            public string FunctionDecleration(string return_list, Token<Tokens> identifier, string param_list, string block)
            {
                return $"{return_list} {identifier.Value} {param_list} {block}";
            }
        }

        static void LangTest()
        {
            var parser = new Parser();
            var builder = new ParserBuilder<Tokens, string>();
            var built = builder.BuildParser(parser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "function");

            if (built.IsError)
                foreach(var error in built.Errors)
                {
                    Console.WriteLine($"Whoops : {error.Message}");
                }

            var result = built.Result.Parse(testProgram);
            if (result.IsError)
                foreach(var error in result.Errors)
                {
                    Console.WriteLine($"SyntaxError : {error.ErrorMessage}");
                }

            Console.WriteLine($"Success : {result.Result}");
        }

        static void Main(string[] args)
        {
            LangTest();

            var context = CreateContext();
            var module = context.CreateModuleWithName("Hello");

            var paramTypes = new[] { context.Int32Type, context.Int32Type };
            var functionType = CreateFunctionType(context.Int32Type, paramTypes, false);
            var functionSum = module.AddFunction("sum", functionType);

            var bb = functionSum.AppendBasicBlock("entry");

            var builder = context.CreateBuilder();
            builder.PositionAtEnd(bb);
            var addParams = builder.BuildAdd(functionSum.GetParam(0), functionSum.GetParam(1), "AddParameters");
            builder.BuildRet(addParams);

            if (!module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var message))
            {
                LLVM.DumpModule(module);
                Console.WriteLine($"Module Verification Failed : {message}");
            }

            LLVM.LinkInMCJIT();

            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();

            var options = LLVMMCJITCompilerOptions.Create();
            if (!module.TryCreateMCJITCompiler(out var ee,ref options, out message))
            {
                Console.WriteLine($"Failed to create MCJit : {message}");
            }

            var addMethod = Marshal.GetDelegateForFunctionPointer<Add>(ee.GetPointerToGlobal(functionSum));
            Console.WriteLine(addMethod(5, 2));

            var targetMachine = LLVMTargetRef.First.CreateTargetMachine(LLVMTargetRef.DefaultTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

            module.DataLayout = targetMachine.CreateTargetDataLayout();
            module.Target = LLVMTargetRef.DefaultTriple;

            var pm = LLVMPassManagerRef.Create();
            LLVMPassManagerBuilderRef passes = LLVM.PassManagerBuilderCreate();
            passes.PopulateModulePassManager(pm);
            passes.PopulateFunctionPassManager(pm);

            module.Dump();
            pm.Run(module);
            module.Dump();

            targetMachine.EmitToFile(module, "compiled.o", LLVMCodeGenFileType.LLVMObjectFile);
        }
    }
}
