using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSP.Localization;
using KSP.UI.Screens;
using KSP.UI.Screens.Flight.Dialogs;
using UnityEngine;

namespace ConnectedLivingSpace
{ 
  [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
  public class CLSAddon : MonoBehaviour, ICLSAddon
  {
    #region static Properties

    internal static Dictionary<string, string> CLSTags;

    internal static bool WindowVisable;
    private static Rect windowPosition = new Rect(0, 0, 400, 100);
    private static Rect windowOptionsPosition = new Rect(0, 0, 200, 120);
    private static Rect scrollCrew = new Rect(0, 0, 0, 0);
    private static Rect scrollParts = new Rect(0, 0, 0, 0);
    private static float scrollY = 0;
    private static float scrollXCrew = 0;
    private static float scrollXParts = 0;

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
    internal static bool IsStyleSet = false;

    // For localization.  These are the default (english) values...
    private static string _clsLocTitle = "Connected Living Space";
    private static string _clsLocOptions = "Options";
    private static string _clsLocOptionTt = "Click to view/edit options";
    private static string _clsLocSpace = "Living Space";
    private static string _clsLocName = "Name";
    private static string _clsLocUpdate = "Update";
    private static string _clsLocCapacity = "CrewCapacity";
    private static string _clsLocPartCount = "Selected Space Parts Count";
    private static string _clsLocParts = "Parts";
    private static string _clsLocInfo = "Crew Info";
    private static string _clsLocNoVessel = "No current vessel";
    private static string _clsLocUnrestricted = "Allow Unrestricted Crew Transfers";
    private static string _clsLocOptPassable = "Enable Optional Passable Parts\\n(Requires game restart)";
    private static string _clsLocBlizzy = "Use Blizzy's Toolbar instead of Stock";
    private static string _clsLocWarnFull = "CLS - This module is either full or internally unreachable (different spaces)";
    private static string _clsLocWarnXfer = "CLS has prevented the transfer of";
    private static string _clsLocAnd = "and ";
    private static string _clsLocNotSameLs = "are not in the same living space";
    private static string _clsLocNone = "None";


    public static CLSAddon Instance
    {
      get;
      private set;
    }

    public static EventData<Vessel> onCLSVesselChange = new EventData<Vessel>("onCLSVesselChange");

    #endregion static Properties

    #region Instanced Properties

    private bool optionsVisible;

    private ConfigNode settings;
    private Vector2 scrollViewerCrew = Vector2.zero;
    private Vector2 scrollViewerParts = Vector2.zero;

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
      CacheClsLocalization();
      SetLocalization();
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
      SettingsPath = $"{KSPUtil.ApplicationRootPath}GameData/ConnectedLivingSpace/Plugins/PluginData";
      SettingsFile = $"{SettingsPath}/cls_settings.dat";

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
        CLSStyles.SetupGuiStyles();

        windowPosition = GUILayout.Window(947695, windowPosition, OnWindow, _clsLocTitle, windowStyle, GUILayout.MinHeight(80), GUILayout.MinWidth(400), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Width(400), GUILayout.Height(80));
        if (!optionsVisible) return;
        if (windowOptionsPosition == new Rect(0, 0, 0, 0))
          windowOptionsPosition = new Rect(windowPosition.x + windowPosition.width + 10, windowPosition.y, 260, 120);
        windowOptionsPosition = GUILayout.Window(947696, windowOptionsPosition, DisplayOptionWindow, _clsLocOptions, windowStyle, GUILayout.MinHeight(120), GUILayout.ExpandWidth(true));
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

    #region Display Methods
    private void OnWindow(int windowID)
    {
      DisplayCLSWindow();
    }

    private void DisplayCLSWindow()
    {
      // set scrollviewer sizes...
      if (Event.current.type == EventType.Repaint)
      {
        scrollY = scrollCrew.height > scrollParts.height ? scrollCrew.height : scrollParts.height;
        scrollXCrew = scrollCrew.width > 140 ? scrollCrew.width : 140;
        scrollXParts = scrollParts.width > 240 ? scrollParts.width : 240;

        // reset counters.
        scrollCrew.height = scrollParts.height = scrollCrew.width = scrollParts.width = 0;
      }
      try
      {
        Rect rect = new Rect(windowPosition.width - 20, 4, 16, 16);
        if (GUI.Button(rect, ""))
        {
          OnCLSButtonToggle();
        }
        rect = new Rect(windowPosition.width - 90, 4, 65, 16);
        if (GUI.Button(rect, new GUIContent(_clsLocOptions, _clsLocOptionTt))) // "Options","Click to view/edit options"
        {
          optionsVisible = !optionsVisible;
        }
        GUILayout.BeginVertical();
        GUI.enabled = true;

        // Build strings describing the contents of each of the spaces.
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
              spaceNames[counter] = $"{_clsLocSpace} {counter + 1}";
            }
            else
            {
              spaceNames[counter] = spaces.Current.Name;
            }
            counter++;
          }
          spaces.Dispose();

          if (vessel.Spaces.Count > 0)
          {
            newSelectedSpace = DisplaySpaceButtons(WindowSelectedSpace, spaceNames);
          }


          // Only fiddle with the highlighting if the selected space has actually changed
          UpdateDisplayHighlghting(newSelectedSpace);

          // Update the space that has been selected.
          WindowSelectedSpace = newSelectedSpace;

          // If one of the spaces has been selected then display lists of the crew and parts that make it up
          if (WindowSelectedSpace != -1)
          {
            Rect _rect;
            // Loop through all the parts in the newly selected space and create a list of all the spaces in it.
            partsList = $"{_clsLocParts}:";
            List<ICLSPart>.Enumerator parts = vessel.Spaces[WindowSelectedSpace].Parts.GetEnumerator();
            while (parts.MoveNext())
            {
              if (parts.Current == null) continue;
              partsList += "\n- " + (parts.Current.Part).partInfo.title;
            }
            parts.Dispose();

            string crewList = $"{_clsLocInfo}:";
            if (vessel.Spaces[WindowSelectedSpace].Crew.Count == 0)
              crewList += "\n- " + _clsLocNone;
            else
            {
              List<ICLSKerbal>.Enumerator crewmembers = vessel.Spaces[WindowSelectedSpace].Crew.GetEnumerator();
              while (crewmembers.MoveNext())
              {
                if (crewmembers.Current == null) continue;
                crewList += "\n- " + (crewmembers.Current.Kerbal).name;
              }
              crewmembers.Dispose();
            }

            // Display the text box that allows the space name to be changed
            GUILayout.BeginHorizontal();
            GUILayout.Label(_clsLocName + ":"); // "Name:"
            spaceNameEditField = GUILayout.TextField(spaceNameEditField, GUILayout.Width(200));
            if (GUILayout.Button(_clsLocUpdate)) // "Update"
            {
              vessel.Spaces[WindowSelectedSpace].Name = spaceNameEditField;
            }
            GUILayout.EndHorizontal();

            // Lets use 2 scrollers for Crew and parts to save space...
            GUILayout.BeginHorizontal();

            // Crew Scroller
            scrollViewerCrew = GUILayout.BeginScrollView(scrollViewerCrew, GUILayout.Width(scrollXCrew), GUILayout.Height(20 > scrollY ? 20 : scrollY + 20));
            GUILayout.BeginVertical();

            // Display the crew capacity of the space.
            GUILayout.Label($"{_clsLocCapacity}:  {vessel.Spaces[WindowSelectedSpace].MaxCrew}");
            _rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
              scrollCrew.height = _rect.height;
              scrollCrew.width = _rect.width;
            }

            // Crew Capacity
            GUILayout.Label(crewList);
            _rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
              scrollCrew.height += _rect.height;
              scrollCrew.width = scrollCrew.width > _rect.width ? scrollCrew.width : _rect.width;
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            // Part Scroller
            scrollViewerParts = GUILayout.BeginScrollView(scrollViewerParts, GUILayout.Width(scrollXParts), GUILayout.Height(20 > scrollY ? 20 : scrollY + 20));
            GUILayout.BeginVertical();

            // Display the Part count of the space.
            GUILayout.Label($"{_clsLocPartCount}:  {vessel.Spaces[WindowSelectedSpace].Parts.Count}"); // Selected Space Parts Count
            _rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
              scrollParts.height = _rect.height;
              scrollParts.width = _rect.width;
            }

            // Display the list of component parts.
            GUILayout.Label(partsList);
            _rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
              scrollParts.height += _rect.height;
              scrollParts.width = scrollParts.width > _rect.width ? scrollParts.width : _rect.width;
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
          }
        }
        else
        {
          GUILayout.Label("", GUILayout.Height(20)); // Add some vertical space.
          GUILayout.Label(_clsLocNoVessel, CLSStyles.LabelStyleBold, GUILayout.Width(380)); // "No current vessel"
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

    private void UpdateDisplayHighlghting(int newSelectedSpace)
    {
      // First unhighlight the space that was selected.
      if (WindowSelectedSpace == newSelectedSpace) return;
      if (-1 != WindowSelectedSpace && WindowSelectedSpace < vessel.Spaces.Count)
      {
        vessel.Spaces[WindowSelectedSpace].Highlight(false);
      }

      if (newSelectedSpace == -1) return;

      // Update the text in the Space edit box
      spaceNameEditField = vessel.Spaces[newSelectedSpace].Name;

      // Highlight the new space
      vessel.Spaces[newSelectedSpace].Highlight(true);
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
      allowUnrestrictedTransfers = GUILayout.Toggle(allowUnrestrictedTransfers, _clsLocUnrestricted); // "Allow Crew Unrestricted Transfers"
      if (oldAllow != allowUnrestrictedTransfers)
      {
        backupAllowUnrestrictedTransfers = allowUnrestrictedTransfers;
      }
      // Optional Passable Parts
      enablePassable = GUILayout.Toggle(enablePassable, _clsLocOptPassable); // "Enable Optional Passable Parts\r\n(Requires game restart)"

      // Blizzy Toolbar?
      if (ToolbarManager.ToolbarAvailable)
        GUI.enabled = true;
      else
      {
        GUI.enabled = false;
        enableBlizzyToolbar = false;
      }
      enableBlizzyToolbar = GUILayout.Toggle(enableBlizzyToolbar, _clsLocBlizzy); // "Use Blizzy's Toolbar instead of Stock"

      GUI.enabled = true;
      GUILayout.EndVertical();
      GUI.DragWindow();
      RepositionWindow(ref windowOptionsPosition);
    }

    private static int DisplaySpaceButtons(int selectedSpace, string[] spaceNames)
    {
      // Selected Space options
      GUIContent[] options = new GUIContent[spaceNames.Length];
      GUIStyle[] styles = new GUIStyle[spaceNames.Length];
      int newSelectedSpace = selectedSpace;

      // Populate button characteristics
      for (int x = 0; x < spaceNames.Length; x++)
      {
        options[x] = new GUIContent(spaceNames[x]);
        styles[x] = new GUIStyle(newSelectedSpace == x ? CLSStyles.ButtonToggledStyle : CLSStyles.ButtonStyle);
      }

      // Build Option Buttons
      GUILayout.BeginVertical();
      for (int x = 0; x < spaceNames.Length; x++)
      {
        if (GUILayout.Button(options[x], styles[x], GUILayout.Height(20)))
        {
          if (newSelectedSpace != x) newSelectedSpace = x;
          else newSelectedSpace = -1; // revert to none selected.
        }
      }
      GUILayout.EndVertical();

      return newSelectedSpace;
    }

    #endregion

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

        // Notify other mods that the CLS Vessel has been rebuilt.
        onCLSVesselChange.Fire(FlightGlobals.ActiveVessel);

        if (!WindowVisable || WindowSelectedSpace <= -1) return;
        vessel.Highlight(false);
        vessel.Spaces[CLSAddon.WindowSelectedSpace].Highlight(true);
      }
      catch (Exception ex)
      {
        Debug.Log("CLS rebuild Vessel Error:  " + ex.ToString());
      }
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
      parts.Dispose();
    }

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
      fromList.Dispose();

      if (fullList.Count <= 0) return;
      //CrewTransfer.fullMessage = "<color=orange>CLS - This module is either full or internally unreachable.</color>";
      List<Part>.Enumerator removeList = fullList.GetEnumerator();
      while (removeList.MoveNext())
      {
        eventData.from.Remove(removeList.Current);
      }
      removeList.Dispose();
      eventData.to.AddRange(fullList);
    }

    internal void OnItemTransferStarted(PartItemTransfer xferPartItem)
    {
      if (!allowUnrestrictedTransfers && xferPartItem.type == "Crew")
        xferPartItem.semiValidMessage = $"<color=orange>{_clsLocWarnFull}.</color>"; // CLS - This module is either full or internally unreachable (different spaces)
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
        $"<color=orange>{_clsLocWarnXfer}: {crewTransferData.crewMember.name}.  {crewTransferData.sourcePart.partInfo.title} {_clsLocAnd} {crewTransferData.destPart.partInfo.title} {_clsLocNotSameLs}.</color>", 10f);
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
        blizzyToolbarButton.ToolTip = _clsLocTitle; // "Connected Living Space";
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
      list.Dispose();
    }
    #endregion Support/action methods

    #region Localization
    internal static void CacheClsLocalization()
    {
      CLSTags = new Dictionary<string, string>();
      IEnumerator tags = Localizer.Tags.Keys.GetEnumerator();
      while (tags.MoveNext())
      {
        if (tags.Current == null) continue;
        if (tags.Current.ToString().Contains("#clsloc_"))
          CLSTags.Add(tags.Current.ToString(), Localizer.GetStringByTag(tags.Current.ToString()).Replace("\\n", "\n"));
      }
    }

    internal static string Localize(string tag)
    {
      return CLSTags[tag];
    }

    private static void SetLocalization()
    {
      _clsLocTitle = Localize("#clsloc_001"); // "Connected Living Space";
      _clsLocOptions = Localize("#clsloc_002"); // "Options";
      _clsLocOptionTt = Localize("#clsloc_003"); // "Click to view/edit options";
      _clsLocSpace = Localize("#clsloc_004"); // "Living Space";
      _clsLocName = Localize("#clsloc_005"); // "Name";
      _clsLocUpdate = Localize("#clsloc_006"); // "Update";
      _clsLocCapacity = Localize("#clsloc_007"); // "CrewCapacity";
      _clsLocInfo = Localize("#clsloc_008"); // "Crew Info";
      _clsLocNoVessel = Localize("#clsloc_009"); // "No current vessel";
      _clsLocUnrestricted = Localize("#clsloc_010"); // "Allow Unrestricted Crew Transfers";
      _clsLocOptPassable = Localize("#clsloc_011"); // "Enable Optional Passable Parts\\n(Requires game restart)";
      _clsLocBlizzy = Localize("#clsloc_012"); // "Use Blizzy's Toolbar instead of Stock";
      _clsLocWarnFull = Localize("#clsloc_013"); // "CLS - This module is either full or internally unreachable (different spaces)";
      _clsLocWarnXfer = Localize("#clsloc_014"); // "CLS has prevented the transfer of";
      _clsLocAnd = Localize("#clsloc_015"); // "and";
      _clsLocNotSameLs = Localize("#clsloc_016"); // "are not in the same living space";
      _clsLocPartCount = Localize("#clsloc_040"); // "Selected Space Parts Count"
      _clsLocParts = Localize("#clsloc_041"); // "Parts";
      _clsLocNone = Localize("#clsloc_020"); // "None"
    }
    #endregion
  }
}
