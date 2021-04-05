using Xunit;

using System;

namespace Humphrey.FrontEnd.Tests
{

    public unsafe class SemanticTests
    {
        [Theory]
        [InlineData(@" Main : bit", "Main", SemanticPass.IdentifierKind.Type, typeof(AstBitType))]
        [InlineData(@" Main : [8]bit", "Main", SemanticPass.IdentifierKind.Type, typeof(AstArrayType))]
        [InlineData(@" Main : *bit", "Main", SemanticPass.IdentifierKind.Type, typeof(AstPointerType))]
        [InlineData(@" Main : {bob:bit}", "Main", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType))]
        [InlineData(@" Main : [8]bit {bob:=1}", "Main", SemanticPass.IdentifierKind.EnumType, typeof(AstEnumType))]
        public void CheckGlobalType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main := 0", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstBitType))]
        [InlineData(@" bob:()()={} Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstFunctionType))]
        [InlineData(@" Main := bob bob:()()={} ", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstFunctionType))]
        [InlineData(@" bob : [8]bit Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstArrayType))]
        [InlineData(@" Main := ""bob""", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstArrayType))]
        [InlineData(@" Main := bob bob : [8]bit", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstArrayType))]
        [InlineData(@" bob : *[8]bit Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstPointerType))]
        [InlineData(@" Main := bob bob : *[8]bit", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstPointerType))]
        [InlineData(@" bob: {a:bit} Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstStructureType))]
        [InlineData(@" Main := bob bob: {a:bit}", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstStructureType))]
        [InlineData(@" bob: [8]bit {a:=1} Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstEnumType))]
        [InlineData(@" Main := bob bob: [8]bit {a:=1}", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstEnumType))]
        public void CheckInferredGlobalType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION] MyExtern:(a:bit)()", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=+a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=-a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=!a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=~a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a+1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a-1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a/1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a*1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a^1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a|1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a&1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a&&1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a||1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a==1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a!=1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a<1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a>1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a<=1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a>=1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a<<1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a>>1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a>>>1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=++a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a++; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=--a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a--; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ Main(a); }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]

        public void CheckParamType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main : (a:bit)()={ b:=+a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=-a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=!a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=~a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a+1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a-1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a/1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a*1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a^1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a|1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a&1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a&&1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a||1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a==1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a!=1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a<1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a>1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a<=1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a>=1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a<<1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a>>1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a>>>1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=++a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a++; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=--a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a--; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]

        public void CheckInferredLocal(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }


        [Theory]
        [InlineData(@" Main : (a:bit)()={ Main(a); }", "Main", SemanticPass.IdentifierKind.Function, typeof(AstFunctionType))]
        [InlineData(@" Main : (a:bit)()={ Main(a+a); }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ c:=a; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ c:=Main; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstFunctionType))]
        [InlineData(@" Main : (a:bit)()={ c:()()={}; }", "c", SemanticPass.IdentifierKind.Function, typeof(AstFunctionType))]
        [InlineData(@" Main : (a:bit)(b:[8]bit)={ b=a as [8]bit; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit)(b:bit)={ b=a[1]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]
        [InlineData(@" Main : (a:[8]bit)(b:bit)={ b=a[1]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        public void CheckLocalType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; c:=b.a; }", "a", SemanticPass.IdentifierKind.StructMember, typeof(AstBitType))]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; c:=b.a; }", "Struct", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType))]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; c:=b.a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstStructureType))]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; c:=b.a; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType))]
        [InlineData(@" Enum : [8]bit { a:=1 } Main : ()()={ b:=Enum.a; }", "a", SemanticPass.IdentifierKind.EnumMember, typeof(AstArrayType))]
        [InlineData(@" Enum : [8]bit { a:=1 } Main : ()()={ b:=Enum.a; }", "Enum", SemanticPass.IdentifierKind.EnumType, typeof(AstEnumType))]
        [InlineData(@" Enum : [8]bit { a:=1 } Main : ()()={ b:=Enum.a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]
        public void CheckMember(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ b:[8]bit=_; for b = a..c { b++; } }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ b:[8]bit=_; for b = a..c { b++; } }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ b:[8]bit=_; for b = a..c { b++; } }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]

        public void CheckFor(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ b:[8]bit=0; while b<2 { b++; } }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]

        public void CheckWhile(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ return; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]

        public void CheckReturn(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit)={ b:=Main(a,c); r=b;}", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit)={ b:=Main(a,c); r=b;}", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit)={ b:=Main(a,c); r=b;}", "r", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit)={ b:=Main(a,c); r=b;}", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit,s:bit)={ b:=Main(a,c); r=b;}", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstStructureType))]
        [InlineData(@" Test : ()(r:[8]bit, s:bit) = {r=0;s=0;} Main : ()(q:bit)={ b:=Test(a,c); q=b.s;}", "q", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType))]

        public void CheckCall(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }
        

        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ if a==c { a++; } }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ if a==c { a++; } else { c++; } }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ if a==c { a++; } else { c++; } }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]

        public void CheckIf(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" Main : (a:*[8]bit)(b:[8]bit)={ b=*a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstPointerType))]
        [InlineData(@" Main : (a:*[8]bit)(b:[8]bit)={ b=*a; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:*[8]bit)()={ b:=*a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstPointerType))]
        [InlineData(@" Main : (a:*[8]bit)()={ b:=*a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]

        public void CheckDeref(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main : (a:[8]bit)(b:*[8]bit)={ b=&a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit)(b:*[8]bit)={ b=&a; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstPointerType))]
        [InlineData(@" Main : (a:[8]bit)()={ b:=&a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit)()={ b:=&a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstPointerType))]

        public void CheckAddressOf(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[..c]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[..c]; }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[..c]; }", "out", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..]; }", "out", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "out", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        public void CheckInclusiveRange(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[..c]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[..c]; }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[..c]; }", "out", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..]; }", "out", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..c]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..c]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..c]; }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..c]; }", "out", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]
        public void CheckInferInclusiveRange(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"Struct:{a:bit} pStruct:=0 as *Struct", "Struct", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType))]
        [InlineData(@"Struct:{a:bit} pStruct:=0 as *Struct", "pStruct", SemanticPass.IdentifierKind.GlobalValue, typeof(AstPointerType))]
        [InlineData(@"Struct:{a:bit} pStruct:=0 as *Struct Main:()()={local:=*pStruct;}", "pStruct", SemanticPass.IdentifierKind.GlobalValue, typeof(AstPointerType))]
        public void CheckAs(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" UInt32:[32]bit Main:(a:UInt32)()={ b:=a + 7; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]
        [InlineData(@" UInt32:[32]bit Struct:{ s:UInt32 } Main:(a:Struct)()={ b:=a.s + 7; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType))]
        public void CheckExpressionResolve(string input, string symbol, SemanticPass.IdentifierKind expected, Type t)
        {
            var result = Build(input, symbol, expected, t);
            Assert.True(result);
        }

        public bool Build(string input, string symbol, SemanticPass.IdentifierKind expectedKind, Type astKind)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var semantic = new SemanticPass("test", messages);
            semantic.RunPass(parsed);

            // find token in tokens
            bool matches = true;
            foreach (var t in tokens)
            {
                if (t.ToStringValue() == symbol)
                {
                    if (!semantic.FetchSemanticInfo(t,out var info))
                        throw new System.NotImplementedException($"Missing SemanticInfo set on token {t.ToStringValue()} {t.Location.ToString()}");

                    matches &= info.Ast.GetType().Equals(astKind);
                    matches &= info.Kind.Equals(expectedKind);
                }
            }

            return matches;
        }
    }
}