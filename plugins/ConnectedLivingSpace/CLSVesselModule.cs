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

      // Recoupler support
      for (int i = CLSAddon.Instance.requestedConnections.Count - 1; i >= 0; i--)
      {
        CLSAddon.ConnectPair connectPair = CLSAddon.Instance.requestedConnections[i];
        if (connectPair.part1.vessel == this.vessel)
          CLSAddon.Instance.requestedConnections.Remove(connectPair);
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

        // Recoupler support
        for (int i = CLSAddon.Instance.requestedConnections.Count - 1; i >= 0; i--)
        {
          CLSAddon.ConnectPair connectPair = CLSAddon.Instance.requestedConnections[i];
          if (connectPair.part1.vessel != connectPair.part2.vessel)
            CLSAddon.Instance.requestedConnections.Remove(connectPair);
          _clsVessel.MergeSpaces(connectPair.part1, connectPair.part2);
        }

      }
      catch (Exception ex)
      {
        Debug.Log($"CLS rebuild Vessel Error:  { ex}");
      }
    }

  }
}
