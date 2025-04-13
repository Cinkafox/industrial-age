using Robust.Shared.Serialization;

namespace Content.Shared.DrawDepth;

[Flags]
[FlagsFor(typeof(VisibilityMaskLayer))]
public enum VisibilityFlags : int
{
    None   = 0,
    Normal = 1 << 0,
    Ghost  = 1 << 1,
    Subfloor = 1 << 2,
}