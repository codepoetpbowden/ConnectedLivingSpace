using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConnectedLivingSpace
{
  // This module is added by a module Manager config to any part that also has a ModuleDockingNode. 
  // There is a one to one relationship between ModuleDockingHatch and ModuleDockingNode
  public class ModuleDockingHatch : PartModule, IModuleDockingHatch
  {
    [KSPField(isPersistant = true)]
    private bool hatchOpen;

    [KSPField(isPersistant = true)]
    internal string docNodeAttachmentNodeName = "top"; // Note, on some ModuleDockingNodes this does not exist, so we set the value to "none"

    [KSPField(isPersistant = true)]
    internal string docNodeTransformName = "dockingNode";

    internal ModuleDockingNode modDockNode;

    // For localization.  These are the default (english) values...
    private static string _strOpen = "Open";
    private static string _strClosed = "Closed";
    private static string _strHatchStatus = "Hatch Status";
    private static string _strOpenHatch = "Open Hatch";
    private static string _strCloseHatch = "Close Hatch";
    private static string _strHasHatch = "Has Hatch";
    private static string _strHatchNode = "Hatch Node";
    private static string _strYes = $"<color={XKCDColors.HexFormat.Lime}>Yes</color>";
    //private readonly string _strNo = "<color=" + XKCDColors.HexFormat.Maroon + ">No</color>";

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
        hatchStatus = value ? _strOpen : _strClosed;
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

      if (vessel != null) GameEvents.onVesselWasModified.Fire(vessel);
    }

    [KSPEvent(active = true, guiActive = true, guiName = "Close Hatch")]
    private void CloseHatch()
    {
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

      if (vessel != null) GameEvents.onVesselWasModified.Fire(vessel);
    }

    public override void OnLoad(ConfigNode node)
    {
      // The Loader with have set hatchOpen, but not via the Property HatchOpen, so we need to re-do it to ensure that hatchStatus gets properly set.
      HatchOpen = hatchOpen;

      // Set the GUI state of the open/close hatch events as appropriate
      if (HighLogic.LoadedScene != GameScenes.LOADING
          && (isInDockedState() || isAttachedToDockingPort()))
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
              Debug.Log($"Closing a hatch because its corresponding docking port is in state: {modDockNode.state}");

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
      hatchStatus = hatchOpen ? _strOpen : _strClosed;
    }

    private void SetLocalization()
    {
      //CacheClsLocalization();

      _strOpen = CLSAddon.Localize("#clsloc_033");
      _strClosed = CLSAddon.Localize("#clsloc_034");
      _strHatchStatus = CLSAddon.Localize("#clsloc_035");
      _strOpenHatch = CLSAddon.Localize("#clsloc_036");
      _strCloseHatch = CLSAddon.Localize("#clsloc_037");
      _strHasHatch = CLSAddon.Localize("#clsloc_038");
      _strHatchNode = CLSAddon.Localize("#clsloc_039");
      _strYes = $"<color={XKCDColors.HexFormat.Lime}>{CLSAddon.Localize("#clsloc_017")}</color>";
    }

    private void SetEventGuiNames()
    {
      Fields["hatchStatus"].guiName = _strHatchStatus;
      Events["OpenHatch"].guiName = _strOpenHatch;
      Events["CloseHatch"].guiName = _strCloseHatch;
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
        eNodes.Dispose();
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
      if (this.part.FindModulesImplementing<ModuleDockingNode>().Count != 1) return false;
      // we are good.  lets fix the hatch and continue
      modDockNode = this.part.FindModulesImplementing<ModuleDockingNode>().First();
      docNodeTransformName = modDockNode.nodeTransformName;
      docNodeAttachmentNodeName = modDockNode.referenceAttachNode;
      return true;
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
        Debug.LogError($" Error - Docking port hatch can not find its ModuleDockingNode docNodeTransformName: {docNodeTransformName} docNodeAttachmentNodeName: {docNodeAttachmentNodeName}");
      }

      return false;
    }

    // tries to work out if the docking port is attached to another docking port (ie in the VAB) and therefore can be treated as if it is docked (for example by not requiring the hatch to be closed)
    private bool isAttachedToDockingPort()
    {
      // First - this is only possible if we have an reference attachmentNode
      if (string.IsNullOrEmpty(docNodeAttachmentNodeName)) return false;
      AttachNode thisNode = part.attachNodes.Find(x => x.id == docNodeAttachmentNodeName);
      if (null == thisNode) return false;
      Part attachedPart = thisNode.attachedPart;
      if (null == attachedPart) return false;
      // What is the attachNode in the attachedPart that links back to us?
      AttachNode reverseNode = attachedPart.FindAttachNodeByPart(part);
      if (null == reverseNode) return false;
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
      eNodes.Dispose();
      return false;
    }

    // Method to provide extra infomation about the part on response to the RMB of the part gallery
    public override string GetInfo()
    {
      return $"{_strHasHatch}:  {_strYes}\n{_strHatchNode}:  <color={XKCDColors.HexFormat.Lime}>{docNodeAttachmentNodeName}</color>";
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
      SetLocalization();
      SetEventGuiNames();
    }
    #endregion
  }
}
