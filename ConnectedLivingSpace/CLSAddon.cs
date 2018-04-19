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

    // GUI
    internal static bool WindowVisable;
    private static Rect _windowPosition = new Rect(0, 0, 400, 100);
    private static Rect _windowOptionsPosition = new Rect(0, 0, 200, 120);
    private static Rect _scrollCrew = new Rect(0, 0, 0, 0);
    private static Rect _scrollParts = new Rect(0, 0, 0, 0);
    private static float _scrollY = 0;
    private static float _scrollXCrew = 0;
    private static float _scrollXParts = 0;
    private static GUIStyle _windowStyle;
    internal static bool IsStyleSet = false;

    // settings
    private static bool _allowUnrestrictedTransfers;
    private static bool _backupAllowUnrestrictedTransfers; // this value is used to "remember" the actual setting in CLS in the event it was changed by another mod
    public static bool EnablePassable;
    public static bool EnableBlizzyToolbar;
    private static bool _prevEnableBlizzyToolbar;
    private static string _settingsPath;
    private static string _settingsFile;

    // this var is now restricted to use by the CLS window.  Highlighting will be handled by part.
    internal static int WindowSelectedSpace = -1;

    // applauncher/toolbar
    private static ApplicationLauncherButton _stockToolbarButton; // Stock Toolbar Button
    internal static IButton BlizzyToolbarButton; // Blizzy Toolbar Button

    // Localization
    internal static Dictionary<string, string> CLSTags;

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

    private bool _optionsVisible;

    private ConfigNode _settings;
    private Vector2 _scrollViewerCrew = Vector2.zero;
    private Vector2 _scrollViewerParts = Vector2.zero;

    private CLSVessel _vessel;

    // State var used by OnEditorShipModified event handler to note changes to vessel for reconstruction of spaces.
    private int _editorPartCount;

    private string _spaceNameEditField;

    public ICLSVessel Vessel
    {
      get
      {
        return _vessel;
      }
    }

    public bool AllowUnrestrictedTransfers
    {
      get
      {
        return _allowUnrestrictedTransfers;
      }
      set { _allowUnrestrictedTransfers = value; }
    }

    #endregion Instanced Properties

    #region Recoupler support
    protected internal List<ConnectPair> requestedConnections = new List<ConnectPair>();

    protected internal struct ConnectPair
    {
      public Part part1;
      public Part part2;

      public ConnectPair(Part part1, Part part2)
      {
        this.part1 = part1;
        this.part2 = part2;
      }
      public bool Includes(Part part)
      {
        return (part1 == part || part2 == part);
      }
      public bool IsEquivalentTo(Part part1, Part part2)
      {
        return ((this.part1 == part1 && this.part2 == part2) || (this.part1 == part2 && this.part2 == part1));
      }
      public static ConnectPair Other(Part part1, Part part2)
      {
        return new ConnectPair(part2, part1);
      }
      public static ConnectPair Other(ConnectPair connectPair)
      {
        return new ConnectPair(connectPair.part2, connectPair.part1);
      }
      public ConnectPair Other()
      {
        return new ConnectPair(this.part2, this.part1);
      }
      public Part OtherPart(Part inPart)
      {
        if (!this.Includes(inPart))
          return null;
        return part1 == inPart ? part1 : part2;
      }
    }
    #endregion Recoupler support

    #region Constructor
    public CLSAddon()
    {
      if (Instance == null)
      {
        Instance = this;
      }
    }
    #endregion Constructor

    #region Life Cycle
    public void Awake()
    {
      //Debug.Log("CLSAddon:Awake");
      CacheClsLocalization();
      SetLocalization();
      // Added support for Blizzy Toolbar and hot switching between Stock and Blizzy
      if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) return;
      if (EnableBlizzyToolbar)
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

    public void Start()
    {
      // Debug.Log("CLSAddon:Start");
      _settingsPath = $"{KSPUtil.ApplicationRootPath}GameData/ConnectedLivingSpace/Plugins/PluginData";
      _settingsFile = $"{_settingsPath}/cls_settings.dat";

      _windowStyle = new GUIStyle(HighLogic.Skin.window);

      // load toolbar selection setting
      ApplySettings();

      if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
      {
        // TODO check events
        GameEvents.onFlightReady.Add(OnFlightReady);
        GameEvents.onVesselChange.Add(OnVesselChange);
        GameEvents.onVesselWasModified.Add(OnVesselWasModified);
        GameEvents.onCrewTransferred.Add(OnCrewTransferred);
        onCLSVesselChange.Add(onCLSVesselChangeHandler);
        //GameEvents.onEditorPodPicked.Add(OnEditorPodPicked);
        GameEvents.onEditorPodDeleted.Add(OnEditorPodDeleted);
        GameEvents.onEditorShipModified.Add(OnEditorShipModified);
        GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);

        GameEvents.onItemTransferStarted.Add(OnItemTransferStarted);
        GameEvents.onCrewTransferPartListCreated.Add(OnCrewTransferPartListCreated);
        GameEvents.onCrewTransferSelected.Add(OnCrewTransferSelected);
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

    public void OnDestroy()
    {
      //Debug.Log("CLSAddon::OnDestroy");

      _allowUnrestrictedTransfers = _backupAllowUnrestrictedTransfers;
      saveSettings();

      // TODO check events
      GameEvents.onFlightReady.Remove(OnFlightReady);
      GameEvents.onVesselChange.Remove(OnVesselChange);
      GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
      GameEvents.onCrewTransferred.Remove(OnCrewTransferred);
      onCLSVesselChange.Remove(onCLSVesselChangeHandler);
      //GameEvents.onEditorPodPicked.Remove(OnEditorPodPicked);
      GameEvents.onEditorPodDeleted.Remove(OnEditorPodDeleted);
      GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
      GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);

      GameEvents.onItemTransferStarted.Remove(OnItemTransferStarted);
      GameEvents.onCrewTransferPartListCreated.Remove(OnCrewTransferPartListCreated);
      GameEvents.onCrewTransferSelected.Remove(OnCrewTransferSelected);

      // Remove the stock toolbar button
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
      GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
      if (_stockToolbarButton != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(_stockToolbarButton);
      }
    }
    #endregion Life Cycle

    #region Game Events
    // Event called after game is loaded, all vessels initialized etc. Vessel ready to fly.
    private void OnFlightReady()
    {
      UpdateActiveVessel();
    }

    // This event is fired when switching between vessels.
    private void OnVesselChange(Vessel data)
    {
      UpdateActiveVessel();
    }

    private void OnVesselWasModified(Vessel data)
    {
      if (data == null || !data.loaded) return;
      data.GetComponent<CLSVesselModule>().MarkDirty();
    }

    // CLS state needs to be updated for crew movements (transfers between parts, to/from EVA)
    private void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> data)
    {
      data.from.vessel.GetComponent<CLSVesselModule>().MarkDirty();
      if (data.from.vessel != data.to.vessel)
        data.to.vessel.GetComponent<CLSVesselModule>().MarkDirty();
    }

    private void onCLSVesselChangeHandler(Vessel data)
    {
      if (data.Equals(FlightGlobals.ActiveVessel))
        UpdateActiveVessel();
    }

    private void UpdateActiveVessel()
    {
      _vessel = FlightGlobals.ActiveVessel.GetComponent<CLSVesselModule>().CLSVessel;
      if (!WindowVisable || WindowSelectedSpace <= -1) return;
      _vessel.Highlight(false);
      _vessel.Spaces[CLSAddon.WindowSelectedSpace].Highlight(true);
    }

    // Git Issue #85.  sometimes spaces are not updating on a vessel.  Deleting a pod does not clear the current vessel.
    private void OnEditorPodPicked(Part part)
    {
      _vessel.Clear();
      _vessel = null;
      if (null != EditorLogic.RootPart)
      {
        _vessel = new CLSVessel();
        _vessel.Populate(EditorLogic.RootPart);
      }
      _editorPartCount = 1;
    }
    // Git Issue #85.  sometimes spaces are not updating on a vessel.  Deleting a pod does not clear the current vessel.
    private void OnEditorPodDeleted()
    {
      _vessel.Clear();
      _vessel = null;
      _editorPartCount = 0;
    }

    private void OnEditorShipModified(ShipConstruct vesselConstruct)
    {
      if (vesselConstruct.Parts.Count == _editorPartCount) return;
      //Debug.Log("Calling RebuildCLSVessel as the part count has changed in the editor");

      UpdateShipConstruct();

      _editorPartCount = vesselConstruct.Parts.Count;
      // First unhighlight the space that was selected.
      if (-1 != WindowSelectedSpace && WindowSelectedSpace < _vessel.Spaces.Count)
      {
        _vessel.Spaces[WindowSelectedSpace].Highlight(true);
      }
    }

    internal void DelayedUpdateShipConstruct()
    {
      StartCoroutine(DelayUpdateShipConstruct());
    }
    private IEnumerator DelayUpdateShipConstruct() {
      yield return new WaitForEndOfFrame();
      UpdateShipConstruct();
    }

    private void UpdateShipConstruct()
    {
      if (null != _vessel)
      {
        _vessel.Clear();
        _vessel = null;
      }

      if (EditorLogic.RootPart == null)
        return;
      
      try
      {
        // Build new vessel information
        _vessel = new CLSVessel();
        _vessel.Populate(EditorLogic.RootPart);

        // Recoupler support
        for (int i = requestedConnections.Count - 1; i >= 0; i--)
        {
          ConnectPair connectPair = requestedConnections[i];
          if (connectPair.part1.vessel != connectPair.part2.vessel)
            requestedConnections.Remove(connectPair);
          _vessel.MergeSpaces(connectPair.part1, connectPair.part2);
        }

      }
      catch (Exception ex)
      {
        Debug.Log($"CLS rebuild Vessel Error:  { ex}");
      }
    }

    private void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> sceneData)
    {
      if (WindowVisable) OnCLSButtonToggle();
    }
    #endregion Game Events

    #region API Support
    public ICLSVessel getCLSVessel(Vessel v)
    {
      if (v == null || !v.loaded) return null;
      return v.GetComponent<CLSVesselModule>()?.CLSVessel;
    }

    protected internal bool RequestAddConnection(Part part1, Part part2, bool rebuildVessel = true)
    {
      ConnectPair connectPair = new ConnectPair(part1, part2);
      if (requestedConnections.Contains(connectPair) || requestedConnections.Contains(connectPair.Other()))
        return false;
      requestedConnections.Add(connectPair);
      if (rebuildVessel)
        if (HighLogic.LoadedSceneIsEditor)
          UpdateShipConstruct();
        else
          part1.vessel.GetComponent<CLSVesselModule>().MarkDirty();
      return true;
    }

    public bool RequestAddConnection(Part part1, Part part2)
    {
      return RequestAddConnection(part1, part2, true);
    }

    public List<bool> RequestAddConnections(List<Part> part1, List<Part> part2)
    {
      List<bool> success = new List<bool>();
      if (part1.Count != part2.Count)
        return success;
      for (int i = 0; i < part1.Count; i++)
      {
        success.Add(RequestAddConnection(part1[i], part2[i], false));
      }
      if (!success.Any((bool b) => b)) return success;
      if (HighLogic.LoadedSceneIsEditor)
        UpdateShipConstruct();
      else
        foreach (Vessel v in success.Select((x, i) => new { suc = x, idx = i }).Where(x => x.suc).Select(x => part1.ElementAt(x.idx)).Select(p => p.vessel).Distinct())
          v.GetComponent<CLSVesselModule>().MarkDirty();
      return success;
    }

    protected internal bool RequestRemoveConnection(Part part1, Part part2, bool rebuildVessel = true)
    {
      bool modified = false;
      ConnectPair CPtoRemove = new ConnectPair(part1, part2);
      if (requestedConnections.Contains(CPtoRemove))
      {
        requestedConnections.Remove(CPtoRemove);
        modified = true;
      }
      else if (requestedConnections.Contains(CPtoRemove.Other()))
      {
        requestedConnections.Remove(CPtoRemove.Other());
        modified = true;
      }
      if (modified && rebuildVessel)
        if (HighLogic.LoadedSceneIsEditor)
          UpdateShipConstruct();
        else
          part1.vessel.GetComponent<CLSVesselModule>().MarkDirty();
      return modified;
    }

    public bool RequestRemoveConnection(Part part1, Part part2)
    {
      return RequestRemoveConnection(part1, part2, true);
    }

    public List<bool> RequestRemoveConnections(List<Part> part1, List<Part> part2)
    {
      List<bool> success = new List<bool>();
      if (part1.Count != part2.Count)
        return success;
      for (int i = 0; i < part1.Count; i++)
      {
        success.Add(RequestRemoveConnection(part1[i], part2[i], false));
      }
      if (!success.Any((bool b) => b)) return success;
      if (HighLogic.LoadedSceneIsEditor)
        UpdateShipConstruct();
      else
        foreach (Vessel v in success.Select((x, i) => new { suc = x, idx = i }).Where(x => x.suc).Select(x => part1.ElementAt(x.idx)).Select(p => p.vessel).Distinct())
          v.GetComponent<CLSVesselModule>().MarkDirty();
      return success;
    }
    #endregion API Support

    #region Crew Transfer Restriction
    private void OnCrewTransferPartListCreated(GameEvents.HostedFromToAction<Part, List<Part>> eventData)
    {
      if (_allowUnrestrictedTransfers) return;

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
      if (!_allowUnrestrictedTransfers && xferPartItem.type == "Crew")
        xferPartItem.semiValidMessage = $"<color=orange>{_clsLocWarnFull}.</color>"; // CLS - This module is either full or internally unreachable (different spaces)
    }

    // Method to optionally abort an attempt to use the stock crew transfer mechanism
    private void OnCrewTransferSelected(CrewTransfer.CrewTransferData crewTransferData)
    {
      // If transfers are not restricted then we have got nothing to do here.
      if (_allowUnrestrictedTransfers) return;
      ICLSPart clsFrom = Instance.Vessel.Parts.Find(x => x.Part == crewTransferData.sourcePart);
      ICLSPart clsTo = Instance.Vessel.Parts.Find(x => x.Part == crewTransferData.destPart);

      // If in same space, ignore
      if (clsFrom != null && clsTo != null && clsFrom.Space == clsTo.Space) return;
      
      // Okay, houston, we have a problem.   Prevent transfer.
      crewTransferData.canTransfer = false;
      ScreenMessages.PostScreenMessage(
        $"<color=orange>{_clsLocWarnXfer}: {crewTransferData.crewMember.name}.  {crewTransferData.sourcePart.partInfo.title} {_clsLocAnd} {crewTransferData.destPart.partInfo.title} {_clsLocNotSameLs}.</color>", 10f);
    }
    #endregion Crew Transfer Restriction

    #region Toolbar Functionality
    void OnGUIAppLauncherReady()
    {
      _stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
          OnCLSButtonToggle,
          OnCLSButtonToggle,
          DummyVoid,
          DummyVoid,
          DummyVoid,
          DummyVoid,
          ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
          GameDatabase.Instance.GetTexture("ConnectedLivingSpace/assets/cls_icon_off_128", false));
    }

    void OnGUIAppLauncherDestroyed()
    {
      if (_stockToolbarButton == null) return;
      ApplicationLauncher.Instance.RemoveModApplication(_stockToolbarButton);
      _stockToolbarButton = null;
    }

    private static void DummyVoid() { }

    internal void OnCLSButtonToggle()
    {
      //Debug.Log("CLSAddon::OnCLSButtonToggle");
      WindowVisable = !WindowVisable;

      if (!WindowVisable && null != _vessel)
        _vessel.Highlight(false);

      if (EnableBlizzyToolbar)
        BlizzyToolbarButton.TexturePath = WindowVisable ? "ConnectedLivingSpace/assets/cls_b_icon_on" : "ConnectedLivingSpace/assets/cls_b_icon_off";
      else
        _stockToolbarButton.SetTexture(GameDatabase.Instance.GetTexture(WindowVisable ? "ConnectedLivingSpace/assets/cls_icon_on_128" : "ConnectedLivingSpace/assets/cls_icon_off_128", false));
    }

    internal bool ActivateBlizzyToolBar()
    {
      if (!EnableBlizzyToolbar) return false;
      try
      {
        if (!ToolbarManager.ToolbarAvailable) return false;
        if (HighLogic.LoadedScene != GameScenes.EDITOR && HighLogic.LoadedScene != GameScenes.FLIGHT) return true;
        BlizzyToolbarButton = ToolbarManager.Instance.add("ConnectedLivingSpace", "ConnectedLivingSpace");
        BlizzyToolbarButton.TexturePath = "ConnectedLivingSpace/assets/cls_b_icon_on";
        BlizzyToolbarButton.ToolTip = _clsLocTitle; // "Connected Living Space";
        BlizzyToolbarButton.Visible = true;
        BlizzyToolbarButton.OnClick += (e) =>
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

    private void CheckForToolbarTypeToggle()
    {
      if (EnableBlizzyToolbar && !_prevEnableBlizzyToolbar)
      {
        // Let't try to use Blizzy's toolbar
        if (!ActivateBlizzyToolBar())
        {
          // We failed to activate the toolbar, so revert to stock
          GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);

          EnableBlizzyToolbar = _prevEnableBlizzyToolbar;
        }
        else
        {
          OnGUIAppLauncherDestroyed();
          GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
          GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
          _prevEnableBlizzyToolbar = EnableBlizzyToolbar;
          if (HighLogic.LoadedSceneIsFlight)
            BlizzyToolbarButton.Visible = true;
        }

      }
      else if (!EnableBlizzyToolbar && _prevEnableBlizzyToolbar)
      {
        // Use stock Toolbar
        if (HighLogic.LoadedSceneIsFlight)
          BlizzyToolbarButton.Visible = false;
        GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
        GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
        OnGUIAppLauncherReady();
        _prevEnableBlizzyToolbar = EnableBlizzyToolbar;
      }
    }
    #endregion Toolbar Functionality

    #region Settings
    private void ApplySettings()
    {
      if (_settings == null)
        loadSettings();
      ConfigNode toolbarNode = _settings.HasNode("clsSettings") ? _settings.GetNode("clsSettings") : _settings.AddNode("clsSettings");
      if (toolbarNode.HasValue("enableBlizzyToolbar"))
        EnableBlizzyToolbar = bool.Parse(toolbarNode.GetValue("enableBlizzyToolbar"));
      _windowPosition = getRectangle(toolbarNode, "windowPosition", _windowPosition);
      _windowOptionsPosition = getRectangle(toolbarNode, "windowOptionsPosition", _windowOptionsPosition);
      EnableBlizzyToolbar = toolbarNode.HasValue("enableBlizzyToolbar") ? bool.Parse(toolbarNode.GetValue("enableBlizzyToolbar")) : EnableBlizzyToolbar;
      EnablePassable = toolbarNode.HasValue("enablePassable") ? bool.Parse(toolbarNode.GetValue("enablePassable")) : EnablePassable;

    }

    private ConfigNode loadSettings()
    {
      return _settings ?? (_settings = ConfigNode.Load(_settingsFile) ?? new ConfigNode());
    }

    private void saveSettings()
    {
      if (_settings == null)
        _settings = loadSettings();
      ConfigNode toolbarNode = _settings.HasNode("clsSettings") ? _settings.GetNode("clsSettings") : _settings.AddNode("clsSettings");
      if (toolbarNode.HasValue("enableBlizzyToolbar"))
        toolbarNode.RemoveValue("enableBlizzyToolbar");
      toolbarNode.AddValue("enableBlizzyToolbar", EnableBlizzyToolbar.ToString());
      WriteRectangle(toolbarNode, "windowPosition", _windowPosition);
      WriteRectangle(toolbarNode, "windowOptionsPosition", _windowOptionsPosition);
      WriteValue(toolbarNode, "enableBlizzyToolbar", EnableBlizzyToolbar);
      WriteValue(toolbarNode, "enablePassable", EnablePassable);
      if (!Directory.Exists(_settingsPath))
        Directory.CreateDirectory(_settingsPath);
      _settings.Save(_settingsFile);
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
    #endregion Settings

    #region Init
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
            //Debug.Log($"Adding ConnectedLivingSpace Support to {part.name}/{prefabPart.partInfo.title}");

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
    #endregion Init

    #region Display
    private void OnGUI()
    {
      if (WindowVisable)
      {
        //Set the GUI Skin
        //GUI.skin = HighLogic.Skin;
        CLSStyles.SetupGuiStyles();

        _windowPosition = GUILayout.Window(947695, _windowPosition, OnWindow, _clsLocTitle, _windowStyle, GUILayout.MinHeight(80), GUILayout.MinWidth(400), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Width(400), GUILayout.Height(80));
        if (!_optionsVisible) return;
        if (_windowOptionsPosition == new Rect(0, 0, 0, 0))
          _windowOptionsPosition = new Rect(_windowPosition.x + _windowPosition.width + 10, _windowPosition.y, 260, 120);
        _windowOptionsPosition = GUILayout.Window(947696, _windowOptionsPosition, DisplayOptionWindow, _clsLocOptions, _windowStyle, GUILayout.MinHeight(120), GUILayout.ExpandWidth(true));
      }
      else
      {
        if (WindowSelectedSpace <= -1) return;
        _vessel.Spaces[WindowSelectedSpace].Highlight(false);
        WindowSelectedSpace = -1;
      }
    }

    private void OnWindow(int windowID)
    {
      DisplayCLSWindow();
    }

    private void DisplayCLSWindow()
    {
      // set scrollviewer sizes...
      if (Event.current.type == EventType.Repaint)
      {
        _scrollY = _scrollCrew.height > _scrollParts.height ? _scrollCrew.height : _scrollParts.height;
        _scrollXCrew = _scrollCrew.width > 140 ? _scrollCrew.width : 140;
        _scrollXParts = _scrollParts.width > 240 ? _scrollParts.width : 240;

        // reset counters.
        _scrollCrew.height = _scrollParts.height = _scrollCrew.width = _scrollParts.width = 0;
      }
      try
      {
        Rect rect = new Rect(_windowPosition.width - 20, 4, 16, 16);
        if (GUI.Button(rect, ""))
        {
          OnCLSButtonToggle();
        }
        rect = new Rect(_windowPosition.width - 90, 4, 65, 16);
        if (GUI.Button(rect, new GUIContent(_clsLocOptions, _clsLocOptionTt))) // "Options","Click to view/edit options"
        {
          _optionsVisible = !_optionsVisible;
        }
        GUILayout.BeginVertical();
        GUI.enabled = true;

        // Build strings describing the contents of each of the spaces.
        if (null != _vessel)
        {
          string[] spaceNames = new string[_vessel.Spaces.Count];
          int counter = 0;
          int newSelectedSpace = -1;

          string partsList = "";
          List<ICLSSpace>.Enumerator spaces = _vessel.Spaces.GetEnumerator();
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

          if (_vessel.Spaces.Count > 0)
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
            List<ICLSPart>.Enumerator parts = _vessel.Spaces[WindowSelectedSpace].Parts.GetEnumerator();
            while (parts.MoveNext())
            {
              if (parts.Current == null) continue;
              partsList += $"\n- {(parts.Current.Part).partInfo.title}";
            }
            parts.Dispose();

            string crewList = $"{_clsLocInfo}:";
            if (_vessel.Spaces[WindowSelectedSpace].Crew.Count == 0)
              crewList += $"\n- {_clsLocNone}";
            else
            {
              List<ICLSKerbal>.Enumerator crewmembers = _vessel.Spaces[WindowSelectedSpace].Crew.GetEnumerator();
              while (crewmembers.MoveNext())
              {
                if (crewmembers.Current == null) continue;
                crewList += $"\n- {(crewmembers.Current.Kerbal).name}";
              }
              crewmembers.Dispose();
            }

            // Display the text box that allows the space name to be changed
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{_clsLocName}:"); // "Name:"
            _spaceNameEditField = GUILayout.TextField(_spaceNameEditField, GUILayout.Width(200));
            if (GUILayout.Button(_clsLocUpdate)) // "Update"
            {
              _vessel.Spaces[WindowSelectedSpace].Name = _spaceNameEditField;
            }
            GUILayout.EndHorizontal();

            // Lets use 2 scrollers for Crew and parts to save space...
            GUILayout.BeginHorizontal();

            // Crew Scroller
            _scrollViewerCrew = GUILayout.BeginScrollView(_scrollViewerCrew, GUILayout.Width(_scrollXCrew), GUILayout.Height(20 > _scrollY ? 20 : _scrollY + 20));
            GUILayout.BeginVertical();

            // Display the crew capacity of the space.
            GUILayout.Label($"{_clsLocCapacity}:  {_vessel.Spaces[WindowSelectedSpace].MaxCrew}");
            _rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
              _scrollCrew.height = _rect.height;
              _scrollCrew.width = _rect.width;
            }

            // Crew Capacity
            GUILayout.Label(crewList);
            _rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
              _scrollCrew.height += _rect.height;
              _scrollCrew.width = _scrollCrew.width > _rect.width ? _scrollCrew.width : _rect.width;
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            // Part Scroller
            _scrollViewerParts = GUILayout.BeginScrollView(_scrollViewerParts, GUILayout.Width(_scrollXParts), GUILayout.Height(20 > _scrollY ? 20 : _scrollY + 20));
            GUILayout.BeginVertical();

            // Display the Part count of the space.
            GUILayout.Label($"{_clsLocPartCount}:  {_vessel.Spaces[WindowSelectedSpace].Parts.Count}"); // Selected Space Parts Count
            _rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
              _scrollParts.height = _rect.height;
              _scrollParts.width = _rect.width;
            }

            // Display the list of component parts.
            GUILayout.Label(partsList);
            _rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
              _scrollParts.height += _rect.height;
              _scrollParts.width = _scrollParts.width > _rect.width ? _scrollParts.width : _rect.width;
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
        RepositionWindow(ref _windowPosition);
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
      if (-1 != WindowSelectedSpace && WindowSelectedSpace < _vessel.Spaces.Count)
      {
        _vessel.Spaces[WindowSelectedSpace].Highlight(false);
      }

      if (newSelectedSpace == -1) return;

      // Update the text in the Space edit box
      _spaceNameEditField = _vessel.Spaces[newSelectedSpace].Name;

      // Highlight the new space
      _vessel.Spaces[newSelectedSpace].Highlight(true);
    }

    private void DisplayOptionWindow(int windowID)
    {
      Rect rect = new Rect(_windowOptionsPosition.width - 20, 4, 16, 16);
      if (GUI.Button(rect, ""))
      {
        _optionsVisible = false;
      }
      GUILayout.BeginVertical();
      // Unrestricted Xfers
      bool oldAllow = _allowUnrestrictedTransfers;
      _allowUnrestrictedTransfers = GUILayout.Toggle(_allowUnrestrictedTransfers, _clsLocUnrestricted); // "Allow Crew Unrestricted Transfers"
      if (oldAllow != _allowUnrestrictedTransfers)
      {
        _backupAllowUnrestrictedTransfers = _allowUnrestrictedTransfers;
      }
      // Optional Passable Parts
      EnablePassable = GUILayout.Toggle(EnablePassable, _clsLocOptPassable); // "Enable Optional Passable Parts\r\n(Requires game restart)"

      // Blizzy Toolbar?
      if (ToolbarManager.ToolbarAvailable)
        GUI.enabled = true;
      else
      {
        GUI.enabled = false;
        EnableBlizzyToolbar = false;
      }
      EnableBlizzyToolbar = GUILayout.Toggle(EnableBlizzyToolbar, _clsLocBlizzy); // "Use Blizzy's Toolbar instead of Stock"

      GUI.enabled = true;
      GUILayout.EndVertical();
      GUI.DragWindow();
      RepositionWindow(ref _windowOptionsPosition);
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

    internal void RepositionWindow(ref Rect windowPosition)
    {
      if (windowPosition.x < 0) windowPosition.x = 0;
      if (windowPosition.y < 0) windowPosition.y = 0;
      if (windowPosition.xMax > Screen.currentResolution.width)
        windowPosition.x = Screen.currentResolution.width - windowPosition.width;
      if (windowPosition.yMax > Screen.currentResolution.height)
        windowPosition.y = Screen.currentResolution.height - windowPosition.height;
    }
    #endregion Display

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
    #endregion Localization
  }
}
