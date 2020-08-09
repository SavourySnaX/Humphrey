using System.Collections.Generic;
using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationEnumType : CompilationType
    {
        CompilationType elementType;
        CompilationConstantValue[] values;
        Dictionary<string, uint> names;
        public CompilationEnumType(CompilationType type, CompilationConstantValue[] elements, Dictionary<string,uint> elementNames) : base(type.BackendType)
        {
            elementType = type;
            values = elements;
            names = elementNames;
        }
        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationEnumType;
            if (check == null)
                return false;

            if (!elementType.Same(check.elementType))
                return false;

            if (values.Length!=check.values.Length)
                return false;
            if (names.Count!=check.names.Count)
                return false;

            for (int a = 0; a < values.Length;a++)
            {
                if (!values[a].Same(check.values[a]))
                    return false;
            }

            foreach (var kp in names)
            {
                if (!check.names.ContainsKey(kp.Key))
                    return false;
                if (check.names[kp.Key] != kp.Value)
                    return false;
            }

            return Identifier == check.Identifier;
        }

        public override CompilationType CopyAs(string identifier)
        {
            var clone = new CompilationEnumType(elementType, values, names);
            clone.identifier = identifier;
            return clone;
        }

        public CompilationValue LoadElement(CompilationUnit unit, CompilationBuilder builder, string identifier)
        {
            if (names.TryGetValue(identifier, out var idx))
            {
                return values[idx].GetCompilationValue(unit, elementType);
            }

            throw new System.NotImplementedException($"Error - enum '' does not contain {identifier}");
        }

        public CompilationType ElementType => elementType;
    }
}
