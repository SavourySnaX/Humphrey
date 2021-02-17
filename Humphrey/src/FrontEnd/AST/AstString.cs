using System;
using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstString : IExpression
    {
        string temp;
        public AstString(string stringLiteral)
        {
            temp = stringLiteral.Substring(1,stringLiteral.Length-2);   // Skip enclosing quotes
        }
    
        public string Dump()
        {
            return $"\"{temp}\"";
        }

        public byte[] GetNullTerminatedArray()
        {
            var bytes = Encoding.UTF8.GetBytes(temp);
            var nullTerminated = new byte[bytes.Length+1];
            Array.Copy(bytes,nullTerminated, bytes.Length);
            return nullTerminated;
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var bitType = new AstBitType();
            var byteType = new AstArrayType(new AstNumber("8"), bitType);
            var bytes = GetNullTerminatedArray();
            var arrayType = new AstArrayType(new AstNumber($"{bytes.Length}"), byteType);
            arrayType.Token = Token;

            var initialiser = new CompilationConstantIntegerKind[bytes.Length];
            var idx = 0;
            foreach( var b in bytes)
            {
                initialiser[idx++] = new CompilationConstantIntegerKind(new AstNumber($"{b}"));
            }

            return new CompilationConstantArrayKind(byteType, initialiser, Token);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var bitType = new AstBitType();
            var byteType = new AstArrayType(new AstNumber("8"), bitType);
            var bytes = GetNullTerminatedArray();
            var arrayType = new AstArrayType(new AstNumber($"{bytes.Length}"), byteType);
            arrayType.Token = Token;
            return ProcessConstantExpression(unit).GetCompilationValue(unit, arrayType.CreateOrFetchType(unit).compilationType);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }
    }
}