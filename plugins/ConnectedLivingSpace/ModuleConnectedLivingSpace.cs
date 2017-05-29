using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConnectedLivingSpace
{
  // A module that can be added to a part in order to be able to set and read in part specific config that relates to ConnectedLivingSpace
  public class ModuleConnectedLivingSpace : PartModule
  {
    internal CLSPart clsPart; // reference back to the CLS Part that refers to the part that this is a module on. Tghis value might well be null, but the CLSPart will attempt to set it when the CLS part is created.

    [KSPField(isPersistant = true)]
    public bool passable;
    [KSPField(isPersistant = true)]
    public bool passableWhenSurfaceAttached;
    [KSPField(isPersistant = true)]
    public bool surfaceAttachmentsPassable;
    [KSPField(isPersistant = true)]
    public string passablenodes = "";
    [KSPField(isPersistant = true)]
    public string impassablenodes = "";
    [KSPField]
    public string impassableDockingNodeTypes = "";
    [KSPField]
    public string passableDockingNodeTypes = "";
    [KSPField(isPersistant = true)]
    public string spaceName;

    private string returnValue = string.Empty;

    // For localization.  These are the default (english) values...
    private static string _strYes = $"<color={XKCDColors.HexFormat.Lime}>Yes</color>";
    private static string _strNo = $"<color={ XKCDColors.HexFormat.Maroon}>No</color>";
    private static string _strAll = "All";
    private static string _strNone = "None";
    private static string _strPassable = "Passable";
    private static string _strCapacity = "Crew Capacity";
    private static string _strImpassableNodes = "Impassable Nodes";
    private static string _strPassableNodes = "Passable Nodes";
    private static string _strPassableSA = "Pass when Surface Attached";
    private static string _strSAPassable = "Surface Attached Parts Pass";

    public override void OnAwake()
    {
      try
      {

      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    /// <summary>
    /// Called during the Part startup.
    /// StartState gives flag values of initial state
    /// </summary>
    public override void OnStart(StartState state)
    {
      //Debug.Log($"[CLS]:  CLS::OnStart state={state}");

      try
      {
        // If the CLS Space name for this part is not set or empty, then set it to the title of this part.
        SetLocalization();
        SetEventGuiNames();

        if (null == spaceName)
        {
          spaceName = part.partInfo.title;
        }
        else if ("" == spaceName)
        {
          spaceName = part.partInfo.title;
        }
        SetEventState();
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    /// <summary>
    /// Called when PartModule is asked to save its values.
    /// Can save additional data here.
    /// </summary>
    /// <param name='node'>The node to save in to</param>
    public override void OnSave(ConfigNode node)
    {
      //node.AddValue("passable", passable);
      //node.AddValue("passableWhenSurfaceAttached", passableWhenSurfaceAttached);
      //node.AddValue("surfaceAttachmentsPassable", surfaceAttachmentsPassable);
      //node.AddValue("passablenodes", passablenodes);
      //node.AddValue("impassablenodes", passable);
      //node.AddValue("spaceName", spaceName);
    }

    /// <summary>
    /// Called when PartModule is asked to load its values.
    /// Can load additional data here.
    /// </summary>
    /// <param name='node'>The node to load from</param>
    public override void OnLoad(ConfigNode node)
    {
      SetEventState();
    }

    private void SetEventState()
    {
      Events["EnablePassable"].guiActiveEditor = 
      Events["DisablePassable"].guiActiveEditor = 
      Events["EnableSurfaceAttachable"].guiActiveEditor = 
      Events["DisableSurfaceAttachable"].guiActiveEditor = 
      Events["EnableAttachableSurface"].guiActiveEditor = 
      Events["DisableAttachableSurface"].guiActiveEditor = CLSAddon.EnablePassable;

      if (!CLSAddon.EnablePassable) return;

      if (passable)
      {
        Events["EnablePassable"].active = false;
        Events["DisablePassable"].active = true;
      }
      else
      {
        Events["EnablePassable"].active = true;
        Events["DisablePassable"].active = false;
      }
      if (passableWhenSurfaceAttached)
      {
        Events["EnableSurfaceAttachable"].active = false;
        Events["DisableSurfaceAttachable"].active = true;
      }
      else
      {
        Events["EnableSurfaceAttachable"].active = true;
        Events["DisableSurfaceAttachable"].active = false;
      }
      if (surfaceAttachmentsPassable)
      {
        Events["EnableAttachableSurface"].active = false;
        Events["DisableAttachableSurface"].active = true;
      }
      else
      {
        Events["EnableAttachableSurface"].active = true;
        Events["DisableAttachableSurface"].active = false;
      }
    }

    private void SetLocalization()
    {
      _strYes = $"<color={XKCDColors.HexFormat.Lime}>{CLSAddon.Localize("#clsloc_017")}</color>";
      _strNo = $"<color={ XKCDColors.HexFormat.Maroon}>{CLSAddon.Localize("#clsloc_018")}</color>";
      _strAll = CLSAddon.Localize("#clsloc_019");
      _strNone = CLSAddon.Localize("#clsloc_020");
      _strPassable = CLSAddon.Localize("#clsloc_021");
      _strCapacity = CLSAddon.Localize("#clsloc_022");
      _strImpassableNodes = CLSAddon.Localize("#clsloc_023");
      _strPassableNodes = CLSAddon.Localize("#clsloc_024");
      _strPassableSA = CLSAddon.Localize("#clsloc_025");
      _strSAPassable = CLSAddon.Localize("#clsloc_026");
    }

    private void SetEventGuiNames()
    {
      Events["EnablePassable"].guiName = CLSAddon.Localize("#clsloc_027");
      Events["DisablePassable"].guiName = CLSAddon.Localize("#clsloc_028");
      Events["EnableSurfaceAttachable"].guiName = CLSAddon.Localize("#clsloc_029");
      Events["DisableSurfaceAttachable"].guiName = CLSAddon.Localize("#clsloc_030");
      Events["EnableAttachableSurface"].guiName = CLSAddon.Localize("#clsloc_031");
      Events["DisableAttachableSurface"].guiName = CLSAddon.Localize("#clsloc_032");
    }

    // Allow a CLSPart to be cast into a ModuleConnectedLivingSpace. Note that this might fail, if the part in question does not have the CLS module configured.
    public static implicit operator ModuleConnectedLivingSpace(Part _p)
    {
      IEnumerator<ModuleConnectedLivingSpace> eModules = _p.Modules.OfType<ModuleConnectedLivingSpace>().GetEnumerator();
      while (eModules.MoveNext())
      {
        if (eModules.Current == null) continue;
        return (eModules.Current);
      }
      eModules.Dispose();
      return null;
    }

    // Method to provide extra infomation about the part on response to the RMBof the part gallery
    public override string GetInfo()
    {
      if (passable)
      {
        //returnValue += "Passable:  <color=" + XKCDColors.HexFormat.Lime + ">Yes</color>";
        //returnValue += "\r\nCrewable:  " + (part.CrewCapacity > 0 ? yes : no);
        //returnValue += "\r\nImpassable Nodes:  " + (impassablenodes != "" ? impassablenodes : (passable ? "None" : "All"));
        //returnValue += "\r\nPassable Nodes:  " + (passablenodes != "" ? passablenodes : (passable ? "All" : "None"));
        //returnValue += "\r\nPass when Surface Attached:  " + (passableWhenSurfaceAttached ? yes : no);
        //returnValue += "\r\nSurface Attached Parts Pass:  " + (surfaceAttachmentsPassable ? yes : no);
        returnValue += $"{_strPassable}:  {_strYes}";
        returnValue += $"\n{_strCapacity}:  {(part.CrewCapacity > 0 ? _strYes : _strNo)}";
        returnValue += $"\n{_strImpassableNodes}:  {(impassablenodes != "" ? impassablenodes : passable ? _strNone : _strAll)}";
        returnValue += $"\n{_strPassableNodes}:  {(passablenodes != "" ? passablenodes : passable ? _strAll : _strNone)}";
        returnValue += $"\n{_strPassableSA}:  {(passableWhenSurfaceAttached ? _strYes : _strNo)}";
        returnValue += $"\n{_strSAPassable}:  {(surfaceAttachmentsPassable ? _strYes : _strNo)}";
      }
      else
      {
        returnValue += $"{_strPassable}:  {_strNo}";
        if (passablenodes != "") returnValue += $"\n{_strPassableNodes}:  {passablenodes}";
      }
      return returnValue;
    }


    [KSPEvent(guiActive = false, guiActiveEditor = true, name = "DisablePassable", guiName = "CLS Passable: Yes")]
    public void DisablePassable()
    {
      passable = false;
      Events["EnablePassable"].active = true;
      Events["DisablePassable"].active = false;
    }

    [KSPEvent(guiActive = false, guiActiveEditor = true, name = "EnablePassable", guiName = "CLS Passable: No")]
    public void EnablePassable()
    {
      passable = true;
      Events["EnablePassable"].active = false;
      Events["DisablePassable"].active = true;
    }

    [KSPEvent(guiActive = false, guiActiveEditor = true, name = "DisableSurfaceAttachable", guiName = "CLS Surface Attachable: Yes")]
    public void DisableSurfaceAttachable()
    {
      passableWhenSurfaceAttached = false;
      Events["EnableSurfaceAttachable"].active = true;
      Events["DisableSurfaceAttachable"].active = false;
    }
    [KSPEvent(guiActive = false, guiActiveEditor = true, name = "EnableSurfaceAttachable", guiName = "CLS Surface Attachable: No")]
    public void EnableSurfaceAttachable()
    {
      passableWhenSurfaceAttached = true;
      Events["EnableSurfaceAttachable"].active = false;
      Events["DisableSurfaceAttachable"].active = true;
    }

    [KSPEvent(guiActive = false, guiActiveEditor = true, name = "DisableAttachableSurface", guiName = "CLS Attachable Surface: Yes")]
    public void DisableAttachableSurface()
    {
      surfaceAttachmentsPassable = false;
      Events["EnableAttachableSurface"].active = true;
      Events["DisableAttachableSurface"].active = false;
    }
    [KSPEvent(guiActive = false, guiActiveEditor = true, name = "EnableAttachableSurface", guiName = "CLS Attachable Surface: No")]
    public void EnableAttachableSurface()
    {
      surfaceAttachmentsPassable = true;
      Events["EnableAttachableSurface"].active = false;
      Events["DisableAttachableSurface"].active = true;
    }
  }
}
