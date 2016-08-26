using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// KSPAddon with equality checking using an additional type parameter. Fixes the issue where AddonLoader prevents multiple start-once addons with the same start scene.
/// </summary>
internal class KSPAddonFixedCLS : KSPAddon, IEquatable<KSPAddonFixedCLS>
{
  private readonly Type type;

  public KSPAddonFixedCLS(KSPAddon.Startup startup, bool once, Type type)
    : base(startup, once)
  {
    this.type = type;
  }

  public override bool Equals(object obj)
  {
    if (obj.GetType() != GetType()) { return false; }
    return Equals((KSPAddonFixedCLS)obj);
  }

  public bool Equals(KSPAddonFixedCLS other)
  {
    if (once != other.once) { return false; }
    if (startup != other.startup) { return false; }
    if (type != other.type) { return false; }
    return true;
  }

  public override int GetHashCode()
  {
    return startup.GetHashCode() ^ once.GetHashCode() ^ type.GetHashCode();
  }
}
