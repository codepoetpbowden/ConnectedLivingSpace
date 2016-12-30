using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSP.UI.Screens;
using KSP.UI.Screens.Flight.Dialogs;
using UnityEngine;

namespace ConnectedLivingSpace
{ 
  [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
  public class CLSAddon : MonoBehaviour, ICLSAddon
  {
    #region static Properties

    internal static bool WindowVisable;
    private static Rect windowPosition = new Rect(0, 0, 360, 480);
    private static Rect windowOptionsPosition = new Rect(0, 0, 0, 0);
    private static GUIStyle windowStyle;
    internal static bool allowUnrestrictedTransfers;
    // this value is used to "remember" the actual setting in CLS in the event it was changed by another mod
    public static bool backupAllowUnrestrictedTransfers;
    public static bool enableBlizzyToolbar;
    public static bool enablePassable;
    private static bool prevEnableBlizzyToolbar;
    private static string SettingsPath;
    private static string SettingsFile;

    // this var is now restricted to use by the CLS window.  Highlighting will be handled by part.
    internal static int WindowSelectedSpace = -1;

    private static ApplicationLauncherButton stockToolbarButton; // Stock Toolbar Button
    internal static IButton blizzyToolbarButton; // Blizzy Toolbar Button

    public static CLSAddon Instance
    {
      get;
      private set;
    }

    #endregion static Properties

    #region Instanced Properties

    private bool optionsVisible;

    private ConfigNode settings;
    private Vector2 scrollViewer = Vector2.zero;
    internal CLSVessel vessel;

    // State var used by OnEditorShipModified event handler to note changes to vessel for reconstruction of spaces.
    private int editorPartCount;

    private string spaceNameEditField;

    public ICLSVessel Vessel
    {
      get
      {
        return vessel;
      }
    }

    public bool AllowUnrestrictedTransfers
    {
      get
      {
        return allowUnrestrictedTransfers;
      }
      set { allowUnrestrictedTransfers = value; }
    }

    #endregion Instanced Properties

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
      if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
      {
        if (enableBlizzyToolbar)
        {
          // Let't try to use Blizzy's toolbar
          //Debug.Log("CLSAddon.Awake - Blizzy Toolbar Selected.");
          if (ActivateBlizzyToolBar()) return;
          // We failed to activate the toolbar, so revert to stock
          //Debug.Log("CLSAddon.Awake - Stock Toolbar Selected.");
          GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
        }
        else
        {
          // Use stock Toolbar
          //Debug.Log("CLSAddon.Awake - Stock Toolbar Selected.");
          GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
        }
      }
    }

    public void Start()
    {
      // Debug.Log("CLSAddon:Start");
      SettingsPath = string.Format("{0}GameData/ConnectedLivingSpace/Plugins/PluginData", KSPUtil.ApplicationRootPath);
      SettingsFile = string.Format("{0}/cls_settings.dat", SettingsPath);


      windowStyle = new GUIStyle(HighLogic.Skin.window);

      // load toolbar selection setting
      ApplySettings();

      if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
      {
        GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);
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
        GameEvents.onEditorShipModified.Add(OnEditorShipModified);

        GameEvents.onItemTransferStarted.Add(OnItemTransferStarted);
        GameEvents.onCrewTransferPartListCreated.Add(OnCrewTransferPartListCreated);
        GameEvents.onCrewTransferSelected.Add(OnCrewTransferSelected);
        //GameEvents.onCrewTransferred.Add(OnCrewTransfered);
      }

      // Add the CLSModule to all parts that can house crew (and do not already have it).
      if (HighLogic.LoadedScene == GameScenes.LOADING)
      {
        AddModuleToParts();

        // Add hatches to all the docking ports (prefabs)
        //AddHatchModuleToPartPrefabs();
      }
    }

    public void Update()
    {
      // Debug.Log("CLSAddon:Update");
      if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
        CheckForToolbarTypeToggle();
    }

    public void FixedUpdate()
    {
    }

    //private void ReconcileHatches()
    //{
    //  try
    //  {
    //    //Debug.Log("CLSAddon:ReconcileHatches");

    //    // Although hatches have been added to the docking port prefabs, for some reason that is not fully understood 
    //    // when the prefab is used to instantiate an actual part the hatch module has not been properly setup. 
    //    // This is not a problem where the craft is being loaded, as the act of loading it will overwrite all the persisted KSPFields with the saved values. 
    //    // However in the VAB/SPH we end up with a ModuleDockingHatch that has not its docNodeTransformName or docNodeAttahcmentNodeName set properly. 
    //    // The solution is to check for this state in the editor, and patch it up. In flight the part will get loaded so it is not an issue.
    //    if (HighLogic.LoadedSceneIsEditor)
    //    {
    //      CheckAndFixDockingHatchesInEditor();
    //    }

    //    // It seems that there are sometimes problems with hatches that do not refer to dockingports in flight too, so check this in flight. 
    //    //It would be good to find a way of making this less expensive.
    //    if (!HighLogic.LoadedSceneIsFlight) return;
    //    if (FlightGlobals.ready)
    //    {
    //      CheckAndFixDockingHatchesInFlight();
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    Debug.LogException(ex);
    //  }
    //}

    public void OnDestroy()
    {
      //Debug.Log("CLSAddon::OnDestroy");

      allowUnrestrictedTransfers = backupAllowUnrestrictedTransfers;
      saveSettings();

      GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);
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
      GameEvents.onEditorShipModified.Remove(OnEditorShipModified);

      // Remove the stock toolbar button
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
      if (stockToolbarButton != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
      }

      GameEvents.onItemTransferStarted.Remove(OnItemTransferStarted);
      GameEvents.onCrewTransferPartListCreated.Remove(OnCrewTransferPartListCreated);
      GameEvents.onCrewTransferSelected.Remove(OnCrewTransferSelected);
      //GameEvents.onCrewTransferred.Remove(OnCrewTransfered);
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
          GameDatabase.Instance.GetTexture("ConnectedLivingSpace/assets/cls_icon_off", false));
    }

    void OnGUIAppLauncherDestroyed()
    {
      if (stockToolbarButton == null) return;
      ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
      stockToolbarButton = null;
    }

    void DummyVoid() { }

    private void OnFlightReady()
    {
      //Debug.Log("CLSAddon::OnFlightReady");          

      // Now scan the vessel
      //Debug.Log("Calling RebuildCLSVessel from onFlightReady");
      RebuildCLSVessel();
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

    // This event is fired when the vessel is changed. If this happens we need to throw away all of our thoughts about the previous vessel, and analyse the new one.
    private void OnVesselChange(Vessel data)
    {
      //Debug.Log("CLSAddon::OnVesselChange");

      //Debug.Log("Calling RebuildCLSVessel from OnVesselChange");
      RebuildCLSVessel(data);
    }

    private void OnEditorShipModified(ShipConstruct vesselConstruct)
    {
        if (vesselConstruct.Parts.Count == editorPartCount) return;
      //Debug.Log("Calling RebuildCLSVessel as the part count has changed in the editor");

      RebuildCLSVessel();
      editorPartCount = vesselConstruct.Parts.Count;
      // First unhighlight the space that was selected.
      if (-1 != WindowSelectedSpace && WindowSelectedSpace < vessel.Spaces.Count)
      {
        vessel.Spaces[WindowSelectedSpace].Highlight(true);
      }
    }

    private void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> sceneData)
    {
      if (WindowVisable) OnCLSButtonToggle();
    }

    internal void OnCLSButtonToggle()
    {
      //Debug.Log("CLSAddon::OnCLSButtonToggle");
      WindowVisable = !WindowVisable;

      if (!WindowVisable && null != vessel)
        vessel.Highlight(false);

      if (enableBlizzyToolbar)
        blizzyToolbarButton.TexturePath = WindowVisable ? "ConnectedLivingSpace/assets/cls_b_icon_on" : "ConnectedLivingSpace/assets/cls_b_icon_off";
      else
        stockToolbarButton.SetTexture(GameDatabase.Instance.GetTexture(WindowVisable ? "ConnectedLivingSpace/assets/cls_icon_on" : "ConnectedLivingSpace/assets/cls_icon_off", false));
    }

    private void OnGUI()
    {
      if (WindowVisable)
      {
        //Set the GUI Skin
        //GUI.skin = HighLogic.Skin;

        windowPosition = GUILayout.Window(947695, windowPosition, OnWindow, "Connected Living Space", windowStyle, GUILayout.MinHeight(20), GUILayout.ExpandHeight(true));
        if (!optionsVisible) return;
        if (windowOptionsPosition == new Rect(0, 0, 0, 0))
          windowOptionsPosition = new Rect(windowPosition.x + windowPosition.width + 10, windowPosition.y, 260, 115);
        windowOptionsPosition = GUILayout.Window(947696, windowOptionsPosition, DisplayOptionWindow, "Options", windowStyle, GUILayout.MinHeight(20), GUILayout.ExpandHeight(true));
      }
      else
      {
        if (WindowSelectedSpace <= -1) return;
        vessel.Spaces[WindowSelectedSpace].Highlight(false);
        WindowSelectedSpace = -1;
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
          if (null != vessel)
          {
            vessel.Clear();
          }
          vessel = null;
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
      if (newVessel?.rootPart == null) return;
      RebuildCLSVessel(newVessel.rootPart);
    }

    private void RebuildCLSVessel(Part newRootPart)
    {
      try
      {
        //Debug.Log("RebuildCLSVessel");
        if (null != vessel)
        {
          // Tidy up the old vessel information
          vessel.Clear();
          vessel = null;
        }

        // Build new vessel information
        vessel = new CLSVessel();
        vessel.Populate(newRootPart);
        if (!WindowVisable || WindowSelectedSpace <= -1) return;
        vessel.Highlight(false);
        vessel.Spaces[CLSAddon.WindowSelectedSpace].Highlight(true);
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
          optionsVisible = !optionsVisible;
        }
        GUILayout.BeginVertical();
        GUI.enabled = true;

        // Build a string descibing the contents of each of the spaces.
        if (null != vessel)
        {

          string[] spaceNames = new string[vessel.Spaces.Count];
          int counter = 0;
          int newSelectedSpace = -1;

          string partsList = "";
          List<ICLSSpace>.Enumerator spaces = vessel.Spaces.GetEnumerator();
          while (spaces.MoveNext())
          {
            if (spaces.Current == null) continue;
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
            newSelectedSpace = GUILayout.SelectionGrid(WindowSelectedSpace, spaceNames, 1);
          }

          // If one of the spaces has been selected then display a list of parts that make it up and sort out the highlighting
          if (-1 != newSelectedSpace)
          {
            // Only fiddle with the highlighting if the selected space has actually changed
            if (newSelectedSpace != WindowSelectedSpace)
            {
              // First unhighlight the space that was selected.
              if (-1 != WindowSelectedSpace && WindowSelectedSpace < vessel.Spaces.Count)
              {
                vessel.Spaces[WindowSelectedSpace].Highlight(false);
              }

              // Update the space that has been selected.
              WindowSelectedSpace = newSelectedSpace;

              // Update the text in the Space edit box
              spaceNameEditField = vessel.Spaces[WindowSelectedSpace].Name;

              // Highlight the new space
              vessel.Spaces[WindowSelectedSpace].Highlight(true);
            }

            // Loop through all the parts in the newly selected space and create a list of all the spaces in it.
            List<ICLSPart>.Enumerator parts = vessel.Spaces[WindowSelectedSpace].Parts.GetEnumerator();
            while (parts.MoveNext())
            {
              if (parts.Current == null) continue;
              partsList += (parts.Current.Part).partInfo.title + "\n";
            }

            // Display the text box that allows the space name to be changed
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            spaceNameEditField = GUILayout.TextField(spaceNameEditField);
            if (GUILayout.Button("Update"))
            {
              vessel.Spaces[WindowSelectedSpace].Name = spaceNameEditField;
            }
            GUILayout.EndHorizontal();

            scrollViewer = GUILayout.BeginScrollView(scrollViewer, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            GUILayout.BeginVertical();

            // Display the crew capacity of the space.
            GUILayout.Label("Crew Capacity: " + vessel.Spaces[WindowSelectedSpace].MaxCrew);

            // And list the crew names
            string crewList = "Crew Info:\n";

            List<ICLSKerbal>.Enumerator crewmembers = vessel.Spaces[WindowSelectedSpace].Crew.GetEnumerator();
            while (crewmembers.MoveNext())
            {
              if (crewmembers.Current == null) continue;
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
        optionsVisible = false;
      }
      GUILayout.BeginVertical();
      // Unrestricted Xfers
      bool oldAllow = allowUnrestrictedTransfers;
      allowUnrestrictedTransfers = GUILayout.Toggle(allowUnrestrictedTransfers, "Allow Crew Unrestricted Transfers");
      if (oldAllow != allowUnrestrictedTransfers)
      {
        backupAllowUnrestrictedTransfers = allowUnrestrictedTransfers;
      }
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
      IEnumerator<AvailablePart> parts = PartLoader.LoadedPartsList.Where(p => p.partPrefab != null && p.partPrefab.CrewCapacity > 0).GetEnumerator();
      while (parts.MoveNext())
      {
        if (parts.Current == null) continue;
        try
        {
          if (parts.Current.name.Contains("kerbalEVA"))
          {
            // Debug.Log("No CLS required for KerbalEVA!");
          }
          else
          {
            Part prefabPart = parts.Current.partPrefab;

            //Debug.Log("Adding ConnectedLivingSpace Support to " + part.name + "/" + prefabPart.partInfo.title);

            if (!parts.Current.partPrefab.Modules.Contains("ModuleConnectedLivingSpace"))
            {
              //Debug.Log("The ModuleConnectedLivingSpace is missing!");

              ConfigNode node = new ConfigNode("MODULE");
              node.AddValue("name", "ModuleConnectedLivingSpace");
              {
                // This block is required as calling AddModule and passing in the node throws an exception if Awake has not been called. 
                PartModule pm = parts.Current.partPrefab.AddModule("ModuleConnectedLivingSpace");
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
    //private void AddHatchModuleToPartPrefabs()
    //{
    //  IEnumerator<AvailablePart> parts = PartLoader.LoadedPartsList.Where(p => p.partPrefab != null).GetEnumerator();
    //  while (parts.MoveNext())
    //  {
    //    if (parts.Current == null) continue;
    //    Part partPrefab = parts.Current.partPrefab;

    //    // If the part does not have any modules set up then move to the next part
    //    if (null == partPrefab.Modules)
    //    {
    //      continue;
    //    }

    //    List<ModuleDockingNode> listDockNodes = new List<ModuleDockingNode>();
    //    List<ModuleDockingHatch> listDockHatches = new List<ModuleDockingHatch>();

    //    // Build a temporary list of docking nodes to consider. This is necassery can we can not add hatch modules to the modules list while we are enumerating the very same list!
    //    IEnumerator<ModuleDockingNode> dockNodes = partPrefab.Modules.OfType<ModuleDockingNode>().GetEnumerator();
    //    while (dockNodes.MoveNext())
    //    {
    //      if (dockNodes.Current == null) continue;
    //      listDockNodes.Add(dockNodes.Current);
    //    }

    //    IEnumerator<ModuleDockingHatch> dockHatches = partPrefab.Modules.OfType<ModuleDockingHatch>().GetEnumerator();
    //    while (dockHatches.MoveNext())
    //    {
    //      if (dockHatches.Current == null) continue;
    //      listDockHatches.Add(dockHatches.Current);
    //    }

    //    IEnumerator<ModuleDockingNode> nodeList = listDockNodes.GetEnumerator();
    //    while (nodeList.MoveNext())
    //    {
    //      if (nodeList.Current == null) continue;
    //      // Does this docking node have a corresponding hatch?
    //      ModuleDockingHatch hatch = null;
    //      IEnumerator<ModuleDockingHatch> hatchList = listDockHatches.GetEnumerator();
    //      while (hatchList.MoveNext())
    //      {
    //        if (hatchList.Current == null) continue;
    //        if (!hatchList.Current.IsRelatedDockingNode(nodeList.Current)) continue;
    //        hatch = hatchList.Current;
    //        break;
    //      }

    //      if (null != hatch) continue;
    //      // There is no corresponding hatch - add one.
    //      ConfigNode node = new ConfigNode("MODULE");
    //      node.AddValue("name", "ModuleDockingHatch");

    //      if (nodeList.Current.referenceNode.id != string.Empty)
    //      {
    //        Debug.Log("Adding ModuleDockingHatch to part " + parts.Current.title +
    //                  " and the docking node that uses attachNode " + nodeList.Current.referenceNode.id);
    //        node.AddValue("docNodeAttachmentNodeName", nodeList.Current.referenceNode.id);
    //      }
    //      else
    //      {
    //        if (nodeList.Current.nodeTransformName != string.Empty)
    //        {
    //          Debug.Log("Adding ModuleDockingHatch to part " + parts.Current.title +
    //                    " and the docking node that uses transform " + nodeList.Current.nodeTransformName);
    //          node.AddValue("docNodeTransformName", nodeList.Current.nodeTransformName);
    //        }
    //      }
    //      // This block is required as calling AddModule and passing in the node throws an exception if Awake has not been called. The method Awaken uses reflection to call then private method Awake. See http://forum.kerbalspaceprogram.com/threads/27851 for more information.
    //      PartModule pm = partPrefab.AddModule("ModuleDockingHatch");
    //      if (Awaken(pm))
    //      {
    //        Debug.Log("Loading the ModuleDockingHatch config");
    //        pm.Load(node);
    //      }
    //      else
    //      {
    //        Debug.LogWarning("Failed to call Awaken so the config has not been loaded.");
    //      }
    //    }
    //  }
    //}


    //private void CheckAndFixDockingHatches(List<Part> listParts)
    //{
    //  IEnumerator<Part> parts = listParts.GetEnumerator();
    //  while (parts.MoveNext())
    //  {
    //    if (parts.Current == null) continue;
    //    // If the part does not have any modules set up then move to the next part
    //    if (null == parts.Current.Modules) continue;

    //    List<ModuleDockingNode> listDockNodes = new List<ModuleDockingNode>();
    //    List<ModuleDockingHatch> listDockHatches = new List<ModuleDockingHatch>();

    //    // Build a temporary list of docking nodes to consider. This is necessary can we can not add hatch modules to the modules list while we are enumerating the very same list!
    //    IEnumerator<ModuleDockingNode> edN = parts.Current.Modules.OfType<ModuleDockingNode>().GetEnumerator();
    //    while (edN.MoveNext())
    //    {
    //      if (edN.Current == null) continue;
    //      listDockNodes.Add(edN.Current);
    //    }

    //    IEnumerator<ModuleDockingHatch> edH = parts.Current.Modules.OfType<ModuleDockingHatch>().GetEnumerator();
    //    while (edH.MoveNext())
    //    {
    //      if (edH.Current == null) continue;
    //      listDockHatches.Add(edH.Current);
    //    }

    //    // First go through all the hatches. If any do not refer to a dockingPort then remove it.
    //    IEnumerator<ModuleDockingHatch> eLDH = listDockHatches.GetEnumerator();
    //    while (eLDH.MoveNext())
    //    {
    //      if (eLDH.Current == null) continue;
    //      if (string.IsNullOrEmpty(eLDH.Current.docNodeAttachmentNodeName) && string.IsNullOrEmpty(eLDH.Current.docNodeTransformName))
    //      {
    //        Debug.Log("Found a hatch that does not reference a docking node. Removing it from the part.");
    //        parts.Current.RemoveModule(eLDH.Current);
    //      }
    //    }

    //    // Now because we might have removed for dodgy hatches, rebuild the hatch list.
    //    listDockHatches.Clear();
    //    IEnumerator<ModuleDockingHatch> eMDH = parts.Current.Modules.OfType<ModuleDockingHatch>().GetEnumerator();
    //    while (eMDH.MoveNext())
    //    {
    //      if (eMDH.Current == null) continue;
    //      listDockHatches.Add(eMDH.Current);
    //    }

    //    // Now go through all the dockingPorts and add hatches for any docking ports that do not have one.
    //    IEnumerator<ModuleDockingNode> eldn = listDockNodes.GetEnumerator();
    //    while (eldn.MoveNext())
    //    {
    //      if (eldn.Current == null) continue;
    //      // Does this docking node have a corresponding hatch?
    //      ModuleDockingHatch hatch = null;
    //      IEnumerator<ModuleDockingHatch> eldh = listDockHatches.GetEnumerator();
    //      while (eldh.MoveNext())
    //      {
    //        if (eldh.Current == null) continue;
    //        if (!eldh.Current.IsRelatedDockingNode(eldn.Current)) continue;
    //        hatch = eldh.Current;
    //        break;
    //      }

    //      if (null != hatch) continue;
    //      // There is no corresponding hatch - add one.
    //      ConfigNode node = new ConfigNode("MODULE");
    //      node.AddValue("name", "ModuleDockingHatch");

    //      if (eldn.Current.referenceNode.id != string.Empty)
    //      {
    //        // Debug.Log("Adding ModuleDockingHatch to part " + part.partInfo.title + " and the docking node that uses attachNode " + dockNode.referenceNode.id);
    //        node.AddValue("docNodeAttachmentNodeName", eldn.Current.referenceNode.id);
    //      }
    //      else
    //      {
    //        if (eldn.Current.nodeTransformName != string.Empty)
    //        {
    //          // Debug.Log("Adding ModuleDockingHatch to part " + part.partInfo.title + " and the docking node that uses transform " + dockNode.nodeTransformName);
    //          node.AddValue("docNodeTransformName", eldn.Current.nodeTransformName);
    //        }
    //      }
    //      // This block is required as calling AddModule and passing in the node throws an exception if Awake has not been called. The method Awaken uses reflection to call then private method Awake. See http://forum.kerbalspaceprogram.com/threads/27851 for more information.
    //      PartModule pm = parts.Current.AddModule("ModuleDockingHatch");
    //      if (Awaken(pm))
    //      {
    //        // Debug.Log("Loading the ModuleDockingHatch config");
    //        pm.Load(node);
    //      }
    //      else
    //      {
    //        Debug.LogWarning("Failed to call Awaken so the config has not been loaded.");
    //      }
    //    }
    //  }
    //}

    //private void CheckAndFixDockingHatchesInEditor()
    //{
    //  if (EditorLogic.RootPart == null)
    //  {
    //    return; // If there are no parts then there is nothing to check. 
    //  }
    //  CheckAndFixDockingHatches(EditorLogic.SortedShipList);
    //}

    //private void CheckAndFixDockingHatchesInFlight()
    //{
    //  CheckAndFixDockingHatches(FlightGlobals.ActiveVessel.Parts);
    //}

    //// Method to add Docking Hatches to all parts that have Docking Nodes
    //private void AddHatchModuleToParts()
    //{
    //  // If we are in the editor or if flight, take a look at the active vesssel and add a ModuleDockingHatch to any part that has a ModuleDockingNode without a corresponding ModuleDockingHatch
    //  List<Part> listParts;

    //  if (HighLogic.LoadedSceneIsEditor && null != EditorLogic.RootPart)
    //  {
    //    listParts = EditorLogic.SortedShipList;
    //  }
    //  else if (HighLogic.LoadedSceneIsFlight && null != FlightGlobals.ActiveVessel && null != FlightGlobals.ActiveVessel.Parts)
    //  {
    //    listParts = FlightGlobals.ActiveVessel.Parts;
    //  }
    //  else
    //  {
    //    listParts = new List<Part>();
    //  }

    //  IEnumerator<Part> elP = listParts.GetEnumerator();
    //  while (elP.MoveNext())
    //  {
    //    if (elP.Current == null) continue;
    //    try
    //    {
    //      // If the part does not have any modules set up then move to the next part
    //      if (null == elP.Current.Modules) continue;

    //      List<ModuleDockingNode> listDockNodes = new List<ModuleDockingNode>();
    //      List<ModuleDockingHatch> listDockHatches = new List<ModuleDockingHatch>();

    //      // Build a temporary list of docking nodes to consider. This is necassery can we can not add hatch modules to the modules list while we are enumerating the very same list!
    //      IEnumerator<ModuleDockingNode> edn = elP.Current.Modules.OfType<ModuleDockingNode>().GetEnumerator();
    //      while (edn.MoveNext())
    //      {
    //        if (edn.Current == null) continue;
    //        listDockNodes.Add(edn.Current);
    //      }

    //      IEnumerator<ModuleDockingHatch> edh = elP.Current.Modules.OfType<ModuleDockingHatch>().GetEnumerator();
    //      while (edh.MoveNext())
    //      {
    //        if (edh.Current == null) continue;
    //        listDockHatches.Add(edh.Current);
    //      }

    //      IEnumerator<ModuleDockingNode> eldn = listDockNodes.GetEnumerator();
    //      while (eldn.MoveNext())
    //      {
    //        if (eldn.Current == null) continue;
    //        // Does this docking node have a corresponding hatch?
    //        ModuleDockingHatch hatch = null;
    //        IEnumerator<ModuleDockingHatch> eldh = listDockHatches.GetEnumerator();
    //        while (eldh.MoveNext())
    //        {
    //          if (eldh.Current == null) continue;
    //          if (!eldh.Current.IsRelatedDockingNode(eldn.Current)) continue;
    //          hatch = eldh.Current;
    //          break;
    //        }

    //        if (null != hatch) continue;
    //        // There is no corresponding hatch - add one.
    //        ConfigNode node = new ConfigNode("MODULE");
    //        node.AddValue("name", "ModuleDockingHatch");

    //        if (eldn.Current.referenceNode.id != string.Empty)
    //        {
    //          //Debug.Log("Adding ModuleDockingHatch to part " + part.partInfo.title + " and the docking node that uses attachNode " + dockNode.referenceNode.id);
    //          node.AddValue("docNodeAttachmentNodeName", eldn.Current.referenceNode.id);
    //        }
    //        else
    //        {
    //          if (eldn.Current.nodeTransformName != string.Empty)
    //          {
    //            //Debug.Log("Adding ModuleDockingHatch to part " + part.partInfo.title + " and the docking node that uses transform " + dockNode.nodeTransformName);
    //            node.AddValue("docNodeTransformName", eldn.Current.nodeTransformName);
    //          }
    //        }

    //        {
    //          // This block is required as calling AddModule and passing in the node throws an exception if Awake has not been called. The method Awaken uses reflection to call then private method Awake. See http://forum.kerbalspaceprogram.com/threads/27851 for more information.
    //          PartModule pm = elP.Current.AddModule("ModuleDockingHatch");
    //          if (Awaken(pm))
    //          {
    //            //Debug.Log("Loading the ModuleDockingHatch config");
    //            pm.Load(node);
    //          }
    //          else
    //          {
    //            Debug.LogWarning("Failed to call Awaken so the config has not been loaded.");
    //          }
    //        }
    //      }
    //    }
    //    catch (Exception ex)
    //    {
    //      Debug.LogException(ex);
    //    }
    //  }
    //}

    public static bool Awaken(PartModule module)
    {
      if (module == null) return false;
      module.Awake();
      return true;
    }

    private void OnCrewTransferPartListCreated(GameEvents.HostedFromToAction<Part, List<Part>> eventData)
    {
      if (allowUnrestrictedTransfers) return;

      // How can I tell if the parts are in the same space?... I need a starting point!  What part initiated the event?
      Part sourcePart = null;

      // Get the Dialog and find the source part.
      CrewHatchDialog dialog = Resources.FindObjectsOfTypeAll<CrewHatchDialog>().FirstOrDefault();
      if (dialog?.Part == null) return;
      sourcePart = dialog.Part;
      ICLSPart clsFrom = Instance.Vessel.Parts.Find(x => x.Part == sourcePart);

      //Let's manhandle the lists
      List<Part> fullList = new List<Part>();
      List<Part>.Enumerator fromList = eventData.from.GetEnumerator();
      while (fromList.MoveNext())
      {
        if (fromList.Current == null) continue;
        ICLSPart clsTo = Instance.Vessel.Parts.Find(x => x.Part == fromList.Current);

        // If in same space, ignore
        if (clsFrom != null && clsTo != null && clsFrom.Space == clsTo.Space) continue;
        fullList.Add(fromList.Current);
      }
      if (fullList.Count <= 0) return;
      //CrewTransfer.fullMessage = "<color=orange>CLS - This module is either full or internally unreachable.</color>";
      List<Part>.Enumerator removeList = fullList.GetEnumerator();
      while (removeList.MoveNext())
      {
        eventData.from.Remove(removeList.Current);
      }
      eventData.to.AddRange(fullList);
    }

    internal void OnItemTransferStarted(PartItemTransfer xferPartItem)
    {
      if (!allowUnrestrictedTransfers && xferPartItem.type == "Crew")
        xferPartItem.semiValidMessage = "<color=orange>CLS - This module is either full or internally unreachable (different spaces).</color>";
    }

    // Method to optionally abort an attempt to use the stock crew transfer mechanism
    private void OnCrewTransferSelected(CrewTransfer.CrewTransferData crewTransferData)
    {
      // If transfers are not restricted then we have got nothing to do here.
      if (allowUnrestrictedTransfers) return;
      ICLSPart clsFrom = Instance.Vessel.Parts.Find(x => x.Part == crewTransferData.sourcePart);
      ICLSPart clsTo = Instance.Vessel.Parts.Find(x => x.Part == crewTransferData.destPart);

      // If in same space, ignore
      if (clsFrom != null && clsTo != null && clsFrom.Space == clsTo.Space) return;
      
      // Okay, houston, we have a problem.   Prevent transfer.
      crewTransferData.canTransfer = false;
      ScreenMessages.PostScreenMessage(
        string.Format(
          "<color=orange>CLS has prevented {0} from moving.   {1} and {2} are not in the same living space.</color>",
          crewTransferData.crewMember.name, crewTransferData.sourcePart.partInfo.title, crewTransferData.destPart.partInfo.title), 10f);
    }

    internal bool ActivateBlizzyToolBar()
    {
      if (!enableBlizzyToolbar) return false;
      try
      {
        if (!ToolbarManager.ToolbarAvailable) return false;
        if (HighLogic.LoadedScene != GameScenes.EDITOR && HighLogic.LoadedScene != GameScenes.FLIGHT) return true;
        blizzyToolbarButton = ToolbarManager.Instance.add("ConnectedLivingSpace", "ConnectedLivingSpace");
        blizzyToolbarButton.TexturePath = "ConnectedLivingSpace/assets/cls_b_icon_on";
        blizzyToolbarButton.ToolTip = "Connected Living Space";
        blizzyToolbarButton.Visible = true;
        blizzyToolbarButton.OnClick += (e) =>
        {
          OnCLSButtonToggle();
        };
        return true;
      }
      catch
      {
        // Blizzy Toolbar instantiation error.  ignore.
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
      return settings ?? (settings = ConfigNode.Load(SettingsFile) ?? new ConfigNode());
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
      if (!Directory.Exists(SettingsPath))
        Directory.CreateDirectory(SettingsPath);
      settings.Save(SettingsFile);
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
      ScreenMessages messages = ScreenMessages.Instance;
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
      List<ScreenMessagesText>.Enumerator list = messagetexts.GetEnumerator();
      while (list.MoveNext())
      {
        //If the user specified text to search for only delete messages that contain that text.
        if (messagetext != "")
        {
          if (list.Current != null && list.Current.text.text.Contains(messagetext))
          {
            Destroy(list.Current.gameObject);
          }
        }
        else  //If the user did not specific a message text to search for we DELETE ALL messages!!
        {
          if (list.Current == null) continue;
          Destroy(list.Current.gameObject);
        }
      }
    }
    #endregion Support/action methods
  }
}
