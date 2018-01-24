using System;
using UnityEngine;

namespace ConnectedLivingSpace
{
  public class CLSVesselModule : VesselModule
  {
    private bool dirty = true;

    private CLSVessel _clsVessel;
    internal CLSVessel CLSVessel
    {
      get
      {
        if (dirty) RebuildCLSVessel();
        return _clsVessel;
      }
    }

    public override Activation GetActivation()
    {
      return Activation.LoadedVessels;
    }

    public override void OnLoadVessel()
    {
      MarkDirty();
    }

    public override void OnUnloadVessel()
    {
      if (null != _clsVessel)
      {
        _clsVessel.Clear();
        _clsVessel = null;
      }
    }

    internal void MarkDirty()
    {
      dirty = true;
      CLSAddon.onCLSVesselChange.Fire(vessel);
    }

    private void RebuildCLSVessel()
    {
      dirty = false;

      if (null != _clsVessel)
      {
        _clsVessel.Clear();
        _clsVessel = null;
      }

      if (vessel.rootPart == null)
        return;
      
      try
      {
        // Build new vessel information
        _clsVessel = new CLSVessel();
        _clsVessel.Populate(vessel.rootPart);

        // TODO recoupler support
      }
      catch (Exception ex)
      {
        Debug.Log($"CLS rebuild Vessel Error:  { ex}");
      }
    }

  }
}
