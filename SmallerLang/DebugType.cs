using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace SmallerLang
{
    internal struct DebugType
    {
        public ulong Size { get; private set; }
        public LLVMMetadataRef Value { get; private set; }

        public DebugType(ulong pSize, LLVMMetadataRef pValue)
        {
            Size = pSize;
            Value = pValue;
        }

        static Dictionary<SmallType, DebugType> _debugMappings = new Dictionary<SmallType, DebugType>();
        internal static DebugType GetLLVMDebugType(LLVMDIBuilderRef pBuilder, LLVMMetadataRef pScope, LLVMMetadataRef pFile, SmallType pType)
        {
            if (!_debugMappings.ContainsKey(pType))
            {
                if (pType.IsArray)
                {
                    var elementType = GetLLVMDebugType(pBuilder, pScope, pFile, pType.GetElementType());
                    var value = LLVM.DIBuilderCreateArrayType(pBuilder, elementType.Size, 0, elementType.Value, default);

                    var t = new DebugType(elementType.Size, value);
                    _debugMappings.Add(pType, t);
                    return t;
                }
                else if (pType.IsTuple || pType.IsStruct || pType.IsTrait)
                {
                    ulong size = 0;
                    ulong offset = 0;
                    LLVMMetadataRef type = default;
                    foreach(var f in pType.GetFields())
                    {
                        var memberType = GetLLVMDebugType(pBuilder, pScope, pFile, f.Type);
                        type = LLVM.DIBuilderCreateMemberType(pBuilder, pScope, f.Name, pFile, 0, memberType.Size, 0, offset, 0, memberType.Value);
                        offset += memberType.Size;
                    }
                    var value = LLVM.DIBuilderCreateStructType(pBuilder, pScope, pType.Name, pFile, 0, 0, 0, 0, default, type);

                    var t = new DebugType(size, value);
                    _debugMappings.Add(pType, t);
                    return t;
                }
                else
                {
                    ulong size;
                    if (pType == SmallTypeCache.Boolean) size = 1;
                    else if (pType == SmallTypeCache.Short) size = 16;
                    else if (pType == SmallTypeCache.Int) size = 32;
                    else if (pType == SmallTypeCache.Long) size = 64;
                    else if (pType == SmallTypeCache.Float) size = 32;
                    else if (pType == SmallTypeCache.Double) size = 64;
                    else if (pType == SmallTypeCache.String) throw new NotImplementedException();
                    else if (pType.IsEnum) size = 32;
                    else throw new ArgumentException("pType");

                    LLVMMetadataRef value = LLVM.DIBuilderCreateBasicType(pBuilder, pType.Name, size, 0, 0);
                    var t = new DebugType(size, value);
                    _debugMappings.Add(pType, t);
                    return t;
                }
            }
            return _debugMappings[pType];
        }
    }
}
