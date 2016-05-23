using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KSP.UI.Screens;
using UnityEngine;

namespace ConnectedLivingSpace
{
  [KSPAddonFixedCLS(KSPAddon.Startup.EveryScene, false, typeof(CLSAddon))]
  public class CLSAddon : MonoBehaviour, ICLSAddon
  {
    #region Properties
    private static Rect windowPosition = new Rect(0, 0, 360, 480);
    private static Rect windowOptionsPosition = new Rect(0, 0, 0, 0);
    private static GUIStyle windowStyle = null;
    public static bool allowUnrestrictedTransfers = false;
    public static bool enableBlizzyToolbar = false;
    public static bool enablePassable = false;
    private static bool prevEnableBlizzyToolbar = false;
    private static readonly string SETTINGS_FILE = KSPUtil.ApplicationRootPath + "GameData/cls_settings.dat";
    private ConfigNode settings = null;
    private static bool windowVisable = false;
    private bool optionsVisible = false;

    private Vector2 scrollViewer = Vector2.zero;

    private CLSVessel vessel = null;

    // this var is now restricted to use by the CLS window.  Highlighting will be handled by part.
    int WindowSelectedSpace = -1;

    private static ApplicationLauncherButton stockToolbarButton = null; // Stock Toolbar Button
    internal static IButton blizzyToolbarButton = null; // Blizzy Toolbar Button


    private int editorPartCount = 0; // This is horrible. Because there does not seem to be an obvious callback to sink when parts are added and removed in the editor, on each fixed update we will could the parts and if it has changed then rebuild the CLSVessel. Yuk!

    private string spaceNameEditField;

    public ICLSVessel Vessel
    {
      get
      {
        return this.vessel;
      }
    }

    public static CLSAddon Instance
    {
      get;
      private set;
    }

    #endregion Properties

    #region Constructor
    public CLSAddon()
    {
      if (Instance == null)
      {
        Instance = this;
      }
    }
    #endregion Constructor

    #region Event handlers (in use)
    public void Awake()
    {
      //Debug.Log("CLSAddon:Awake");
      // Added support for Blizzy Toolbar and hot switching between Stock and Blizzy
      if (enableBlizzyToolbar)
      {
        // Let't try to use Blizzy's toolbar
        //Debug.Log("CLSAddon.Awake - Blizzy Toolbar Selected.");
        if (!ActivateBlizzyToolBar())
        {
          // We failed to activate the toolbar, so revert to stock
          //Debug.Log("CLSAddon.Awake - Stock Toolbar Selected.");
          GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
        }
      }
      else
      {
        // Use stock Toolbar
        //Debug.Log("CLSAddon.Awake - Stock Toolbar Selected.");
        GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
        GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
      }
    }

    public void Start()
    {
      // Debug.Log("CLSAddon:Start");

      windowStyle = new GUIStyle(HighLogic.Skin.window);

      // load toolbar selection setting
      ApplySettings();

      if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
      {
        GameEvents.onPartAttach.Add(OnPartAttach);
        GameEvents.onPartCouple.Add(OnPartCouple);
        GameEvents.onPartDie.Add(OnPartDie);
        GameEvents.onPartExplode.Add(OnPartExplode);
        GameEvents.onPartRemove.Add(OnPartRemove);
        GameEvents.onPartUndock.Add(OnPartUndock);
        GameEvents.onStageSeparation.Add(OnStageSeparation);
        GameEvents.onUndock.Add(OnUndock);
        GameEvents.onVesselCreate.Add(OnVesselCreate);
        GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        GameEvents.onVesselWasModified.Add(OnVesselWasModified);
        GameEvents.onVesselChange.Add(OnVesselChange);
        GameEvents.onVesselLoaded.Add(OnVesselLoaded);
        GameEvents.onVesselTerminated.Add(OnVesselTerminated);
        GameEvents.onFlightReady.Add(OnFlightReady);

        GameEvents.onCrewTransferred.Add(OnCrewTransfered);


        ////KSP 1.0 has an issue with GameEvents.onGUIAppLauncherReady.  It does not fire as expected.  This code line accounts for it.
        //// Reference:  http://forum.kerbalspaceprogram.com/threads/86682-Appilcation-Launcher-and-Mods?p=1871124&viewfull=1#post1871124
        //if (!enableBlizzyToolbar && ApplicationLauncher.Ready)
        //  OnGUIAppLauncherReady();
      }

      // Add the CLSModule to all parts that can house crew (and do not already have it).
      AddModuleToParts();

      // Add hatches to all the docking ports (prefabs)
      AddHatchModuleToPartPrefabs();
    }

    public void Update()
    {
      // Debug.Log("CLSAddon:Update");
      CheckForToolbarTypeToggle();
    }

    public void FixedUpdate()
    {
      try
      {
        //Debug.Log("CLSAddon:FixedUpdate");

        // Although hatches have been added to the docking port prefabs, for some reason that is not fully understood when the prefab is used to instantiate an actual part the hatch module has not been properly setup. This is not a problem where the craft is being loaded, as the act of loading it will overwrite all the persisted KSPFields with the saved values. However in the VAB/SPH we end up with a ModuleDockingHatch that has not its docNodeTransformName or docNodeAttahcmentNodeName set properly. The solution is to check for this state in the editor, and patch it up. In flight the part will get loaded so it is not an issue.
        if (HighLogic.LoadedSceneIsEditor)
        {
          CheckAndFixDockingHatchesInEditor();
        }

        // It seems that there are sometimes problems with hatches that do not refer to dockingports in flight too, so check this in flight. It would be good to find a way of making this less expensive.
        if (HighLogic.LoadedSceneIsFlight)
        {
          if (FlightGlobals.ready)
          {
            CheckAndFixDockingHatchesInFlight();
          }
        }

        // If we are in the editor, and there is a ship in the editor, then compare the number of parts to last time we did this. If it has changed then rebuild the CLSVessel
        if (HighLogic.LoadedSceneIsEditor)
        {
          int currentPartCount = 0;
          if (null == EditorLogic.RootPart)
          {
            currentPartCount = 0; // I know that this is already 0, but just to make the point - if there is no startPod in the editor, then there are no parts in the vessel.
          }
          else
          {
            currentPartCount = EditorLogic.SortedShipList.Count;
          }

          if (currentPartCount != this.editorPartCount)
          {
            //Debug.Log("Calling RebuildCLSVessel as the part count has changed in the editor");

            this.RebuildCLSVessel();
            this.editorPartCount = currentPartCount;
          }
        }
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    public void OnDestroy()
    {
      //Debug.Log("CLSAddon::OnDestroy");

      saveSettings();

      GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
      GameEvents.onVesselChange.Remove(OnVesselChange);
      GameEvents.onPartAttach.Remove(OnPartAttach);
      GameEvents.onPartCouple.Remove(OnPartCouple);
      GameEvents.onPartDie.Remove(OnPartDie);
      GameEvents.onPartExplode.Remove(OnPartExplode);
      GameEvents.onPartRemove.Remove(OnPartRemove);
      GameEvents.onPartUndock.Remove(OnPartUndock);
      GameEvents.onStageSeparation.Remove(OnStageSeparation);
      GameEvents.onUndock.Remove(OnUndock);
      GameEvents.onVesselCreate.Remove(OnVesselCreate);
      GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
      GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
      GameEvents.onVesselChange.Remove(OnVesselChange);
      GameEvents.onVesselTerminated.Remove(OnVesselTerminated);
      GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
      GameEvents.onFlightReady.Remove(OnFlightReady);

      // Remove the stock toolbar button
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
      if (stockToolbarButton != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
      }

      GameEvents.onCrewTransferred.Remove(OnCrewTransfered);
    }

    void OnGUIAppLauncherReady()
    {
      stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
          OnCLSButtonToggle,
          OnCLSButtonToggle,
          DummyVoid,
          DummyVoid,
          DummyVoid,
          DummyVoid,
          ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
          (Texture)GameDatabase.Instance.GetTexture("ConnectedLivingSpace/assets/cls_icon_off", false));
    }

    void OnGUIAppLauncherDestroyed()
    {
      if (stockToolbarButton != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
        stockToolbarButton = null;
      }
    }

    void onAppLaunchToggleOff()
    {
      if (null != this.vessel)
      {
        vessel.Highlight(false);
      }
      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("ConnectedLivingSpace/assets/cls_icon_off", false));

      windowVisable = false;
    }

    void DummyVoid() { }

    private void OnFlightReady()
    {
      //Debug.Log("CLSAddon::OnFlightReady");          

      // Now scan the vessel
      //Debug.Log("Calling RebuildCLSVessel from onFlightReady");
      this.RebuildCLSVessel();
    }

    private void OnVesselLoaded(Vessel data)
    {
      //Debug.Log("CLSAddon::OnVesselLoaded");

      //Debug.Log("Calling RebuildCLSVessel from OnVesselLoaded");
      //  This check is needed to differentiate from nearby debris, as event is fired for every object within range
      if (data.Equals(FlightGlobals.ActiveVessel))
        RebuildCLSVessel(data);
    }

    private void OnVesselWasModified(Vessel data)
    {
      //Debug.Log("CLSAddon::OnVesselWasModified");

      //Debug.Log("Calling RebuildCLSVessel from OnVesselWasModified");

      RebuildCLSVessel(data);
    }

    // This event is fired when the vessel is changed. If this happens we need to throw away all of our thoiughts about the previous vessel, and analyse the new one.
    private void OnVesselChange(Vessel data)
    {
      //Debug.Log("CLSAddon::OnVesselChange");

      //Debug.Log("Calling RebuildCLSVessel from OnVesselChange");
      RebuildCLSVessel(data);
    }

    internal void OnCLSButtonToggle()
    {
      //Debug.Log("CLSAddon::OnCLSButtonToggle");
      windowVisable = !windowVisable;

      if (!windowVisable && null != vessel)
        vessel.Highlight(false);

      if (enableBlizzyToolbar)
        blizzyToolbarButton.TexturePath = windowVisable ? "ConnectedLivingSpace/assets/cls_b_icon_on" : "ConnectedLivingSpace/assets/cls_b_icon_off";
      else
        stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(windowVisable ? "ConnectedLivingSpace/assets/cls_icon_on" : "ConnectedLivingSpace/assets/cls_icon_off", false));
    }

    private void OnGUI()
    {
      if (windowVisable)
      {
        //Set the GUI Skin
        //GUI.skin = HighLogic.Skin;

        windowPosition = GUILayout.Window(947695, windowPosition, OnWindow, "Connected Living Space", windowStyle, GUILayout.MinHeight(20), GUILayout.ExpandHeight(true));
        if (this.optionsVisible)
        {
          if (windowOptionsPosition == new Rect(0, 0, 0, 0))
            windowOptionsPosition = new Rect(windowPosition.x + windowPosition.width + 10, windowPosition.y, 260, 115);
          windowOptionsPosition = GUILayout.Window(947696, windowOptionsPosition, DisplayOptionWindow, "Options", windowStyle, GUILayout.MinHeight(20), GUILayout.ExpandHeight(true));
        }
      }
      else
      {
        if (WindowSelectedSpace > -1)
        {
          vessel.Spaces[WindowSelectedSpace].Highlight(false);
          WindowSelectedSpace = -1;
        }

      }
    }

    #endregion Event Handlers (in Use)

    #region Event Handlers (not in use)
    private void OnVesselTerminated(ProtoVessel data)
    {
      //Debug.Log("CLSAddon::OnVesselTerminated");
    }
    private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> data)
    {
      //Debug.Log("CLSAddon::OnPartAttach"); 
    }
    private void OnPartCouple(GameEvents.FromToAction<Part, Part> data)
    {
      //Debug.Log("CLSAddon::OnPartCouple");
    }
    private void OnPartDie(Part data)
    {
      //Debug.Log("CLSAddon::OnPartDie");
    }
    private void OnPartExplode(GameEvents.ExplosionReaction data)
    {
      //Debug.Log("CLSAddon::OnPartExplode");
    }
    private void OnPartRemove(GameEvents.HostTargetAction<Part, Part> data)
    {
      //Debug.Log("CLSAddon::OnPartRemove");
    }
    private void OnPartUndock(Part data)
    {
      //Debug.Log("CLSAddon::OnPartUndock");
    }
    private void OnStageSeparation(EventReport eventReport)
    {
      //Debug.Log("CLSAddon::OnStageSeparation");
    }
    private void OnUndock(EventReport eventReport)
    {
      //Debug.Log("CLSAddon::OnUndock");
    }
    private void OnVesselDestroy(Vessel data)
    {
      //Debug.Log("CLSAddon::OnVesselDestroy");
    }
    private void OnVesselCreate(Vessel data)
    {
      //Debug.Log("CLSAddon::OnVesselCreate");
    }

    #endregion Event Handlers (not in use)

    #region Support/action Methods
    private void RebuildCLSVessel()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        RebuildCLSVessel(FlightGlobals.ActiveVessel);
      }
      else if (HighLogic.LoadedSceneIsEditor)
      {
        if (null == EditorLogic.RootPart)
        {
          // There is no root part in the editor - this ought to mean that there are no parts. Juest clear out everything
          if (null != this.vessel)
          {
            vessel.Clear();
          }
          this.vessel = null;
        }
        else
        {
          RebuildCLSVessel(EditorLogic.RootPart);
        }
      }
    }

    private void CheckForToolbarTypeToggle()
    {
      if (enableBlizzyToolbar && !prevEnableBlizzyToolbar)
      {
        // Let't try to use Blizzy's toolbar
        if (!ActivateBlizzyToolBar())
        {
          // We failed to activate the toolbar, so revert to stock
          GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);

          enableBlizzyToolbar = prevEnableBlizzyToolbar;
        }
        else
        {
          OnGUIAppLauncherDestroyed();
          GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
          prevEnableBlizzyToolbar = enableBlizzyToolbar;
          if (HighLogic.LoadedSceneIsFlight)
            blizzyToolbarButton.Visible = true;
        }

      }
      else if (!enableBlizzyToolbar && prevEnableBlizzyToolbar)
      {
        // Use stock Toolbar
        if (HighLogic.LoadedSceneIsFlight)
          blizzyToolbarButton.Visible = false;
        GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
        GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
        OnGUIAppLauncherReady();
        prevEnableBlizzyToolbar = enableBlizzyToolbar;
      }
    }


    private void RebuildCLSVessel(Vessel newVessel)
    {
      RebuildCLSVessel(newVessel.rootPart);
    }

    private void RebuildCLSVessel(Part newRootPart)
    {
      try
      {
        //Debug.Log("RebuildCLSVessel");
        // Before we rebuild the vessel, we need to take some steps to tidy up the highlighting. 
        // We will make a list of all the parts that are currently highlighted. We will also unhighlight parts that are highlighted. 
        // Once the rebuild is complete we will then highlight any parts that are still in the list we created.

        uint flightID = 0;
        List<CLSPart> listHighlightedParts = new List<CLSPart>();
        if (null != this.vessel)
        {
          try
          {
            var spaces = vessel.Spaces.GetEnumerator();
            while (spaces.MoveNext())
            {
              var parts = spaces.Current.Parts.GetEnumerator();
              while (parts.MoveNext())
              {
                Part part = parts.Current.Part;
                if (flightID != part.flightID)
                {
                  flightID = part.flightID;
                  //Debug.Log("Part : "+ part.flightID + " found." ) ;
                }
                if (((CLSPart)parts.Current).highlighted)
                {
                  listHighlightedParts.Add((CLSPart)parts.Current);
                  ((CLSPart)parts.Current).Highlight(false);
                }
              }
            }
          }
          catch (Exception ex)
          {
            Debug.Log("CLS highlighted parts gathering Error:  " + ex.ToString());
          }
          //Debug.Log("Old selected vessel had "+ listHighlightedParts.Count + " parts in it.");
          vessel.Clear();
        }

        // Tidy up the old vessel information
        this.vessel = null;

        // Build new vessel information
        this.vessel = new CLSVessel();
        this.vessel.Populate(newRootPart);
      }
      catch (Exception ex)
      {
        Debug.Log("CLS rebuild Vessel Error:  " + ex.ToString());
      }
    }

    private void OnWindow(int windowID)
    {
      DisplayCLSWindow();
    }

    private void DisplayCLSWindow()
    {
      try
      {
        Rect rect = new Rect(windowPosition.width - 20, 4, 16, 16);
        if (GUI.Button(rect, ""))
        {
          OnCLSButtonToggle();
        }
        rect = new Rect(windowPosition.width - 90, 4, 65, 16);
        if (GUI.Button(rect, new GUIContent("Options", "Click to view/edit options")))
        {
          this.optionsVisible = !this.optionsVisible;
        }
        GUILayout.BeginVertical();
        GUI.enabled = true;

        // Build a string descibing the contents of each of the spaces.
        if (null != this.vessel)
        {

          string[] spaceNames = new string[vessel.Spaces.Count];
          int counter = 0;
          int newSelectedSpace = -1;

          string partsList = "";
          var spaces = vessel.Spaces.GetEnumerator();
          while (spaces.MoveNext())
          {
            if (spaces.Current.Name == "")
            {
              spaceNames[counter] = "Living Space " + (counter + 1).ToString();
            }
            else
            {
              spaceNames[counter] = spaces.Current.Name;
            }
            counter++;
          }

          if (vessel.Spaces.Count > 0)
          {
            newSelectedSpace = GUILayout.SelectionGrid(this.WindowSelectedSpace, spaceNames, 1);
          }

          // If one of the spaces has been selected then display a list of parts that make it up and sort out the highlighting
          if (-1 != newSelectedSpace)
          {
            // Only fiddle with the highlighting if the selected space has actually changed
            if (newSelectedSpace != this.WindowSelectedSpace)
            {
              // First unhighlight the space that was selected.
              if (-1 != this.WindowSelectedSpace && this.WindowSelectedSpace < this.vessel.Spaces.Count)
              {
                vessel.Spaces[this.WindowSelectedSpace].Highlight(false);
              }

              // Update the space that has been selected.
              this.WindowSelectedSpace = newSelectedSpace;

              // Update the text in the Space edit box
              this.spaceNameEditField = vessel.Spaces[this.WindowSelectedSpace].Name;

              // Highlight the new space
              vessel.Spaces[this.WindowSelectedSpace].Highlight(true);
            }

            // Loop through all the parts in the newly selected space and create a list of all the spaces in it.
            var parts = vessel.Spaces[this.WindowSelectedSpace].Parts.GetEnumerator();
            while (parts.MoveNext())
            {
              partsList += (parts.Current.Part).partInfo.title + "\n";
            }

            // Display the text box that allows the space name to be changed
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            this.spaceNameEditField = GUILayout.TextField(this.spaceNameEditField);
            if (GUILayout.Button("Update"))
            {
              vessel.Spaces[this.WindowSelectedSpace].Name = this.spaceNameEditField;
            }
            GUILayout.EndHorizontal();

            this.scrollViewer = GUILayout.BeginScrollView(this.scrollViewer, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            GUILayout.BeginVertical();

            // Display the crew capacity of the space.
            GUILayout.Label("Crew Capacity: " + vessel.Spaces[this.WindowSelectedSpace].MaxCrew);

            // And list the crew names
            String crewList = "Crew Info:\n";

            var crewmembers = vessel.Spaces[this.WindowSelectedSpace].Crew.GetEnumerator();
            while (crewmembers.MoveNext())
            {
              crewList += (crewmembers.Current.Kerbal).name + "\n";
            }
            GUILayout.Label(crewList);

            // Display the list of component parts.
            GUILayout.Label(partsList);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

          }
        }
        else
        {
          GUILayout.Label("No current vessel.");
        }
        GUILayout.EndVertical();
        GUI.DragWindow();
        RepositionWindow(ref windowPosition);
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    private void DisplayOptionWindow(int windowID)
    {
      Rect rect = new Rect(windowOptionsPosition.width - 20, 4, 16, 16);
      if (GUI.Button(rect, ""))
      {
        this.optionsVisible = false;
      }
      GUILayout.BeginVertical();
      // Unrestricted Xfers
      allowUnrestrictedTransfers = GUILayout.Toggle(allowUnrestrictedTransfers, "Allow Crew Unrestricted Transfers");

      // Optional Passable Parts
      enablePassable = GUILayout.Toggle(enablePassable, "Enable Optional Passable Parts\r\n(Requires game restart)");

      // Blizzy Toolbar?
      if (ToolbarManager.ToolbarAvailable)
        GUI.enabled = true;
      else
      {
        GUI.enabled = false;
        enableBlizzyToolbar = false;
      }
      enableBlizzyToolbar = GUILayout.Toggle(enableBlizzyToolbar, "Use Blizzy's Toolbar instead of Stock");

      GUI.enabled = true;
      GUILayout.EndVertical();
      GUI.DragWindow();
      RepositionWindow(ref windowOptionsPosition);
    }

    // Method to ensure that all parts which have a crewcapacity >0 have a CLSModule attached to it.
    private void AddModuleToParts()
    {
      IEnumerable<AvailablePart> parts = PartLoader.LoadedPartsList.Where(p => p.partPrefab != null && p.partPrefab.CrewCapacity > 0);
      foreach (AvailablePart part in parts)
      {
        try
        {
          if (part.name.Equals("kerbalEVA"))
          {
            // Debug.Log("No CLS required for KerbalEVA!");
          }
          else
          {
            Part prefabPart = part.partPrefab;

            //Debug.Log("Adding ConnectedLivingSpace Support to " + part.name + "/" + prefabPart.partInfo.title);

            if (!prefabPart.Modules.Contains("ModuleConnectedLivingSpace"))
            {
              //Debug.Log("The ModuleConnectedLivingSpace is missing!");

              ConfigNode node = new ConfigNode("MODULE");
              node.AddValue("name", "ModuleConnectedLivingSpace");
              {
                // This block is required as calling AddModule and passing in the node throws an exception if Awake has not been called. The method Awaken uses reflection to call then private method Awake. See http://forum.kerbalspaceprogram.com/threads/27851 for more information.
                PartModule pm = prefabPart.AddModule("ModuleConnectedLivingSpace");
                if (Awaken(pm))
                {
                  pm.Load(node);
                }
              }
            }
            else
            {
              // Debug.Log("The ModuleConnectedLivingSpace is already there.");
            }
          }
        }
        catch (Exception ex)
        {
          Debug.LogException(ex);
        }
      }
    }


    // Method to add Docking Hatches to all parts that have Docking Nodes
    private void AddHatchModuleToPartPrefabs()
    {
      IEnumerable<AvailablePart> parts = PartLoader.LoadedPartsList.Where(p => p.partPrefab != null);
      foreach (AvailablePart part in parts)
      {
        Part partPrefab = part.partPrefab;

        // If the part does not have any modules set up then move to the next part
        if (null == partPrefab.Modules)
        {
          continue;
        }

        List<ModuleDockingNode> listDockNodes = new List<ModuleDockingNode>();
        List<ModuleDockingHatch> listDockHatches = new List<ModuleDockingHatch>();

        // Build a temporary list of docking nodes to consider. This is necassery can we can not add hatch modules to the modules list while we are enumerating the very same list!
        foreach (ModuleDockingNode dockNode in partPrefab.Modules.OfType<ModuleDockingNode>())
        {
          listDockNodes.Add(dockNode);
        }

        foreach (ModuleDockingHatch dockHatch in partPrefab.Modules.OfType<ModuleDockingHatch>())
        {
          listDockHatches.Add(dockHatch);
        }

        foreach (ModuleDockingNode dockNode in listDockNodes)
        {
          // Does this docking node have a corresponding hatch?
          ModuleDockingHatch hatch = null;
          foreach (ModuleDockingHatch h in listDockHatches)
          {
            if (h.IsRelatedDockingNode(dockNode))
            {
              hatch = h;
              break;
            }
          }

          if (null == hatch)
          {
            // There is no corresponding hatch - add one.
            ConfigNode node = new ConfigNode("MODULE");
            node.AddValue("name", "ModuleDockingHatch");

            if (dockNode.referenceAttachNode != string.Empty)
            {
              Debug.Log("Adding ModuleDockingHatch to part " + part.title + " and the docking node that uses attachNode " + dockNode.referenceAttachNode);
              node.AddValue("docNodeAttachmentNodeName", dockNode.referenceAttachNode);
            }
            else
            {
              if (dockNode.nodeTransformName != string.Empty)
              {
                Debug.Log("Adding ModuleDockingHatch to part " + part.title + " and the docking node that uses transform " + dockNode.nodeTransformName);
                node.AddValue("docNodeTransformName", dockNode.nodeTransformName);
              }
            }

            {
              // This block is required as calling AddModule and passing in the node throws an exception if Awake has not been called. The method Awaken uses reflection to call then private method Awake. See http://forum.kerbalspaceprogram.com/threads/27851 for more information.
              PartModule pm = partPrefab.AddModule("ModuleDockingHatch");
              if (Awaken(pm))
              {
                Debug.Log("Loading the ModuleDockingHatch config");
                pm.Load(node);
              }
              else
              {
                Debug.LogWarning("Failed to call Awaken so the config has not been loaded.");
              }
            }
          }
        }
      }
    }


    private void CheckAndFixDockingHatches(List<Part> listParts)
    {
      foreach (Part part in listParts)
      {
        // If the part does not have any modules set up then move to the next part
        if (null == part.Modules)
        {
          continue;
        }

        List<ModuleDockingNode> listDockNodes = new List<ModuleDockingNode>();
        List<ModuleDockingHatch> listDockHatches = new List<ModuleDockingHatch>();

        // Build a temporary list of docking nodes to consider. This is necassery can we can not add hatch modules to the modules list while we are enumerating the very same list!
        foreach (ModuleDockingNode dockNode in part.Modules.OfType<ModuleDockingNode>())
        {
          listDockNodes.Add(dockNode);
        }

        foreach (ModuleDockingHatch dockHatch in part.Modules.OfType<ModuleDockingHatch>())
        {
          listDockHatches.Add(dockHatch);
        }

        // First go through all the hatches. If any do not refer to a dockingPort then remove it.
        foreach (ModuleDockingHatch dockHatch in listDockHatches)
        {
          // I know we are making  abit of a meal of this. It is unclear to me what the unset vakues will be, and this way we are catching every possibility. It seems that open (3) is the open that gets called, but I will leave this as is for now.
          if ("" == dockHatch.docNodeAttachmentNodeName && "" == dockHatch.docNodeTransformName)
          {
            Debug.Log("Found a hatch that does not reference a docking node. Removing it from the part.(1)");
            part.RemoveModule(dockHatch);
          }
          else if (string.Empty == dockHatch.docNodeAttachmentNodeName && string.Empty == dockHatch.docNodeTransformName)
          {
            Debug.Log("Found a hatch that does not reference a docking node. Removing it from the part.(2)");
            part.RemoveModule(dockHatch);
          }
          else if (null == dockHatch.docNodeAttachmentNodeName && null == dockHatch.docNodeTransformName)
          {
            Debug.Log("Found a hatch that does not reference a docking node. Removing it from the part.(3)");
            part.RemoveModule(dockHatch);
          }
          else if ((null == dockHatch.docNodeAttachmentNodeName || string.Empty == dockHatch.docNodeAttachmentNodeName || "" == dockHatch.docNodeAttachmentNodeName) && ("" == dockHatch.docNodeTransformName || string.Empty == dockHatch.docNodeTransformName || null == dockHatch.docNodeTransformName))
          {
            Debug.Log("Found a hatch that does not reference a docking node. Removing it from the part.(4)");
            part.RemoveModule(dockHatch);
          }
        }

        // Now because we might have removed for dodgy hatches, rebuild the hatch list.
        listDockHatches.Clear();
        foreach (ModuleDockingHatch dockHatch in part.Modules.OfType<ModuleDockingHatch>())
        {
          listDockHatches.Add(dockHatch);
        }

        // Now go through all the dockingPorts and add hatches for any docking ports that do not have one.
        foreach (ModuleDockingNode dockNode in listDockNodes)
        {
          // Does this docking node have a corresponding hatch?
          ModuleDockingHatch hatch = null;
          foreach (ModuleDockingHatch h in listDockHatches)
          {
            if (h.IsRelatedDockingNode(dockNode))
            {
              hatch = h;
              break;
            }
          }

          if (null == hatch)
          {
            // There is no corresponding hatch - add one.
            ConfigNode node = new ConfigNode("MODULE");
            node.AddValue("name", "ModuleDockingHatch");

            if (dockNode.referenceAttachNode != string.Empty)
            {
              // Debug.Log("Adding ModuleDockingHatch to part " + part.partInfo.title + " and the docking node that uses attachNode " + dockNode.referenceAttachNode);
              node.AddValue("docNodeAttachmentNodeName", dockNode.referenceAttachNode);
            }
            else
            {
              if (dockNode.nodeTransformName != string.Empty)
              {
                // Debug.Log("Adding ModuleDockingHatch to part " + part.partInfo.title + " and the docking node that uses transform " + dockNode.nodeTransformName);
                node.AddValue("docNodeTransformName", dockNode.nodeTransformName);
              }
            }

            {
              // This block is required as calling AddModule and passing in the node throws an exception if Awake has not been called. The method Awaken uses reflection to call then private method Awake. See http://forum.kerbalspaceprogram.com/threads/27851 for more information.
              PartModule pm = part.AddModule("ModuleDockingHatch");
              if (Awaken(pm))
              {
                // Debug.Log("Loading the ModuleDockingHatch config");
                pm.Load(node);
              }
              else
              {
                Debug.LogWarning("Failed to call Awaken so the config has not been loaded.");
              }
            }
          }
        }
      }
    }

    private void CheckAndFixDockingHatchesInEditor()
    {
      if (EditorLogic.RootPart == null)
      {
        return; // If there are no parts then there is nothing to check. 
      }
      else
      {
        this.CheckAndFixDockingHatches(EditorLogic.SortedShipList);
      }
    }

    private void CheckAndFixDockingHatchesInFlight()
    {
      this.CheckAndFixDockingHatches(FlightGlobals.ActiveVessel.Parts);
    }

    // Method to add Docking Hatches to all parts that have Docking Nodes
    private void AddHatchModuleToParts()
    {
      // If we are in the editor or if flight, take a look at the active vesssel and add a ModuleDockingHatch to any part that has a ModuleDockingNode without a corresponding ModuleDockingHatch
      List<Part> listParts;

      if (HighLogic.LoadedSceneIsEditor && null != EditorLogic.RootPart)
      {
        listParts = EditorLogic.SortedShipList;
      }
      else if (HighLogic.LoadedSceneIsFlight && null != FlightGlobals.ActiveVessel && null != FlightGlobals.ActiveVessel.Parts)
      {
        listParts = FlightGlobals.ActiveVessel.Parts;
      }
      else
      {
        listParts = new List<Part>();
      }

      foreach (Part part in listParts)
      {
        try
        {
          // If the part does not have any modules set up then move to the next part
          if (null == part.Modules)
          {
            continue;
          }

          List<ModuleDockingNode> listDockNodes = new List<ModuleDockingNode>();
          List<ModuleDockingHatch> listDockHatches = new List<ModuleDockingHatch>();

          // Build a temporary list of docking nodes to consider. This is necassery can we can not add hatch modules to the modules list while we are enumerating the very same list!
          foreach (ModuleDockingNode dockNode in part.Modules.OfType<ModuleDockingNode>())
          {
            listDockNodes.Add(dockNode);
          }

          foreach (ModuleDockingHatch dockHatch in part.Modules.OfType<ModuleDockingHatch>())
          {
            listDockHatches.Add(dockHatch);
          }

          foreach (ModuleDockingNode dockNode in listDockNodes)
          {
            // Does this docking node have a corresponding hatch?
            ModuleDockingHatch hatch = null;
            foreach (ModuleDockingHatch h in listDockHatches)
            {
              if (h.IsRelatedDockingNode(dockNode))
              {
                hatch = h;
                break;
              }
            }

            if (null == hatch)
            {
              // There is no corresponding hatch - add one.
              ConfigNode node = new ConfigNode("MODULE");
              node.AddValue("name", "ModuleDockingHatch");

              if (dockNode.referenceAttachNode != string.Empty)
              {
                //Debug.Log("Adding ModuleDockingHatch to part " + part.partInfo.title + " and the docking node that uses attachNode " + dockNode.referenceAttachNode);
                node.AddValue("docNodeAttachmentNodeName", dockNode.referenceAttachNode);
              }
              else
              {
                if (dockNode.nodeTransformName != string.Empty)
                {
                  //Debug.Log("Adding ModuleDockingHatch to part " + part.partInfo.title + " and the docking node that uses transform " + dockNode.nodeTransformName);
                  node.AddValue("docNodeTransformName", dockNode.nodeTransformName);
                }
              }

              {
                // This block is required as calling AddModule and passing in the node throws an exception if Awake has not been called. The method Awaken uses reflection to call then private method Awake. See http://forum.kerbalspaceprogram.com/threads/27851 for more information.
                PartModule pm = part.AddModule("ModuleDockingHatch");
                if (Awaken(pm))
                {
                  //Debug.Log("Loading the ModuleDockingHatch config");
                  pm.Load(node);
                }
                else
                {
                  Debug.LogWarning("Failed to call Awaken so the config has not been loaded.");
                }
              }
            }
          }
        }
        catch (Exception ex)
        {
          Debug.LogException(ex);
        }
      }
    }

    //This method uses reflection to call the Awake private method in PartModule. It turns out that Part.AddModule fails if Awake has not been called (which sometimes it has not). See http://forum.kerbalspaceprogram.com/threads/27851 for more info on this.
    public static bool Awaken(PartModule module)
    {
      // thanks to Mu and Kine for help with this bit of Dark Magic. 
      // KINEMORTOBESTMORTOLOLOLOL
      if (module == null)
        return false;
      object[] paramList = new object[] { };
      MethodInfo awakeMethod = typeof(PartModule).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);

      if (awakeMethod == null)
        return false;

      awakeMethod.Invoke(module, paramList);
      return true;
    }

    // Method to optionally abort an attempt to use the stock crew transfer mechanism
    private void OnCrewTransfered(GameEvents.HostedFromToAction<ProtoCrewMember, Part> data)
    {
      try
      {
        // If transfers are not restricted then we have got nothing to do here.
        if (allowUnrestrictedTransfers) return;

        // "Transfers" to/from EVA are always permitted.
        // Trying to step them results in really bad things happening, and would be out of
        // scope for this plugin anyway.
        if (data.from.Modules.Cast<PartModule>().Any(x => x is KerbalEVA) ||
            data.to.Modules.Cast<PartModule>().Any(x => x is KerbalEVA)) return;

        if (null == Instance.Vessel)
        {
          Instance.RebuildCLSVessel();
        }

        ICLSPart clsFrom = Instance.Vessel.Parts.Find(x => x.Part == data.from);
        ICLSPart clsTo = Instance.Vessel.Parts.Find(x => x.Part == data.to);

        if (clsFrom == null || clsTo == null || clsFrom.Space != clsTo.Space)
        {
          // Ok, override is active, so let's remove the old message and revert the move.
          string oldMessage = string.Format("{0} moved to {1}", data.host.name, clsTo.Part.partInfo.title);
          DeleteScreenMessages(oldMessage, "UC");

          data.to.RemoveCrewmember(data.host);
          data.from.AddCrewmember(data.host);

          ScreenMessages.PostScreenMessage(string.Format("<color=orange>{0} is unable to reach {1}.</color>", data.host.name, clsTo.Part.partInfo.title),10f);
        }

        // Whatever happened it seems like a good idea to rebuild the CLS data as the kerbals may now in different places.
        Instance.RebuildCLSVessel();
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    internal bool ActivateBlizzyToolBar()
    {
      if (enableBlizzyToolbar)
      {
        try
        {
          if (ToolbarManager.ToolbarAvailable)
          {
            if (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
              blizzyToolbarButton = ToolbarManager.Instance.add("ConnectedLivingSpace", "ConnectedLivingSpace");
              blizzyToolbarButton.TexturePath = "ConnectedLivingSpace/assets/cls_b_icon_on";
              blizzyToolbarButton.ToolTip = "Connected Living Space";
              blizzyToolbarButton.Visible = true;
              blizzyToolbarButton.OnClick += (e) =>
              {
                OnCLSButtonToggle();
              };
            }
            return true;
          }
          else
          {
            return false;
          }
        }
        catch
        {
          // Blizzy Toolbar instantiation error.  ignore.
          return false;
        }
      }
      else
      {
        // No Blizzy Toolbar
        return false;
      }
    }

    private void ApplySettings()
    {
      if (settings == null)
        loadSettings();
      ConfigNode toolbarNode = settings.HasNode("clsSettings") ? settings.GetNode("clsSettings") : settings.AddNode("clsSettings");
      if (toolbarNode.HasValue("enableBlizzyToolbar"))
        enableBlizzyToolbar = bool.Parse(toolbarNode.GetValue("enableBlizzyToolbar"));
      windowPosition = getRectangle(toolbarNode, "windowPosition", windowPosition);
      windowOptionsPosition = getRectangle(toolbarNode, "windowOptionsPosition", windowOptionsPosition);
      enableBlizzyToolbar = toolbarNode.HasValue("enableBlizzyToolbar") ? bool.Parse(toolbarNode.GetValue("enableBlizzyToolbar")) : enableBlizzyToolbar;
      enablePassable = toolbarNode.HasValue("enablePassable") ? bool.Parse(toolbarNode.GetValue("enablePassable")) : enablePassable;
    }

    private ConfigNode loadSettings()
    {
      if (settings == null)
        settings = ConfigNode.Load(SETTINGS_FILE) ?? new ConfigNode();
      return settings;
    }

    private void saveSettings()
    {
      if (settings == null)
        settings = loadSettings();
      ConfigNode toolbarNode = settings.HasNode("clsSettings") ? settings.GetNode("clsSettings") : settings.AddNode("clsSettings");
      if (toolbarNode.HasValue("enableBlizzyToolbar"))
        toolbarNode.RemoveValue("enableBlizzyToolbar");
      toolbarNode.AddValue("enableBlizzyToolbar", enableBlizzyToolbar.ToString());
      WriteRectangle(toolbarNode, "windowPosition", windowPosition);
      WriteRectangle(toolbarNode, "windowOptionsPosition", windowOptionsPosition);
      WriteValue(toolbarNode, "enableBlizzyToolbar", enableBlizzyToolbar);
      WriteValue(toolbarNode, "enablePassable", enablePassable);
      settings.Save(SETTINGS_FILE);
    }

    private static Rect getRectangle(ConfigNode WindowsNode, string RectName, Rect defaultvalue)
    {
      Rect thisRect = new Rect();
      ConfigNode RectNode = WindowsNode.HasNode(RectName) ? WindowsNode.GetNode(RectName) : WindowsNode.AddNode(RectName);
      thisRect.x = RectNode.HasValue("x") ? int.Parse(RectNode.GetValue("x")) : defaultvalue.x;
      thisRect.y = RectNode.HasValue("y") ? int.Parse(RectNode.GetValue("y")) : defaultvalue.y;
      thisRect.width = RectNode.HasValue("width") ? int.Parse(RectNode.GetValue("width")) : defaultvalue.width;
      thisRect.height = RectNode.HasValue("height") ? int.Parse(RectNode.GetValue("height")) : defaultvalue.height;

      return thisRect;
    }

    private static void WriteRectangle(ConfigNode WindowsNode, string RectName, Rect rectValue)
    {
      ConfigNode RectNode = WindowsNode.HasNode(RectName) ? WindowsNode.GetNode(RectName) : WindowsNode.AddNode(RectName);
      WriteValue(RectNode, "x", (int)rectValue.x);
      WriteValue(RectNode, "y", (int)rectValue.y);
      WriteValue(RectNode, "width", (int)rectValue.width);
      WriteValue(RectNode, "height", (int)rectValue.height);
    }

    private static void WriteValue(ConfigNode configNode, string ValueName, object value)
    {
      if (configNode.HasValue(ValueName))
        configNode.RemoveValue(ValueName);
      configNode.AddValue(ValueName, value.ToString());
    }

    internal void RepositionWindow(ref Rect windowPosition)
    {
      if (windowPosition.x < 0) windowPosition.x = 0;
      if (windowPosition.y < 0) windowPosition.y = 0;
      if (windowPosition.xMax > Screen.currentResolution.width)
        windowPosition.x = Screen.currentResolution.width - windowPosition.width;
      if (windowPosition.yMax > Screen.currentResolution.height)
        windowPosition.y = Screen.currentResolution.height - windowPosition.height;
    }
    /// <summary>
    ///Will delete Screen Messages. If you pass in messagetext it will only delete messages that contain that text string.
    ///If you pass in a messagearea it will only delete messages in that area. Values are: UC,UL,UR,LC,ALL
    /// </summary>
    /// <param name="messagetext">Specify a string that is part of a message that you want to remove, or pass in empty string to delete all messages</param>
    /// <param name="messagearea">Specify a string representing the message area of the screen that you want messages removed from, 
    /// or pass in "ALL" string to delete from all message areas. 
    /// messagearea accepts the values of "UC" - UpperCenter, "UL" - UpperLeft, "UR" - UpperRight, "LC" - LowerCenter, "ALL" - All Message Areas</param>
    internal static void DeleteScreenMessages(string messagetext, string messagearea)
    {
      //Get the ScreenMessages Instance
      var messages = ScreenMessages.Instance;
      List<ScreenMessagesText> messagetexts = new List<ScreenMessagesText>();
      //Get the message Area messages based on the value of messagearea parameter.
      switch (messagearea)
      {
        case "UC":
          messagetexts = messages.upperCenter.gameObject.GetComponentsInChildren<ScreenMessagesText>().ToList();
          break;
        case "UL":
          messagetexts = messages.upperLeft.gameObject.GetComponentsInChildren<ScreenMessagesText>().ToList();
          break;
        case "UR":
          messagetexts = messages.upperRight.gameObject.GetComponentsInChildren<ScreenMessagesText>().ToList();
          break;
        case "LC":
          messagetexts = messages.lowerCenter.gameObject.GetComponentsInChildren<ScreenMessagesText>().ToList();
          break;
        case "ALL":
          messagetexts = messages.gameObject.GetComponentsInChildren<ScreenMessagesText>().ToList();
          break;
      }
      //Loop through all the mesages we found.
      var list = messagetexts.GetEnumerator();
      while  (list.MoveNext())
      {
        //If the user specified text to search for only delete messages that contain that text.
        if (messagetext != "")
        {
          if (list.Current != null && list.Current.text.text.Contains(messagetext))
          {
            UnityEngine.Object.Destroy(list.Current.gameObject);
          }
        }
        else  //If the user did not specific a message text to search for we DELETE ALL messages!!
        {
          UnityEngine.Object.Destroy(list.Current.gameObject);
        }
      }
    }
    #endregion Support/action methods
  }
}
