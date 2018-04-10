using System;
using System.Collections.Generic;

namespace ConnectedLivingSpace
{
  public class CLSSpace : ICLSSpace
  {
    List<ICLSPart> parts;
    List<ICLSKerbal> crew;
    String name;
    int maxCrew;
    CLSVessel vessel;

    public List<ICLSPart> Parts
    {
      get
      {
        return parts;
      }
    }

    public int MaxCrew
    {
      get
      {
        return maxCrew;
      }
    }

    public String Name
    {
      get
      {
        return name;
      }
      set
      {
        name = value;
        // If we change the name of the space, we need to also change the spaceName of all the parts that make it up.
        IEnumerator<ICLSPart> eParts = parts.GetEnumerator();
        while (eParts.MoveNext())
        {
          if (eParts.Current == null) continue;
          ModuleConnectedLivingSpace modCLS = (ModuleConnectedLivingSpace)((CLSPart)eParts.Current);
          if (modCLS)
          {
            modCLS.spaceName = value;
          }
        }
        eParts.Dispose();
      }
    }

    public ICLSVessel Vessel
    {
      get
      {
        return vessel;
      }
    }

    public List<ICLSKerbal> Crew
    {
      get
      {
        return crew;
      }
    }

    public CLSSpace(CLSVessel v)
    {
      parts = new List<ICLSPart>();
      crew = new List<ICLSKerbal>();
      name = "";
      vessel = v;
    }

    public void Highlight(bool val)
    {
      // Iterate through each CLSPart in this space and turn highlighting on or off.
      IEnumerator<ICLSPart> eParts = parts.GetEnumerator();
      while (eParts.MoveNext())
      {
        if (eParts.Current == null) continue;
        ((CLSPart)eParts.Current).Highlight(val);
      }
      eParts.Dispose();
    }

    internal void AddPart(CLSPart p)
    {
      // Add the part to the space, and the space to the part.
      p.Space = this;

      // If this space does not have a name, take the name from the part we just added.
      if ("" == name)
      {
        ModuleConnectedLivingSpace modCLS = (ModuleConnectedLivingSpace)p;

        if (null != modCLS)
        {
          name = modCLS.spaceName;
        }
      }

      parts.Add(p);

      maxCrew += ((Part)p).CrewCapacity;

      IEnumerator<ICLSKerbal> eCrew = p.Crew.GetEnumerator();
      while (eCrew.MoveNext())
      {
        if (eCrew.Current == null) continue;
        crew.Add((CLSKerbal)eCrew.Current);
      }
      eCrew.Dispose();
    }

    // A function to throw away all the parts references, and so break the circular reference. This should be called before throwing a CLSSpace away.
    internal void Clear()
    {
      IEnumerator<ICLSPart> eParts = parts.GetEnumerator();
      while (eParts.MoveNext())
      {
        if (eParts.Current == null) continue;
        ((CLSPart)eParts.Current).Clear();
      }
      eParts.Dispose();
      parts.Clear();
      vessel = null;
    }

  }
}
