using System.Runtime.InteropServices;

namespace EventSourcing;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct VersionMismatch
{
    public static VersionMismatch Instance { get; } = new VersionMismatch();
}
