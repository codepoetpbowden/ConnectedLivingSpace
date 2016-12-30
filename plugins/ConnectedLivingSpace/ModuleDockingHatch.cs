using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace ConnectedLivingSpace
{
  // This module is added by a module Manager config to any part that also has a ModuleDockingNode. There will be a one to one relationship between ModuleDockingHatch and ModuleDockingNode
  public class ModuleDockingHatch : PartModule, IModuleDockingHatch
  {
    [KSPField(isPersistant = true)]
    private bool hatchOpen;

    [KSPField(isPersistant = true)]
    internal string docNodeAttachmentNodeName = "top"; // Note, on some ModuleDockingNodes this does not exist, so we set the value to "none"

    [KSPField(isPersistant = true)]
    internal string docNodeTransformName = "dockingNode";

    internal ModuleDockingNode modDockNode;

    public bool HatchOpen
    {
      get
      {
        return hatchOpen;
      }

      set
      {
        if (hatchOpen != value)
        {
          if (value) OpenHatch();
          else CloseHatch();
        }
        hatchStatus = value ? "Open" : "Closed";
      }
    }

    public string HatchStatus
    {
      get
      {
        return hatchStatus;
      }
    }

    public bool IsDocked
    {
      get
      {
        return isInDockedState() || isAttachedToDockingPort();
      }
    }

    public BaseEventList HatchEvents
    {
      get
      {
        return Events;
      }
    }

    public ModuleDockingNode ModDockNode
    {
      get
      {
        return modDockNode;
      }
    }

    [KSPField(isPersistant = false, guiActive = true, guiName = "Hatch status")]
    private string hatchStatus = "";

    [KSPEvent(active = true, guiActive = true, guiName = "Open Hatch")]
    private void OpenHatch()
    {
      Events["OpenHatch"].active = false;
      if (isInDockedState() || isAttachedToDockingPort())
      {
        hatchOpen = true;
        Events["CloseHatch"].active = true;
      }
      else
      {
        hatchOpen = false;
        Events["CloseHatch"].active = false;
      }

      // Finally fire the VesselChange event to cause the CLSAddon to re-evaluate everything. ActiveVessel is only available in flight. 
      // However, it should only be possible to open and close hatches in flight, so we should be OK.
      if (HighLogic.LoadedSceneIsFlight) GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
    }

    [KSPEvent(active = true, guiActive = true, guiName = "Close Hatch")]
    private void CloseHatch()
    {
      bool docked = isInDockedState();

      hatchOpen = false;

      Events["CloseHatch"].active = false;
      if (isInDockedState() || isAttachedToDockingPort())
      {
        Events["OpenHatch"].active = true;
      }
      else
      {
        Events["OpenHatch"].active = false;
      }

      // Finally fire the VesselChange event to cause the CLSAddon to re-evaluate everything. ActiveVEssel is only available in flight, but then it should only be possible to open and close hatches in flight so we should be OK.
      GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
    }

    public override void OnLoad(ConfigNode node)
    {
      //Debug.Log("ModuleDockingHatch::OnLoad");
      //Debug.Log("this.docNodeAttachmentNodeName: " + this.docNodeAttachmentNodeName);
      //Debug.Log("this.docNodeTransformName: " + this.docNodeTransformName);
      //Debug.Log("node.GetValue(docNodeTransformName): " + node.GetValue("docNodeTransformName"));
      //Debug.Log("node.GetValue(docNodeAttachmentNodeName): " + node.GetValue("docNodeAttachmentNodeName"));

      // The Loader with have set hatchOpen, but not via the Property HatchOpen, so we need to re-do it to ensure that hatchStatus gets properly set.
      HatchOpen = hatchOpen;

      // Set the GUI state of the open/close hatch events as appropriate
      if (isInDockedState() || isAttachedToDockingPort())
      {
        if (HatchOpen)
        {
          Events["CloseHatch"].active = true;
          Events["OpenHatch"].active = false;
        }
        else
        {
          Events["CloseHatch"].active = false;
          Events["OpenHatch"].active = true;
        }
      }
      else
      {
        Events["CloseHatch"].active = false;
        Events["OpenHatch"].active = false;
      }
    }

    // Called every physics frame. Make sure that the menu options are valid for the state that we are in. 
    private void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        if (!FlightGlobals.ready) return;
        if (isInDockedState())
        {
          if (!HatchOpen)
          {
            // We are docked, but the hatch is closed. Make sure that it is possible to open the hatch
            Events["CloseHatch"].active = false;
            Events["OpenHatch"].active = true;
          }
        }
        else
        {
          if (isAttachedToDockingPort())
          {
            if (!HatchOpen)
            {
              // We are not docked, but attached to a docking port, and the hatch is closed. Make sure that it is possible to open the hatch
              Events["CloseHatch"].active = false;
              Events["OpenHatch"].active = true;
            }
            else
            {
              // We are not docked, but attached to a docking port, and the hatch is open. Make sure that it is possible to close the hatch
              Events["CloseHatch"].active = true;
              Events["OpenHatch"].active = false;
            }
          }
          else
          {
            // We are not docked or attached to a docking port - close up the hatch if it is open!
            if (HatchOpen)
            {
              Debug.Log("Closing a hatch because its corresponding docking port is in state: " + modDockNode.state);

              hatchOpen = false;
              Events["CloseHatch"].active = false;
              Events["OpenHatch"].active = false;
            }
          }
        }
      }
      else if (HighLogic.LoadedSceneIsEditor)
      {
        // In the editor force the hatches open for attached docking ports so it is possible to see the living spaces at design time.
        if (isAttachedToDockingPort())
        {
          hatchOpen = true;
        }
      }
    }

    private bool CheckModuleDockingNode()
    {
      if (null == modDockNode)
      {
        // We do not know which ModuleDockingNode we are attached to yet. Try to find one.
        IEnumerator<ModuleDockingNode> eNodes = part.Modules.OfType<ModuleDockingNode>().GetEnumerator();
        while (eNodes.MoveNext())
        {
          if (eNodes.Current == null) continue;
          if (IsRelatedDockingNode(eNodes.Current))
          {
            modDockNode = eNodes.Current;
            return true;
          }
        }
      }
      else
      {
        return true;
      }
      return false;
    }

    // This method allows us to check if a specified ModuleDockingNode is one that this hatch is attached to
    internal bool IsRelatedDockingNode(ModuleDockingNode dockNode)
    {
      if (dockNode.nodeTransformName == docNodeTransformName)
      {
        if (string.IsNullOrEmpty(docNodeAttachmentNodeName)) docNodeAttachmentNodeName = dockNode.referenceNode.id;
        modDockNode = dockNode;
        return true;
      }
      if (dockNode.referenceNode.id == docNodeAttachmentNodeName)
      {
        if (string.IsNullOrEmpty(docNodeTransformName)) docNodeTransformName = dockNode.nodeTransformName;
        modDockNode = dockNode;
        return true;
      }
      // If we are here, we have an orphaned hatch.  we may be able to recover if the part only has one docking module...
      // TODO, check for dups in the same part...
      if (this.part.FindModulesImplementing<ModuleDockingNode>().Count == 1)
      {
        // we are good.  lets fix the hatch and continue
        modDockNode = this.part.FindModulesImplementing<ModuleDockingNode>().First();
        docNodeTransformName = modDockNode.nodeTransformName;
        docNodeAttachmentNodeName = modDockNode.referenceAttachNode;
        return true;
      }
      return false;
    }

    // tries to work out if the docking port is docked based on the state
    private bool isInDockedState()
    {
      // First ensure that we know which ModuleDockingNode we are referring to.
      if (CheckModuleDockingNode())
      {
        if (modDockNode.state == "Docked (dockee)" || modDockNode.state == "Docked (docker)")
        {
          return true;
        }
      }
      else
      {
        // This is bad - it means there is a hatch that we can not match to a docking node. This should not happen. We will log an error but it will likely spam the log.
        Debug.LogError(" Error - Docking port hatch can not find its ModuleDockingNode docNodeTransformName:" + docNodeTransformName + " docNodeAttachmentNodeName " + docNodeAttachmentNodeName);
      }

      return false;
    }

    // tries to work out if the docking port is attached to another docking port (ie in the VAB) and therefore can be treated as if it is docked (for example by not requiring the hatch to be closed)
    private bool isAttachedToDockingPort()
    {
      // First - this is only possible if we have an reference attachmentNode
      if (!string.IsNullOrEmpty(docNodeAttachmentNodeName))
      {
        AttachNode thisNode = part.attachNodes.Find(x => x.id == docNodeAttachmentNodeName);
        if (null != thisNode)
        {
          Part attachedPart = thisNode.attachedPart;
          if (null != attachedPart)
          {
            // What is the attachNode in the attachedPart that links back to us?
            AttachNode reverseNode = attachedPart.FindAttachNodeByPart(part);
            if (null != reverseNode)
            {
              // Now the big question - is the attached part a docking node that is centred on the reverseNode?
              IEnumerator<ModuleDockingNode> eNodes = attachedPart.Modules.OfType<ModuleDockingNode>().GetEnumerator();
              while (eNodes.MoveNext())
              {
                if (eNodes.Current == null) continue;
                if (eNodes.Current.referenceNode.id == reverseNode.id)
                {
                  // The part has a docking node that references the attachnode that connects back to our part - this is what we have been looking for!
                  return true;
                }
              }
            }
          }
        }
      }

      return false;
    }

    //// Method that can be used to set up the ModuleDockingNode that this ModuleDockingHatch refers to.
    //public void AttachModuleDockingNode(ModuleDockingNode _modDocNode)
    //{
    //  modDockNode = _modDocNode;

    //  docNodeTransformName = _modDocNode.nodeTransformName;
    //  docNodeAttachmentNodeName = _modDocNode.referenceNode.id;
    //}

    // Method to provide extra infomation about the part on response to the RMBof the part gallery
    public override string GetInfo()
    {
      string returnValue = string.Empty;
      string yes = "<color=" + XKCDColors.HexFormat.Lime + ">Yes</color>";
      string no = "<color=" + XKCDColors.HexFormat.Maroon + ">No</color>";
      returnValue += "Has Hatch:  <color=" + XKCDColors.HexFormat.Lime + ">Yes</color>";
      returnValue += "\r\nHatch Node:  <color=" + XKCDColors.HexFormat.Lime + ">" + docNodeAttachmentNodeName  + "</color>";
      return returnValue;
    }


    #region Event Handlers
    public override void OnSave(ConfigNode node)
    {
      //node.SetValue("docNodeAttachmentNodeName", this.part.FindModuleImplementing<ModuleDockingNode>().referenceAttachNode, true);
    }

    public override void OnAwake()
    {
      docNodeAttachmentNodeName = part.FindModuleImplementing<ModuleDockingNode>().referenceAttachNode;
      docNodeTransformName = part.FindModuleImplementing<ModuleDockingNode>().nodeTransformName;
    }

    public override void OnStart(PartModule.StartState state)
    {

    }
    #endregion
  }
}
