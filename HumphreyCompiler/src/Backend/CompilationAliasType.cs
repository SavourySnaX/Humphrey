using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationAliasType : CompilationType
    {
        CompilationType baseType;
        CompilationType[][] elementTypes;
        uint[][] rotAmount;

        string[][] elementNames;

        public CompilationAliasType(LLVMTypeRef type, CompilationType bType, CompilationType[][] types, string[][] names, uint[][] rotate, CompilationDebugBuilder debugBuilder, SourceLocation location, string ident = "") : base(type, debugBuilder, location, ident)
        {
            baseType = bType;
            elementTypes = types;
            elementNames = names;
            rotAmount = rotate;
            CreateDebugType();
        }

        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationAliasType;
            if (check == null)
                return false;

            if (!check.baseType.Same(baseType))
                return false;

            if (check.elementTypes.Length != elementTypes.Length)
                return false;
            
            for (int a=0;a<elementTypes.Length;a++)
            {
                if (check.elementTypes[a].Length != elementTypes[a].Length)
                    return false;
                
                for (int b=0;b<elementTypes[a].Length;b++)
                {
                    if (!check.elementTypes[a][b].Same(elementTypes[a][b]))
                        return false;
                    if (check.elementNames[a][b]!=elementNames[a][b])
                        return false;
                }
            }
            var anonMatch = Identifier == "" || check.Identifier == "" || Identifier == check.Identifier;
			return anonMatch;
		}

        public override CompilationType CopyAs(string identifier)
        {
            return new CompilationAliasType(BackendType, baseType, elementTypes, elementNames, rotAmount, DebugBuilder, Location, identifier);
        }

        void CreateDebugType()
        {
            if (DebugBuilder.Enabled)
            {
                var name = DumpType();
                var debugType = new CompilationDebugType(name, baseType.DebugType.BackendType);
                SetDebugType(debugType);
            }
        }

        public CompilationValue LoadElement(CompilationUnit unit, CompilationBuilder builder, CompilationValue src, string identifier)
        {
            uint idxA=0;
            uint idxB=0;
            foreach (var names in elementNames)
            {
                foreach (var name in names)
                {
                    if (name == identifier)
                    {
                        // Compute Shift required, then truncate
                        var rotateBy = unit.CreateConstant($"{rotAmount[idxA][idxB]}", Location);
                        var rotateByMatched = builder.MatchWidth(rotateBy, baseType);
                        var correctedSrc = new CompilationValue(src.BackendValue, baseType, src.FrontendLocation);
                        var shifted = builder.RotateRight(correctedSrc, rotateByMatched);
                        var truncated = builder.MatchWidth(shifted,elementTypes[idxA][idxB]);
                        return truncated;
                    }
                    idxB++;
                }

                idxA++;
                idxB=0;
            }

            throw new System.NotImplementedException($"Error Should Already be handled in semantic pass");
        }

        public void StoreElement(CompilationUnit unit, CompilationBuilder builder, CompilationValue dst, IExpression src, string identifier)
        {
            uint idxA=0;
            uint idxB=0;
            foreach (var names in elementNames)
            {
                foreach (var name in names)
                {
                    if (name == identifier)
                    {
                        var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, src, elementTypes[idxA][idxB]);

                        // we need to slot the value back into the original type
                        // [AB??EFGH]   [ZX]
                        // [EFGHAB??] (rotate original value dst by rotate amount)
                        // [000000ZX] (expand incoming value to fit)
                        // [FFFFFF00] (make inverse mask from element size)
                        // [EFGHAB00]  And Mask with rotated original
                        // [EFGHABZX]  Or expanded and masked original
                        // [ABZXEFGH] rotate commbined value back
                        // store value to destination
                        var rotateBy = unit.CreateConstant($"{rotAmount[idxA][idxB]}", Location);
                        var rotateByMatched = builder.MatchWidth(rotateBy, baseType);
                        var correctedDst = new CompilationValue(dst.BackendValue, baseType, dst.FrontendLocation);
                        var shifted = builder.RotateRight(correctedDst, rotateByMatched);
                        var expanded = builder.MatchWidth(storeValue, baseType);
                        var mask = unit.CreateConstant($"{(1<<(int)(elementTypes[idxA][idxB] as CompilationIntegerType).IntegerWidth)-1}", Location);
                        var maskMatched = builder.MatchWidth(mask, baseType);
                        var maskInv = builder.Not(maskMatched);
                        var anded = builder.And(maskInv, shifted);
                        var ored = builder.Or(anded, expanded);
                        var combinedValue = builder.RotateLeft(ored, rotateByMatched);
                        builder.Store(combinedValue, dst.Storage);
                        return;
                    }
                    idxB++;
                }

                idxA++;
                idxB=0;
            }

            throw new System.NotImplementedException($"Error Should Already be handled in semantic pass");





            throw new System.NotImplementedException($"TODO");
            /*
            // Find identifier in elements
            uint idx=0;
            foreach (var i in elementNames)
            {
                if (i == identifier)
                    break;
                idx++;
            }
            if (idx==elementTypes.Length)
            {
                // Compilation error, struct xxx does not contain field yyy
                throw new System.Exception($"Need error message and partial recovery -struct does not contain field {identifier}");
            }

            CompilationType elementType = elementTypes[idx];
            var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, src, elementType);

            var newVal = builder.InsertValue(dst, storeValue, idx);

            builder.Store(newVal, dst.Storage);
            */
        }

        public override string DumpType()
        {
            var name = Identifier;
            if (string.IsNullOrEmpty(name))
            {
                name = $"__anonymous_alias_{baseType.DumpType()}_";
                foreach (var elements in elementTypes)
                {
                    foreach (var element in elements)
                    {
                        name += $"{element.DumpType()}_";
                    }
                }
            }
            return name;
        }
    }
}

