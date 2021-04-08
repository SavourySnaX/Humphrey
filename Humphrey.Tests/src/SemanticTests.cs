using Xunit;

using System;
using System.Collections.Generic;

namespace Humphrey.FrontEnd.Tests
{

    public unsafe class SemanticTests
    {
        [Theory]
        [InlineData(@" Main : bit", "Main", SemanticPass.IdentifierKind.Type, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : [8]bit", "Main", SemanticPass.IdentifierKind.Type, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : *bit", "Main", SemanticPass.IdentifierKind.Type, typeof(AstPointerType), typeof(AstPointerType))]
        [InlineData(@" Main : {bob:bit}", "Main", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@" Main : [8]bit {bob:=1}", "Main", SemanticPass.IdentifierKind.EnumType, typeof(AstEnumType), typeof(AstEnumType))]
        public void CheckGlobalType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main := 0", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" bob:()()={} Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue,typeof(AstLoadableIdentifier), typeof(AstFunctionType))]
        [InlineData(@" Main := bob bob:()()={} ", "Main", SemanticPass.IdentifierKind.GlobalValue,typeof(AstLoadableIdentifier), typeof(AstFunctionType))]
        [InlineData(@" bob : [8]bit Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstLoadableIdentifier), typeof(AstArrayType))]
        [InlineData(@" Main := ""bob""", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main := bob bob : [8]bit", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstLoadableIdentifier), typeof(AstArrayType))]
        [InlineData(@" bob : *[8]bit Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstLoadableIdentifier), typeof(AstPointerType))]
        [InlineData(@" Main := bob bob : *[8]bit", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstLoadableIdentifier), typeof(AstPointerType))]
        [InlineData(@" bob: {a:bit} Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstLoadableIdentifier), typeof(AstStructureType))]
        [InlineData(@" Main := bob bob: {a:bit}", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstLoadableIdentifier), typeof(AstStructureType))]
        [InlineData(@" bob: [8]bit {a:=1} Main := bob", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstLoadableIdentifier), typeof(AstEnumType))]
        [InlineData(@" Main := bob bob: [8]bit {a:=1}", "Main", SemanticPass.IdentifierKind.GlobalValue, typeof(AstLoadableIdentifier), typeof(AstEnumType))]
        public void CheckInferredGlobalType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION] MyExtern:(a:bit)()", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=+a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=-a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=!a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=~a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a+1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a-1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a/1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a*1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a^1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a|1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a&1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a&&1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a||1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a==1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a!=1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a<1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a>1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a<=1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a>=1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a<<1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a>>1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a>>>1; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=++a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a++; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=--a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ a=a--; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ Main(a); }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]

        public void CheckParamType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main : (a:bit)()={ b:=+a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=-a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=!a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=~a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a+1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a-1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a/1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a*1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a^1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a|1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a&1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a&&1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a||1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a==1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a!=1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a<1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a>1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a<=1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a>=1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a<<1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a>>1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a>>>1; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=++a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a++; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=--a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ b:=a--; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]

        public void CheckInferredLocal(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }


        [Theory]
        [InlineData(@" Main : (a:bit)()={ Main(a); }", "Main", SemanticPass.IdentifierKind.Function, typeof(AstFunctionType), typeof(AstFunctionType))]
        [InlineData(@" Main : (a:bit)()={ Main(a+a); }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ c:=a; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:bit)()={ c:=Main; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstLoadableIdentifier), typeof(AstFunctionType))]
        [InlineData(@" Main : (a:bit)()={ c:()()={}; }", "c", SemanticPass.IdentifierKind.Function, typeof(AstFunctionType), typeof(AstFunctionType))]
        [InlineData(@" Main : (a:bit)(b:[8]bit)={ b=a as [8]bit; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit)(b:bit)={ b=a[1]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Main : (a:[8]bit)(b:bit)={ b=a[1]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        public void CheckLocalType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; c:=b.a; }", "a", SemanticPass.IdentifierKind.StructMember, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; c:=b.a; }", "Struct", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; c:=b.a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstIdentifier), typeof(AstStructureType))]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; c:=b.a; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Enum : [8]bit { a:=1 } Main : ()()={ b:=Enum.a; }", "a", SemanticPass.IdentifierKind.EnumMember, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Enum : [8]bit { a:=1 } Main : ()()={ b:=Enum.a; }", "Enum", SemanticPass.IdentifierKind.EnumType, typeof(AstEnumType), typeof(AstEnumType))]
        [InlineData(@" Enum : [8]bit { a:=1 } Main : ()()={ b:=Enum.a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        public void CheckMember(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ b:[8]bit=_; for b = a..c { b++; } }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ b:[8]bit=_; for b = a..c { b++; } }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ b:[8]bit=_; for b = a..c { b++; } }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Struct : {bacon:[8]bit} Main : (a:[8]bit,c:Struct)()={ b:[8]bit=_; for b = a..c.bacon { b++; } }", "bacon", SemanticPass.IdentifierKind.StructMember, typeof(AstArrayType), typeof(AstArrayType))]

        public void CheckFor(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ b:[8]bit=0; while b<2 { b++; } }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]

        public void CheckWhile(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ return; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]

        public void CheckReturn(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit)={ b:=Main(a,c); r=b;}", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit)={ b:=Main(a,c); r=b;}", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit)={ b:=Main(a,c); r=b;}", "r", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit)={ b:=Main(a,c); r=b;}", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)(r:[8]bit,s:bit)={ b:=Main(a,c); r=b;}", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@" Test : ()(r:[8]bit, s:bit) = {r=0;s=0;} Main : ()(q:bit)={ b:=Test(); q=b.s;}", "q", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Test : ()(r:[8]bit) = {r=0;} Main : ()(q:bit)={ b:=Test(); q=b.r;}", "q", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Test : ()(r:[8]bit) = {r=0;} Main : ()(q:bit)={ b:=Test(); q=b.r;}", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@" Test : ()(r:[8]bit) = {r=0;} Main : ()(q:bit)={ b:=Test(); q=b;}", "q", SemanticPass.IdentifierKind.FunctionParam, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Test : ()(r:[8]bit) = {r=0;} Main : ()(q:bit)={ b:=Test(); q=b;}", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@" Test : ()(r:[8]bit) = {r=0;} Main : ()()={ b:=Test(); q:=b.r;}", "q", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" [C_CALLING_CONVENTION] Test : ()(r:[8]bit) Main : ()()={ b:=Test(); }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]

        public void CheckCall(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@"State:{p:bit} KeyStates:[4]State Main : () () = { state:=KeyStates[3].p; }", "state", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@"State:{p:bit} KeyStates:[4]State Main : () () = { n:=3; state:=KeyStates[n].p; }", "n", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"State:{p:bit} KeyStates:*State Main : () () = { state:=KeyStates[3].p; }", "state", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        public void CheckSubscript(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"void:{} Handle:void StdHandle:*Handle", "void", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@"void:{} Handle:void StdHandle:*Handle", "Handle", SemanticPass.IdentifierKind.StructType, typeof(AstIdentifier), typeof(AstStructureType))]
        //[InlineData(@"void:{} Handle:void StdHandle:*Handle", "StdHandle", SemanticPass.IdentifierKind.Type, typeof(AstPointerType), typeof(AstPointerType))]
        public void EmptyStruct(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }



        [Theory]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ if a==c { a++; } }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ if a==c { a++; } else { c++; } }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit,c:[8]bit)()={ if a==c { a++; } else { c++; } }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]

        public void CheckIf(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" Main : (a:*[8]bit)(b:[8]bit)={ b=*a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstPointerType), typeof(AstPointerType))]
        [InlineData(@" Main : (a:*[8]bit)(b:[8]bit)={ b=*a; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:*[8]bit)()={ b:=*a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstPointerType), typeof(AstPointerType))]
        [InlineData(@" Main : (a:*[8]bit)()={ b:=*a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]

        public void CheckDeref(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Main : (a:[8]bit)(b:*[8]bit)={ b=&a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit)(b:*[8]bit)={ b=&a; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstPointerType), typeof(AstPointerType))]
        [InlineData(@" Main : (a:[8]bit)()={ b:=&a; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" Main : (a:[8]bit)()={ b:=&a; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstPointerType), typeof(AstPointerType))]

        public void CheckAddressOf(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[..c]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[..c]; }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[..c]; }", "out", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..]; }", "out", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "out", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        public void CheckInclusiveRange(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[..c]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[..c]; }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[..c]; }", "out", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..]; }", "out", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..c]; }", "a", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..c]; }", "b", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..c]; }", "c", SemanticPass.IdentifierKind.FunctionParam, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) () = { out:=a[b..c]; }", "out", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        public void CheckInferInclusiveRange(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"Struct:{a:bit} pStruct:=0 as *Struct", "Struct", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@"Struct:{a:bit} pStruct:=0 as *Struct", "pStruct", SemanticPass.IdentifierKind.GlobalValue,typeof(AstPointerType), typeof(AstPointerType))]
        [InlineData(@"Struct:{a:bit} pStruct:=0 as *Struct Main:()()={local:=*pStruct;}", "pStruct", SemanticPass.IdentifierKind.GlobalValue, typeof(AstPointerType), typeof(AstPointerType))]
        [InlineData(@"Struct:{a:bit} pStruct:=0 as *Struct Main:()()={local:=*pStruct;}", "local", SemanticPass.IdentifierKind.LocalValue, typeof(AstIdentifier), typeof(AstStructureType))]
        [InlineData(@"Struct:{a:bit} Main:()()={local:=0 as Struct;}", "Struct", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@"UInt64:[64]bit Main:(a:*UInt64)(out:UInt64)={out=(a as UInt64);}", "UInt64", SemanticPass.IdentifierKind.Type, typeof(AstArrayType), typeof(AstArrayType))]
        public void CheckAs(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" UInt32:[32]bit Main:(a:UInt32)()={ b:=a + 7; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstIdentifier), typeof(AstArrayType))]
        [InlineData(@" UInt32:[32]bit Struct:{ s:UInt32 } Main:(a:Struct)()={ b:=a.s + 7; }", "b", SemanticPass.IdentifierKind.LocalValue, typeof(AstIdentifier), typeof(AstArrayType))]
        public void CheckExpressionResolve(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" UInt32:[32]bit Enum:UInt32{ bob:=1 }", "UInt32", SemanticPass.IdentifierKind.Type, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" UInt32:[32]bit Enum:UInt32{ bob:=1 }", "Enum", SemanticPass.IdentifierKind.EnumType, typeof(AstEnumType), typeof(AstEnumType))]
        [InlineData(@" UInt32:[32]bit Enum:UInt32{ bob:=1 }", "bob", SemanticPass.IdentifierKind.EnumMember, typeof(AstIdentifier), typeof(AstArrayType))]
        public void CheckEnumType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@" UInt32:[32]bit Struct:{ bob:UInt32 }", "UInt32", SemanticPass.IdentifierKind.Type, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@" UInt32:[32]bit Struct:{ bob:UInt32 }", "Struct", SemanticPass.IdentifierKind.StructType, typeof(AstStructureType), typeof(AstStructureType))]
        [InlineData(@" UInt32:[32]bit Struct:{ bob:UInt32 }", "bob", SemanticPass.IdentifierKind.StructMember, typeof(AstIdentifier), typeof(AstArrayType))]
        public void CheckStructType(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; b.a=0; }", "a", SemanticPass.IdentifierKind.StructMember, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Struct : { a:bit } Main : (in:*Struct)()={ in.a=0; }", "a", SemanticPass.IdentifierKind.StructMember, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Struct : { a:bit } Main : ()()={ b:Struct=0; b.a=b.a; }", "a", SemanticPass.IdentifierKind.StructMember, typeof(AstBitType), typeof(AstBitType))]
        public void CheckBinReference(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        
        [Theory]
        [InlineData(@"Main : (a:[8]bit, b:bit)()={ c:=a+b; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:bit)()={ c:=b+a; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:bit)()={ c:=a+a; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstArrayType), typeof(AstArrayType))]
        [InlineData(@"Main : (a:[8]bit, b:bit)()={ c:=b+b; }", "c", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        public void CheckPromotion(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@" Struct:{val:bit} Array:[2]Struct=0 Main : ()()={ local:=Array[0]; local2:=local.val; }", "local", SemanticPass.IdentifierKind.LocalValue, typeof(AstIdentifier), typeof(AstStructureType))]
        [InlineData(@" Struct:{val:bit} Array:[2]Struct=0 Main : ()()={ local:=Array[0]; local2:=local.val; }", "local2", SemanticPass.IdentifierKind.LocalValue, typeof(AstBitType), typeof(AstBitType))]
        [InlineData(@" Struct:{val:bit} Array:[2]Struct=0 PtrArray:*Array Main : ()()={ local:=PtrArray[0][0]; local2:=local.val; }", "local", SemanticPass.IdentifierKind.LocalValue, typeof(AstIdentifier), typeof(AstStructureType))]
        public void CheckStructArray(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }
        [Theory]
        [InlineData(@" void:{} Handle:void StdHandle:*Handle outputHandle:StdHandle=_ Main : ()()={ handle:=outputHandle; }", "handle", SemanticPass.IdentifierKind.LocalValue, typeof(AstIdentifier), typeof(AstPointerType))]
        public void CheckDeepResolve(string input, string symbol, SemanticPass.IdentifierKind expected, Type t, Type b)
        {
            var result = Build(input, symbol, expected, t, b);
            Assert.True(result);
        }

        [Theory]
        [InlineData(@"Main : (a : [32]bit) () = { ptr:=&a; }", "ptr", typeof(AstPointerType), typeof(AstArrayType))]
        [InlineData(@"Main : () (returnValue : [8]bit) = { bob:=""Hello World""; sue:=bob; } ", "sue", typeof(AstArrayType), typeof(AstArrayType))]
        public void CheckIdentifierTypeNotValue(string input, string symbol, Type baseType, Type elementType)
        {
            var result = Build(input);
            foreach (var t in result.tokens)
            {
                if (t.ToStringValue() == symbol)
                {
                    Assert.True(result.semanticPass.FetchSemanticInfo(t, out var info));
                    Assert.IsType(baseType, info.Base);

                    switch (info.Base)
                    {
                        case AstPointerType pT:
                            Assert.IsType(elementType, pT.ElementType);
                            break;
                        case AstArrayType aT:
                            Assert.IsType(elementType, aT.ElementType);
                            break;
                    }
                }
            }
        }

        public (SemanticPass semanticPass, IEnumerable<Result<Tokens>> tokens) Build(string input)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var semantic = new SemanticPass("test", messages);
            semantic.RunPass(parsed);
            return (semantic,tokens);
        }

        public bool Build(string input, string symbol, SemanticPass.IdentifierKind expectedKind, Type astKind, Type baseType)
        {
            var (semantic,tokens) = Build(input);
            if (baseType==null)
            {
                baseType = astKind;
            }
            // find token in tokens
            bool matches = true;
            foreach (var t in tokens)
            {
                if (t.ToStringValue() == symbol)
                {
                    if (!semantic.FetchSemanticInfo(t,out var info))
                        throw new System.NotImplementedException($"Missing SemanticInfo set on token {t.ToStringValue()} {t.Location.ToString()}");

                    matches &= info.Ast.GetType().Equals(astKind);
                    matches &= info.Base.GetType().Equals(baseType);
                    matches &= info.Kind.Equals(expectedKind);

                    if (!matches)
                    {
                        matches = false;
                    }
                }
            }

            return matches;
        }
    }
}