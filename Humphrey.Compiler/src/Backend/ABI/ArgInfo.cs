using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Extensions;
using Humphrey.Backend;
using LLVMSharp;
using LLVMSharp.Interop;
using Microsoft.VisualBasic;
using static Extensions.Helpers;


public struct ArgInfo
{
    public enum EArgKind
    {
        Ignore,
        Extend,
        Direct,
        Cast,
        Indirect,
        Expand,
        InAlloca,
    }

    public ArgInfo(EArgKind kind)
    {
        this.kind = kind;
        this.typeData = default;
        this.paddingType = default;
//        this.paddingInReg = false;
        this.inAllocaSRet = false;
        this.indirectByVal = false;
        this.indirectRealign = false;
        this.sRetAfterThis = false;
//        this.inReg = false;
        this.canBeFlattened = false;
        this.extra = 0;
    }

    LLVMTypeRef typeData;
    LLVMTypeRef paddingType;
    EArgKind kind;
//    bool paddingInReg;
    bool inAllocaSRet;
    bool indirectByVal;
    bool indirectRealign;
    bool sRetAfterThis;
//    bool inReg;
    bool canBeFlattened;

    uint extra; // For Direct, this is the offset in bytes, for indirect its alignment

    public LLVMTypeRef CoerceType => typeData;

    public void setCoerceToType(LLVMTypeRef type)
    {
        typeData = type;
    }

    public void setPaddingType(LLVMTypeRef type)
    {
        paddingType = type;
    }

    public void setDirectOffset(uint offset)
    {
        extra = offset;
    }

    public void setIndirectAlign(uint align)
    {
        extra = align;
    }

    public void setCanBeFlattened(bool value)
    {
        canBeFlattened = value;
    }

    public static ArgInfo getIgnore()
    {
        return new ArgInfo(ArgInfo.EArgKind.Ignore);
    }

    public static ArgInfo getDirect(CompilationUnit unit, LLVMTypeRef type, uint offset = 0)
    {
        var info = new ArgInfo(ArgInfo.EArgKind.Direct);
        info.setCoerceToType(type);
        info.setDirectOffset(offset);
        info.setPaddingType(unit.Context.VoidType);
        info.setCanBeFlattened(true);
        return info;
    }

    public static ArgInfo getCast(CompilationUnit unit, LLVMTypeRef type, uint castSize = 0)
    {
        var info = new ArgInfo(ArgInfo.EArgKind.Cast);
        info.setCoerceToType(type);
        info.setDirectOffset(castSize);
        return info;
    }

    public static ArgInfo getExtend(CompilationUnit unit, LLVMTypeRef type)
    {
        var info = new ArgInfo(ArgInfo.EArgKind.Extend);
        info.setCoerceToType(type);
        info.setDirectOffset(0);
        return info;
    }

    public static ArgInfo getIndirect(CompilationUnit unit, uint alignment, LLVMTypeRef type, bool byVal=true, bool realign=false)
    {
        var info = new ArgInfo(ArgInfo.EArgKind.Indirect);
        info.setIndirectAlign(alignment);
        info.setCoerceToType(type);
        info.indirectByVal = byVal;
        info.indirectRealign = realign;
        info.setPaddingType(unit.Context.VoidType);
        return info;
    }

    public EArgKind getKind()
    {
        return kind;
    }

    public LLVMTypeRef getExpandType()
    {
        if (kind == EArgKind.Expand)
        {
            return typeData;
        }
        throw new Exception("Invalid kind");
    }

    public LLVMTypeRef PaddingType => paddingType;

    public bool IsSRetAfterThis => sRetAfterThis;
    public bool CanBeFlattened => canBeFlattened;

    public bool IsInAllocaSRet => inAllocaSRet;

    public bool IsDirect => kind == EArgKind.Direct;

    public bool IsIndirect => kind == EArgKind.Indirect;

    public uint IndirectAlign => extra;
    public uint DirectOffset => extra;

}

