using System;

namespace CSTI_LuaActionSupport.Attr;

[AttributeUsage(AttributeTargets.Field)]
public class ClsExport : Attribute
{
    public ClsExport(params Type[] extNeedExport)
    {
    }
}