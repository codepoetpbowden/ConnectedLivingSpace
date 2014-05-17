using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Toolbar;


namespace ConnectedLivingSpace
{
    [KSPAddonFixedCLS(KSPAddon.Startup.EveryScene, false, typeof(CLSAddon))]
    public class CLSAddon : MonoBehaviour , ICLSAddon
    {
        private static Rect windowPosition = new Rect(0,0,320,360);
        private static GUIStyle windowStyle = null;

        private Vector2 scrollViewer = Vector2.zero;
        private GUIDropdown spacesDropDown;

        private CLSVessel vessel = null;
        private int selectedSpace = -1;

        private IButton toolbarButton = null; // Toolbar button
        private bool visable = false;

        private int editorPartCount = 0; // This is horrible. Because there does not seem to be an obvious callback to sink when parts are added and removed in the editor, on each fixed update we will could the parts and if it has changed then rebuild the CLSVessel. Yuk!

        private int sanityCheckCounter = 0;
        private int sanityCheckFrequency = 100; // Change this to make the sanity checks more or less frequent.

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

            this.toolbarButton = ToolbarManager.Instance.add("ConnectedLivingSpace", "buttonCLS");
			this.toolbarButton.TexturePath = "ConnectedLivingSpace/assets/cls_icon_off";
            this.toolbarButton.ToolTip = "Connected Living Space";
            this.toolbarButton.OnClick += (e) => { OnToolbarButton_Click(); };
            this.toolbarButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH, GameScenes.FLIGHT);

            this.selectedSpace = -1;
        }

        public void Start() 
        {
            // Debug.Log("CLSAddon:Start");

            windowStyle = new GUIStyle(HighLogic.Skin.window);

            this.CreateSpacesDropDown(); // populates the drop down contol with a list of spaces.

            try
            {
                RenderingManager.RemoveFromPostDrawQueue(0, OnDraw);
            }
            catch (Exception ex)
            {
				Debug.LogException(ex);
            }

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                RenderingManager.AddToPostDrawQueue(0, OnDraw);
                GameEvents.onJointBreak.Add(OnJointBreak);
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
        }

        private void OnToolbarButton_Click()
        {
            //Debug.Log("OnToolbarButton_Click");

            // If the window is currently visible, set the selected space back to -1 so the highlighting is cleared.
            if (this.visable) 
            {
				if (null != this.vessel) 
                {
					vessel.Highlight (false);
				}
				this.selectedSpace = -1;
				this.toolbarButton.TexturePath = "ConnectedLivingSpace/assets/cls_icon_off";
			} 
            else 
            {
                this.toolbarButton.TexturePath = "ConnectedLivingSpace/assets/cls_icon_on";
			}

            this.visable = !this.visable;
        }

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
        private void OnJointBreak(EventReport eventReport)
        {
            //Debug.Log("CLSAddon::OnJointBreak");
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
                GUI.skin = HighLogic.Skin;

                windowPosition = GUILayout.Window(947695, windowPosition, OnWindow, "Connected Living Space", windowStyle,GUILayout.MinHeight(20));
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
                RebuildCLSVessel(EditorLogic.startPod);
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

            // Rebuild the drop down control used to select a space.
            this.CreateSpacesDropDown();
        }

        private void CreateSpacesDropDown()
        {
            List<GUIContent> names = new List<GUIContent>();

            if (null != this.vessel)
            {
                foreach (CLSSpace space in this.vessel.Spaces)
                {
                    names.Add(new GUIContent(space.Name));
                }
            }
            
            if(names.Count>0)
            {
                GUIStyle spaceListStyle = new GUIStyle() ;
                GUIContent buttonContent;
                spaceListStyle.normal.textColor = Color.white;
                spaceListStyle.onHover.background =
                spaceListStyle.hover.background = new Texture2D(2, 2);
                spaceListStyle.padding.left =
                spaceListStyle.padding.right =
                spaceListStyle.padding.top =
                spaceListStyle.padding.bottom = 1;

                //spaceListStyle.normal.background = new Texture2D(2, 2);
                /*
                {
                    Color bgColor = spaceListStyle.normal.background.GetPixel(0, 0);
                    bgColor = Color.cyan;
                    spaceListStyle.normal.background.SetPixel(0, 0, bgColor);
                }
                {
                    Color bgColor = spaceListStyle.normal.background.GetPixel(0, 1);
                    bgColor.a = 1;
                    spaceListStyle.normal.background.SetPixel(0, 1, bgColor);
                }
                {
                    Color bgColor = spaceListStyle.normal.background.GetPixel(1, 0);
                    bgColor.a = 1;
                    spaceListStyle.normal.background.SetPixel(1, 0, bgColor);
                }
                {
                    Color bgColor = spaceListStyle.normal.background.GetPixel(1, 1);
                    bgColor.a = 1;
                    spaceListStyle.normal.background.SetPixel(1, 1, bgColor);
                }
                spaceListStyle.onFocused.background = spaceListStyle.focused.background = spaceListStyle.onActive.background = spaceListStyle.active.background = spaceListStyle.onNormal.background = spaceListStyle.normal.background;
                */

                if (-1 == this.selectedSpace)
                {
                    buttonContent = new GUIContent("Select Space");
                }
                else
                {
                    buttonContent = names[this.selectedSpace];
                }
                this.spacesDropDown = new GUIDropdown(buttonContent, names.ToArray(), "button", "box", spaceListStyle);
            }
        }

        private void OnWindow(int windowID)
        {
            try
            {
                GUI.depth = -200;

                // Build a string descibing the contents of each of the spaces.
                if (null != this.vessel)
                {
                    GUILayout.BeginVertical();
                    
                    String[] spaceNames = new String[vessel.Spaces.Count];
                    int newSelectedSpace = -1;

                    String partsList = "";

                    Rect dropDownRect = new Rect();

                    if (vessel.Spaces.Count > 0)
                    {
                        if (-1 != this.selectedSpace)
                        {
                            this.spacesDropDown.SelectedItemIndex = this.selectedSpace;
                        }
                        dropDownRect = GUILayoutUtility.GetRect(150, 25);
                    }

                    // If one of the spaces has been selected then display a list of parts that make it up and sort out the highlighting
                    if (-1 != this.selectedSpace)
                    {
                        // Loop through all the parts in the newly selected space and create a list of all the spaces in it.
                        foreach (CLSPart p in vessel.Spaces[this.selectedSpace].Parts)
                        {
                            Part part = (Part)p;
                            partsList += part.partInfo.title + "\n";
                        }

                        // Display the text box that allows the space name to be changed
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Space Name:");
                        this.spaceNameEditField = GUILayout.TextField(this.spaceNameEditField);
                        if (GUILayout.Button("Update"))
                        {
                            vessel.Spaces[this.selectedSpace].Name = this.spaceNameEditField;
                            this.CreateSpacesDropDown();
                        }
                        GUILayout.EndHorizontal();

                        this.scrollViewer = GUILayout.BeginScrollView(this.scrollViewer,GUILayout.ExpandHeight(true),GUILayout.ExpandWidth(true));
                        GUILayout.BeginVertical();

                        // Display the crew capacity of the space.
                        GUILayout.Label("Crew Capacity: " + vessel.Spaces[this.selectedSpace].MaxCrew);

                        // And list the crew names
                        String crewList = "Crew Info:\n";

                        foreach(CLSKerbal crewMember in vessel.Spaces[this.selectedSpace].Crew)
                        {
                            crewList += ((ProtoCrewMember)crewMember).name +"\n";
                        }
                        GUILayout.Label(crewList);

                        // Display the list of component parts.
                        GUILayout.Label(partsList);

                        GUILayout.EndVertical();
                        GUILayout.EndScrollView();

                    }
                    GUILayout.EndVertical();

                    // finally - go back and draw in the dropdown control on top of everything else
                    if (vessel.Spaces.Count > 0)
                    {
                        GUI.depth = -250;

                        newSelectedSpace = this.spacesDropDown.Show(dropDownRect);
                        // Only fiddle with the highlighting is the selected space has actually changed
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

                        GUI.depth = 200;
                    }
                }
                else
                {
                    Debug.LogError("this.vessel was null");
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

                // Add the ModuleDockingHatch to all the Docking Nodes
                AddHatchModuleToParts();

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
                else if (HighLogic.LoadedSceneIsFlight)
                {
                    // In flight, run the sanity checker.
                    if (FlightGlobals.ready)
                    {
                        // Do not run the sanity checker if the CLSVessel (and hence all the CLS parts) has not yet been constructed.
                        if (null != this.vessel)
                        {
                            // Only run the sanity check every now and again!
                            this.sanityCheckCounter++;
                            this.sanityCheckCounter = this.sanityCheckCounter % this.sanityCheckFrequency;

                            // Debug.Log("sanityCheckCounter: " + sanityCheckCounter);

                            if (1 == this.sanityCheckCounter) // but running the checker when the counter is one, we know that we can force the check on the next physics frame by setting it to 0.
                            {
                                this.SanityCheck();
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

        public void OnDestroy()
        {
            //Debug.Log("CLSAddon::OnDestroy");
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onJointBreak.Remove(OnJointBreak);
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

            // Remove the toolbar button

            this.toolbarButton.Destroy();
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

        // Method to add Docking Hatches to all pars that have Dockong Nodes
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

        // Utility method that is run every now an again and just checks that everything is in sync and makes sense. The actualt funtionailty in a method on the module class.
        private void SanityCheck()
        {
            foreach(Part p in FlightGlobals.ActiveVessel.Parts)
            {
                foreach (ModuleConnectedLivingSpace clsmod in p.Modules.OfType<ModuleConnectedLivingSpace>())
                {
                    //clsmod.SanityCheck();
                }
            }
        }
    }
}
