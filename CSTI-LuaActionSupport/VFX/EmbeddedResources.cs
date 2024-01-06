using System.IO;

namespace CSTI_LuaActionSupport.VFX;

public static class EmbeddedResources
{
    // ReSharper disable once IdentifierTypo
    public static Stream CFXRBundle => typeof(LuaSupportRuntime).Assembly.GetManifestResourceStream("cfxr.bundle")!;
}