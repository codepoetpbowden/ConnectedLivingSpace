using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace ConnectedLivingSpace
{
    [KSPAddonFixedCLS(KSPAddon.Startup.EveryScene, false, typeof(CLSAddon))]
    public class CLSAddon : MonoBehaviour , ICLSAddon
    {
        private static Rect windowPosition = new Rect(0,0,360,480);
        private static GUIStyle windowStyle = null;
        private static bool stockTransferFixInstalled = false;
        private static bool allowUnrestrictedTransfers = false;

        private Vector2 scrollViewer = Vector2.zero;
        
        private CLSVessel vessel = null;
        private int selectedSpace = -1;

        private ApplicationLauncherButton stockToolbarButton = null; // Stock Toolbar Button

        private bool visable = false;

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

        public CLSAddon()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void Awake() 
        {
            //Debug.Log("CLSAddon:Awake");

            this.selectedSpace = -1;

            // Set up the stock toolbar
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);

            if (!stockTransferFixInstalled)
            {
                GameEvents.onCrewTransferred.Add(CrewTransfered);
                stockTransferFixInstalled = true;
            }
        }

        public void Start() 
        {
            // Debug.Log("CLSAddon:Start");

            windowStyle = new GUIStyle(HighLogic.Skin.window);

            try
            {
                RenderingManager.RemoveFromPostDrawQueue(0, OnDraw);
            }
            catch
            {
                // This is generally not a problem - do not log it.
				// Debug.LogException(ex);
            }

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                RenderingManager.AddToPostDrawQueue(0, OnDraw);
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
            }

            // Add the CLSModule to all parts that can house crew (and do not already have it).
            AddModuleToParts();

            // Add hatches to all the docking ports (prefabs)
            AddHatchModuleToPartPrefabs();
        }

        void OnGUIAppLauncherReady()
        {
            if (ApplicationLauncher.Ready)
            {
                this.stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(onAppLaunchToggleOn,
                                                                                         onAppLaunchToggleOff,
                                                                                         DummyVoid,
                                                                                         DummyVoid,
                                                                                         DummyVoid,
                                                                                         DummyVoid,
                                                                                         ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
                                                                                         (Texture)GameDatabase.Instance.GetTexture("ConnectedLivingSpace/assets/cls_icon_off", false));
            }
        }

        void OnGUIAppLauncherDestroyed()
        {
            if (this.stockToolbarButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(this.stockToolbarButton);
                this.stockToolbarButton = null;
            }
        }

        void onAppLaunchToggleOn()
        {
            this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("ConnectedLivingSpace/assets/cls_icon_on", false));
            this.visable = true;
        }

        void onAppLaunchToggleOff()
        {
            if (null != this.vessel)
            {
                vessel.Highlight(false);
            }
            this.selectedSpace = -1;
            this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("ConnectedLivingSpace/assets/cls_icon_off", false));

            this.visable = false;
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
            RebuildCLSVessel(data);
        }
        private void OnVesselTerminated(ProtoVessel data)
        {
            //Debug.Log("CLSAddon::OnVesselTerminated");
        }
        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        {
            //Debug.Log("CLSAddon::OnPartAttach"); 
        }
        private void OnPartCouple(GameEvents.FromToAction <Part, Part> data)
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

        private void OnDraw()
        {
            if (this.visable)
            {
                //Set the GUI Skin
                //GUI.skin = HighLogic.Skin;

                windowPosition = GUILayout.Window(947695, windowPosition, OnWindow, "Connected Living Space", windowStyle,GUILayout.MinHeight(20),GUILayout.ExpandHeight(true));
            }
        }
        
        private void RebuildCLSVessel()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                RebuildCLSVessel(FlightGlobals.ActiveVessel);
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                if (null == EditorLogic.startPod)
                {
                    // There is no root part in the editor - this ought to mean that there are no parts. Juest clear out everything
                    if (null != this.vessel)
                    {
                        vessel.Clear();
                    }
                    this.vessel = null;
                    this.selectedSpace = -1;
                }
                else
                {
                    RebuildCLSVessel(EditorLogic.startPod);
                }
            }
        }

        private void RebuildCLSVessel(Vessel newVessel)
        {
            RebuildCLSVessel(newVessel.rootPart);
        }

        private void RebuildCLSVessel(Part newRootPart)
        {
            //Debug.Log("RebuildCLSVessel");
            // Before we rebuild the vessel, we need to take some steps to tidy up the highlighting and our idea of which space is the selected space. We will make a list of all the parts that are currently in the selected space. We will also unhighlight parts that are highlighted. Once the rebuild is complete we will work out which space will be the selected space based on the first part in our list that we find in oneof the new spaces. We can then highlight that new space.

            List<uint> listSelectedParts = new List<uint>();

            if (-1 != selectedSpace)
            {
                foreach (CLSPart p in vessel.Spaces[selectedSpace].Parts)
                {
                    Part part = (Part)p;
                    listSelectedParts.Add(part.flightID);
                    //Debug.Log("Part : "+ part.flightID + " currently in use." ) ;
                }

                vessel.Spaces[selectedSpace].Highlight(false);
            }

            //Debug.Log("Old selected space had "+listSelectedParts.Count + " parts in it.");

            // Tidy up the old vessel information
            if (null != this.vessel)
            {
                vessel.Clear();
            }
            this.vessel = null;

            // Build new vessel information
            this.vessel = new CLSVessel();
            this.vessel.Populate(newRootPart);

            // Now work out which space should be highlighted.
            this.selectedSpace = -1;
            foreach (CLSPart clsPart in this.vessel.Parts)
            {
                Part p = clsPart;

                //Debug.Log("New vessel contains part : " + p.flightID);

                if (listSelectedParts.Contains(p.flightID))
                {
                    //Debug.Log("Part " + p.partInfo.title + " was in the old selected space and is in the CLSVessel");
                    if (clsPart.Space != null)
                    {
                        // We have found the new space for a part that was in the old selected space.
                        this.selectedSpace = this.vessel.Spaces.IndexOf(clsPart.Space);
                        //Debug.Log("... it is also part of a space. We will use that space to be our new selected space. index:" + this.selectedSpace);
                        break;
                    }
                    else
                    {
                        //Debug.Log("it is no longer part of a space :(");
                    }
                }
            }

            if (this.selectedSpace != -1)
            {
                this.vessel.Spaces[this.selectedSpace].Highlight(true);
            }
            else
            {
                //Debug.Log("No space is selected after the rebuild.");
            }

            // Sanity check the selected space. If the CLSvessel has been rebuilt and there are no Spaces, or it references an out of range space then set it to -1

            if (vessel.Spaces.Count == 0 || vessel.Spaces.Count <= this.selectedSpace)
            {
                this.selectedSpace = -1;
            }
        }

        private void OnWindow(int windowID)
        {
            try
            {
                // Build a string descibing the contents of each of the spaces.
                if (null != this.vessel)
                {
                    GUILayout.BeginVertical();
                    allowUnrestrictedTransfers = GUILayout.Toggle(allowUnrestrictedTransfers, "Allow Crew Unrestricted Transfers");
                    String[] spaceNames = new String[vessel.Spaces.Count];
                    int counter = 0;
                    int newSelectedSpace = -1;

                    String partsList = "";
                    foreach (CLSSpace space in vessel.Spaces)
                    {
                        if (space.Name == "")
                        {
                            spaceNames[counter] = "Living Space " + (counter + 1).ToString();
                        }
                        else
                        {
                            spaceNames[counter] = space.Name;
                        }
                        counter++;
                    }

                    if (vessel.Spaces.Count > 0)
                    {
                        newSelectedSpace = GUILayout.SelectionGrid(this.selectedSpace, spaceNames, 1);
                    }

                    // If one of the spaces has been selected then display a list of parts that make it up and sort out the highlighting
                    if (-1 != newSelectedSpace)
                    {
                        // Only fiddle witht he highlighting is the selected space has actually changed
                        if (newSelectedSpace != this.selectedSpace)
                        {
                            // First unhighlight the space that was selected.
                            if (-1 != this.selectedSpace && this.selectedSpace < this.vessel.Spaces.Count)
                            {
                                vessel.Spaces[this.selectedSpace].Highlight(false);
                            }

                            // Update the space that has been selected.
                            this.selectedSpace = newSelectedSpace;

                            // Update the text in the Space edit box
                            this.spaceNameEditField = vessel.Spaces[this.selectedSpace].Name;

                            // Highlight the new space
                            vessel.Spaces[this.selectedSpace].Highlight(true);
                        }

                        // Loop through all the parts in the newly selected space and create a list of all the spaces in it.
                        foreach (CLSPart p in vessel.Spaces[this.selectedSpace].Parts)
                        {
                            Part part = (Part)p;
                            partsList += part.partInfo.title + "\n";
                        }

                        // Display the text box that allows the space name to be changed
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Name:");
                        this.spaceNameEditField = GUILayout.TextField(this.spaceNameEditField);
                        if (GUILayout.Button("Update"))
                        {
                            vessel.Spaces[this.selectedSpace].Name = this.spaceNameEditField;
                        }
                        GUILayout.EndHorizontal();

                        this.scrollViewer = GUILayout.BeginScrollView(this.scrollViewer, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                        GUILayout.BeginVertical();

                        // Display the crew capacity of the space.
                        GUILayout.Label("Crew Capacity: " + vessel.Spaces[this.selectedSpace].MaxCrew);

                        // And list the crew names
                        String crewList = "Crew Info:\n";

                        foreach (CLSKerbal crewMember in vessel.Spaces[this.selectedSpace].Crew)
                        {
                            crewList += ((ProtoCrewMember)crewMember).name + "\n";
                        }
                        GUILayout.Label(crewList);

                        // Display the list of component parts.
                        GUILayout.Label(partsList);

                        GUILayout.EndVertical();
                        GUILayout.EndScrollView();

                    }
                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.Label("No current vessel.");
                }

                GUI.DragWindow();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Update()
        {
            // Debug.Log("CLSAddon:Update");
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
                    if(FlightGlobals.ready)
                    {
                        CheckAndFixDockingHatchesInFlight();
                    }
                }

                // If we are in the editor, and there is a ship in the editor, then compare the number of parts to last time we did this. If it has changed then rebuild the CLSVessel
                if (HighLogic.LoadedSceneIsEditor)
                {
                    int currentPartCount = 0;
                    if (null == EditorLogic.startPod)
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
            if (this.stockToolbarButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
            }
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
            if (EditorLogic.startPod == null)
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

            if (HighLogic.LoadedSceneIsEditor && null != EditorLogic.startPod)
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
        private static void CrewTransfered(GameEvents.HostedFromToAction<ProtoCrewMember, Part> data)
        {
            try
            {
                if (allowUnrestrictedTransfers)
                {
                    // If transfers are not restricted then we have got nothing to do here.
                    return;
                }

                if (data.from.Modules.Cast<PartModule>().Any(x => x is KerbalEVA) ||
                    data.to.Modules.Cast<PartModule>().Any(x => x is KerbalEVA))
                {
                    // "Transfers" to/from EVA are always permitted.
                    // Trying to step them results in really bad things happening, and would be out of
                    // scope for this plugin anyway.
                    return;
                }

                if (null == Instance.Vessel)
                {
                    Instance.RebuildCLSVessel();
                }

                ICLSPart clsFrom = Instance.Vessel.Parts.Find(x => x.Part == data.from);
                ICLSPart clsTo = Instance.Vessel.Parts.Find(x => x.Part == data.to);

                if (clsFrom == null || clsTo == null || clsFrom.Space != clsTo.Space)
                {
                    data.to.RemoveCrewmember(data.host);
                    data.from.AddCrewmember(data.host);

                    var message = new ScreenMessage(string.Empty, 15f, ScreenMessageStyle.UPPER_CENTER);
                    ScreenMessages.PostScreenMessage(string.Format("<color=orange>{0} is unable to reach {1}.</color>", data.host.name, data.to.partInfo.title), message, true);

                    // Now try to remove the sucessful transfer message
                    // that stock displayed. 
                    var messages = FindObjectOfType<ScreenMessages>();

                    if (messages != null)
                    {
                        var messagesToRemove = messages.activeMessages.Where(x => x.startTime == message.startTime && x.style == ScreenMessageStyle.LOWER_CENTER).ToList();
                        foreach (var m in messagesToRemove)
                        {
                            ScreenMessages.RemoveMessage(m);
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
}
