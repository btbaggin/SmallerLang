using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Emitting
{
    public readonly struct MethodDefinition
    {
        public string MangledName { get; }
        public string Name { get; }
        public bool External { get; }
        public List<SmallType> ArgumentTypes { get; }
        public SmallType ReturnType { get; }
        public FileScope Scope { get; }

        public MethodDefinition(FileScope pScope, string pName, string pMangled, bool pExternal, List<SmallType> pArguments, SmallType pReturn)
        {
            Scope = pScope;
            Name = pName;
            MangledName = pMangled;
            External = pExternal;
            ArgumentTypes = pArguments;
            ReturnType = pReturn;
        }

        public MethodDefinition(string pName, List<SmallType> pArguments)
        {
            Scope = FileScope.Public;
            Name = pName;
            MangledName = pName;
            ArgumentTypes = pArguments;
            ReturnType = SmallTypeCache.Undefined;
            External = false;
        }

        public MethodDefinition MakeConcreteDefinition(SmallType pType)
        {
            //Not a generic type... don't need to worry about it
            if (pType == null || !pType.HasGenericArguments) return this;

            //Transform the generic parameters and return types to match the concrete type
            List<SmallType> arguments = new List<SmallType>(ArgumentTypes.Count);

            Dictionary<string, int> typeIndexes = new Dictionary<string, int>(pType.GenericParameters.Count);
            for (int i = 0; i < pType.GenericParameters.Count; i++)
            {
                typeIndexes.Add(pType.GenericParameters[i], i);
            }

            //Transform the arguments
            foreach (var a in ArgumentTypes)
            {
                if (a.IsGenericParameter && typeIndexes.ContainsKey(a.Name))
                {
                    var i = typeIndexes[a.Name];
                    arguments.Add(pType.GenericArguments[i]);
                }
                else if (!a.IsGenericParameter)
                {
                    arguments.Add(a);
                }
            }

            //Transform return types
            List<SmallType> returnTypes = new List<SmallType>();
            if (ReturnType.IsTuple)
            {
                //Each field if it's a tuple return
                foreach (var f in ReturnType.GetFields())
                {
                    if (f.Type.IsGenericParameter && typeIndexes.ContainsKey(f.Type.Name))
                    {
                        var i = typeIndexes[f.Type.Name];
                        returnTypes.Add(pType.GenericArguments[i]);
                    }
                    else if (!f.Type.IsGenericParameter)
                    {
                        returnTypes.Add(f.Type);
                    }
                }
            }
            else if (ReturnType.IsGenericParameter && typeIndexes.ContainsKey(ReturnType.Name))
            {
                //Or just the type if it's a generic type
                var i = typeIndexes[ReturnType.Name];
                returnTypes.Add(pType.GenericArguments[i]);
            }
            else if (!ReturnType.IsGenericParameter) returnTypes.Add(ReturnType);

            //Create a new return type
            SmallType ret = SmallTypeCache.GetOrCreateTuple(returnTypes.ToArray());

            return new MethodDefinition(Scope, Name, MangledName, External, arguments, ret);
        }

        public override string ToString()
        {
            var name = new StringBuilder();
            name.Append(Name);
            name.Append("(");

            foreach (var p in ArgumentTypes)
            {
                name.Append(p.ToString());
                name.Append(",");
            }
            if (ArgumentTypes.Count > 0) name = name.Remove(name.Length - 1, 1);

            name.Append(")");

            if (ReturnType != SmallTypeCache.Undefined)
            {
                name.Append(" -> ");
                name.Append(ReturnType.ToString());
            }

            return name.ToString();
        }
    }
}
