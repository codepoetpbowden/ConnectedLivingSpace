using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConnectedLivingSpace
{
  public class CLSPart : ICLSPart
  {
    bool habitable;
    bool navigable;
    Part part;
    CLSSpace space;
    bool docked;
    List<ICLSKerbal> crew;
    public bool highlighted; // This allows us to remember if a part is SUPPOSED to be highlighted by CLS. We can then use appropriate moments to ensure that it either is or is not.

    public CLSPart(Part p)
    {
      part = p;

      habitable = IsHabitable(part);
      navigable = IsNavigable(part);
      space = null;

      crew = new List<ICLSKerbal>();
      IEnumerator<ProtoCrewMember> crewmembers = p.protoModuleCrew.GetEnumerator();
      while (crewmembers.MoveNext())
      {
        if (crewmembers.Current == null) continue;
        CLSKerbal kerbal = new CLSKerbal(crewmembers.Current, this);
        crew.Add(kerbal);
      }
      crewmembers.Dispose();

      // Does the part have a CLSModule on it? If so then give the module a reference to ourselves to make its life a bit easier.
      {
        ModuleConnectedLivingSpace m = (ModuleConnectedLivingSpace)this;
        if (null != m)
        {
          m.clsPart = this;
        }
        else
        {
          // Do not bother logging warnings about EVAs not being configured for CLS!
          if (part.Modules.OfType<KerbalEVA>().Count() == 0)
          {
            // This part does not have a CLSModule. If it is habitable or navigable then it will not be possible to persist the name of the space in the savefile. Log a warning.
            if (habitable)
            {
              Debug.LogWarning("Part " + part.partInfo.title + " is habitable but does not have ModuleConnectedLivingSpace defined in the config. It would be better if it did as some infomation used by CLS will not be saved in the savefile.");
            }
            else if (navigable)
            {
              Debug.LogWarning("Part " + part.partInfo.title + " is passable but does not have ModuleConnectedLivingSpace defined in the config. It would be better if it did as some infomation used by CLS will not be saved in the savefile.");
            }
          }
        }
      }
    }

    public ICLSSpace Space
    {
      get
      {
        return space;
      }

      internal set
      {
        space = (CLSSpace)value;
      }
    }
    public bool Docked
    {
      get
      {
        return docked;
      }
    }

    public List<ICLSKerbal> Crew
    {
      get
      {
        return crew;
      }
    }

    public ModuleConnectedLivingSpace modCLS
    {
      get
      {
        IEnumerator<ModuleConnectedLivingSpace> epModules =
          part.Modules.OfType<ModuleConnectedLivingSpace>().GetEnumerator();
        while (epModules.MoveNext())
        {
          if (epModules.Current == null) continue;
          return epModules.Current;
        }
        epModules.Dispose();
        return null;
      }
    }

    public Part Part
    {
      get
      {
        return part;
      }
    }

    // Allow a CLSPart to be cast into a Part
    public static implicit operator Part(CLSPart _p)
    {
      return _p.Part;
    }

    // Allow a CLSPart to be cast into a ModuleConnectedLivingSpace. Note that this might fail, if the part in question does not have the CLS module configured.
    public static implicit operator ModuleConnectedLivingSpace(CLSPart _p)
    {
      return _p.modCLS;
    }

    public void Highlight(bool val, bool force = false)
    {
      // Set the variable to mark if this part is SUPPOSED to be hightlighted or not.
      if (val && (!highlighted || force))
      {
        SetHighlighting();
        highlighted = val;
        part.highlightType = Part.HighlightType.AlwaysOn;
      }
      else
      {
        if (!val && (highlighted || force))
        {
          highlighted = val;
          part.SetHighlight(false, false);
          part.SetHighlightDefault();
          part.highlightType = Part.HighlightType.OnMouseOver;
        }
      }
    }

    // Actually set this part to be highlighted
    private void SetHighlighting()
    {
      part.SetHighlightDefault();

      // Choose the colour based in the type of part!
      if (Habitable)
      {
        part.SetHighlightColor(Color.green);
      }
      else if (docked)
      {
        // The part has at least one docked dockingnode. If any of the docking nodes for this part support hatches, and any of the hatches are closed then we wil colour magenta rather than cyan.

        Color docNodeColor = Color.cyan;

        IEnumerator<ModuleDockingHatch> epModules = part.Modules.OfType<ModuleDockingHatch>().GetEnumerator();
        while (epModules.MoveNext())
        {
          if (epModules.Current == null) continue;
          if (!epModules.Current.HatchOpen)
          {
            docNodeColor = Color.magenta;
            break;
          }
        }
        epModules.Dispose();
        part.SetHighlightColor(docNodeColor);
      }
      else if (Navigable)
      {
        // A navigable part might be an undocked docking port, in which case it still might have a closed/open hatch. check for this and colour apropriately.
        Color docNodeColor = Color.yellow;

        IEnumerator<ModuleDockingHatch> epModules = part.Modules.OfType<ModuleDockingHatch>().GetEnumerator();
        while (epModules.MoveNext())
        {
          if (epModules.Current == null) continue;
          if (!epModules.Current.HatchOpen)
          {
            docNodeColor.g = docNodeColor.g * 0.66f; // This will turn my yellow into orange.
            break;
          }
        }
        epModules.Dispose();
        part.SetHighlightColor(docNodeColor);
      }
      else
      {
        part.SetHighlightColor(Color.red);
      }
      part.SetHighlight(true, false);
    }

    public bool Habitable
    {
      get
      {
        return IsHabitable(part);
      }
    }

    public bool Navigable
    {
      get
      {
        return navigable;
      }
    }

    private bool IsHabitable(Part p)
    {
      return (p.CrewCapacity > 0);
    }

    private bool IsNavigable(Part p)
    {
      // first test - does it have a crew capacity?
      if (p.CrewCapacity > 0)
      {
        return true;
      }

      ModuleConnectedLivingSpace CLSMod = (ModuleConnectedLivingSpace)this;
      if (null != CLSMod)
      {
        return (CLSMod.passable);
      }

      return false;
    }

    internal void SetDocked(bool val)
    {
      docked = val;
    }

    // Throw away all potentially circular references in preparation this object to be thrown away
    internal void Clear()
    {
      space = null;
      IEnumerator<ICLSKerbal> eCrew = crew.GetEnumerator();
      while (eCrew.MoveNext())
      {
        if (eCrew.Current == null) continue;
        ((CLSKerbal)eCrew.Current).Clear();
      }
      eCrew.Dispose();
      crew.Clear();
    }
  }
}
