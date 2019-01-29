using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace SmallerLang
{
    internal readonly struct DebugType
    {
        public ulong Size { get; }
        public LLVMMetadataRef Value { get; }

        private static ulong _pointerSize = (ulong)IntPtr.Size;
        private static Dictionary<SmallType, DebugType> _debugMappings = new Dictionary<SmallType, DebugType>();

        private DebugType(ulong pSize, LLVMMetadataRef pValue)
        {
            Size = pSize;
            Value = pValue;
        }

        internal static DebugType GetLLVMDebugType(LLVMDIBuilderRef pBuilder, LLVMMetadataRef pScope, LLVMMetadataRef pFile, SmallType pType)
        {
            DebugType t = default;
            if (!_debugMappings.ContainsKey(pType))
            {
                if (pType.IsArray)
                {
                    var elementType = GetLLVMDebugType(pBuilder, pScope, pFile, pType.GetElementType());
                    var value = LLVM.DIBuilderCreateArrayType(pBuilder, elementType.Size, 0, elementType.Value, default);

                    t = new DebugType(elementType.Size + _pointerSize, value);
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

                    t = new DebugType(size, value);
                }
                else if (pType == SmallTypeCache.String)
                {
                    var charValue = GetLLVMDebugType(pBuilder, pScope, pFile, SmallTypeCache.Char);
                    LLVMMetadataRef stringValue = LLVM.DIBuilderCreateArrayType(pBuilder, charValue.Size, 0, charValue.Value, default);
                    t = new DebugType(_pointerSize + 32, stringValue);
                }
                else
                {
                    ulong size;
                    if (pType == SmallTypeCache.Boolean) size = 1;
                    else if (pType == SmallTypeCache.Char) size = 8;
                    else if (pType == SmallTypeCache.Short) size = 16;
                    else if (pType == SmallTypeCache.Int) size = 32;
                    else if (pType == SmallTypeCache.Long) size = 64;
                    else if (pType == SmallTypeCache.Float) size = 32;
                    else if (pType == SmallTypeCache.Double) size = 64;
                    else if (pType.IsEnum) size = 32;
                    else throw new ArgumentException("pType");

                    LLVMMetadataRef value = LLVM.DIBuilderCreateBasicType(pBuilder, pType.Name, size, 0, 0);
                    t = new DebugType(size, value);
                }
            }

            _debugMappings.Add(pType, t);
            return _debugMappings[pType];
        }
    }
}
