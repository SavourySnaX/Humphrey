using System;
using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstString : IExpression
    {
        public enum StringKind
        {
            UTF8 = 8,
            UTF16 = 16,
            UTF32 = 32
        }
        string temp;
        StringKind stringKind;
        public AstString(string stringLiteral)
        {
            HumphreyTokeniser.ConvertString(stringLiteral, out temp, out stringKind);
        }
    
        public string Dump()
        {
            return $"\"{temp}\"";
        }

        private AstArrayType GetElementType()
        {
            var bitType = new AstBitType();
            return new AstArrayType(new AstNumber($"{(int)stringKind}"), bitType);
        }

        private AstArrayType GetArrayType()
        {
            return new AstArrayType(new AstNumber($"{FetchArray().Length}"), GetElementType());
        }

        private CompilationConstantIntegerKind[] GetInitialiser<T>(T[] array)
        {
            var initialiser = new CompilationConstantIntegerKind[array.Length];
            var idx = 0;
            foreach( var b in array)
            {
                initialiser[idx++] = new CompilationConstantIntegerKind(new AstNumber($"{b}"));
            }
            return initialiser;
        }

        private CompilationConstantIntegerKind[] GetCharactersEncodedForStringUTF8()
        {
            var bytes = Encoding.UTF8.GetBytes(temp);
            var array = new byte[bytes.Length + 1];
            Array.Copy(bytes, array, bytes.Length);
            return GetInitialiser(array);
        }
        private CompilationConstantIntegerKind[] GetCharactersEncodedForStringUTF16()
        {
            Encoding unicode = new UnicodeEncoding(!BitConverter.IsLittleEndian, false);
            var bytes = unicode.GetBytes(temp);
            if ((bytes.Length&1)==1)
                throw new Exception($"Should be pairs");
            var array = new ushort[bytes.Length/2 + 1];
            for (int a = 0; a < bytes.Length / 2; a++)
            {
                ushort result = (ushort)((bytes[a * 2 + 1] << 8) | (bytes[a * 2 + 0]));
                array[a] = result;
            }
            return GetInitialiser(array);
        }
        private CompilationConstantIntegerKind[] GetCharactersEncodedForStringUTF32()
        {
            Encoding unicode = new UTF32Encoding(!BitConverter.IsLittleEndian, false);
            var bytes = unicode.GetBytes(temp);
            if ((bytes.Length&3)!=0)
                throw new Exception($"Should be quads");
            var array = new uint[bytes.Length/4 + 1];
            for (int a = 0; a < bytes.Length / 4; a++)
            {
                ushort resultLo = (ushort)((bytes[a * 4 + 1] << 8) | (bytes[a * 4 + 0]));
                ushort resultHi = (ushort)((bytes[a * 4 + 3] << 8) | (bytes[a * 4 + 2]));
                uint result = (uint)((resultHi << 16) | resultLo);
                array[a] = result;
            }
            return GetInitialiser(array);
        }

        private CompilationConstantIntegerKind[] FetchArray()
        {
            CompilationConstantIntegerKind[] array = null;
            switch (stringKind)
            {
                case StringKind.UTF8:
                    array = GetCharactersEncodedForStringUTF8();
                    break;
                case StringKind.UTF16:
                    array = GetCharactersEncodedForStringUTF16();
                    break;
                case StringKind.UTF32:
                    array = GetCharactersEncodedForStringUTF32();
                    break;
            }
            return array;
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            return new CompilationConstantArrayKind(GetElementType(), FetchArray(), Token);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var arrayType = GetArrayType();
            arrayType.Token = Token;
            return ProcessConstantExpression(unit).GetCompilationValue(unit, arrayType.CreateOrFetchType(unit).compilationType);
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            return GetArrayType();
        }

        public void Semantic(SemanticPass pass)
        {
            // nothing to do`
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }
    }
}