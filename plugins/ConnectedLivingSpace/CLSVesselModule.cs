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
    internal void MarkDirty()
    {
      dirty = true;
      CLSAddon.onCLSVesselChange.Fire(vessel);
    }

    private void RebuildCLSVessel()
    {
      if (vessel.rootPart == null)
      {
        ClearCLSVessel();
        return;
      }
      RebuildCLSVessel(vessel.rootPart);
    }

    private void RebuildCLSVessel(Part newRootPart)
    {
      try
      {
        //Debug.Log("RebuildCLSVessel");
        ClearCLSVessel();

        // Build new vessel information
        _clsVessel = new CLSVessel();
        _clsVessel.Populate(newRootPart);

        // TODO recoupler support
      }
      catch (Exception ex)
      {
        Debug.Log($"CLS rebuild Vessel Error:  { ex}");
      }
    }

    private void ClearCLSVessel()
    {
      if (null != _clsVessel)
      {
        _clsVessel.Clear();
        _clsVessel = null;
      }
    }
  }
}