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

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new NotImplementedException();
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            // Otherwise, for now a string Literal is an array of bytes
            var bitType = new AstBitType();
            var byteType = new AstArrayType(new AstNumber("8"), bitType);
            var bytes = GetNullTerminatedArray();
            var arrayType = new AstArrayType(new AstNumber($"{bytes.Length}"), byteType);
            arrayType.Token = Token;
            return new CompilationValue(unit.CreateStringConstant(this), arrayType.CreateOrFetchType(unit).compilationType, Token);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }
    }
}