using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConnectedLivingSpace
{
    [KSPAddonFixed(KSPAddon.Startup.EveryScene, false, typeof(CLSAddon))]
    public class CLSAddon : MonoBehaviour
    {
        private static Rect windowPosition = new Rect(0,0,320,360);
        private static GUIStyle windowStyle = null;

        private CLSVessel vessel = null;
        private int selectedSpace = -1;

        private int editorPartCount = 0; // This is horrible. Because there does not seem to be an obvious callback to sink when parts are added and removed in the editor, on each fixed update we will could the parts and if it has changed then rebuild the CLSVessel. Yuk!

        public void Awake() 
        {
            // Debug.Log("CLSAddon:Awake");
        }

        public void Start() 
        {
            // Debug.Log("CLSAddon:Start");

            windowStyle = new GUIStyle(HighLogic.Skin.window);

            try
            {
                RenderingManager.RemoveFromPostDrawQueue(0, OnDraw);
            }
            catch (Exception ex)
            {
                // TODO ought we do something with the exception?
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
            }
        }


        private void OnVesselLoaded(Vessel data)
        {
            Debug.Log("CLSAddon::OnVesselLoaded");
            RebuildCLSVessel();
        }
        private void OnVesselTerminated(ProtoVessel data)
        {
            Debug.Log("CLSAddon::OnVesselTerminated");
        }
        private void OnJointBreak(EventReport eventReport)
        {
            Debug.Log("CLSAddon::OnJointBreak");
        }
        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        {
            Debug.Log("CLSAddon::OnPartAttach"); 
        }
        private void OnPartCouple(GameEvents.FromToAction <Part, Part> data)
        {
            Debug.Log("CLSAddon::OnPartCouple");
        }
        private void OnPartDie(Part data)
        {
            Debug.Log("CLSAddon::OnPartDie");
        }
        private void OnPartExplode(GameEvents.ExplosionReaction data)
        {
            Debug.Log("CLSAddon::OnPartExplode");
        }
        private void OnPartRemove(GameEvents.HostTargetAction<Part, Part> data)
        {
            Debug.Log("CLSAddon::OnPartRemove");
        }
        private void OnPartUndock(Part data)
        {
            Debug.Log("CLSAddon::OnPartUndock");
        }
        private void OnStageSeparation(EventReport eventReport)
        {
            Debug.Log("CLSAddon::OnStageSeparation");
        }
        private void OnUndock(EventReport eventReport)
        {
            Debug.Log("CLSAddon::OnUndock");
        }
        private void OnVesselDestroy(Vessel data)
        {
            Debug.Log("CLSAddon::OnVesselDestroy");
        }
        private void OnVesselCreate(Vessel data)
        {
            Debug.Log("CLSAddon::OnVesselCreate");
        }
        private void OnVesselWasModified(Vessel data)
        {
            Debug.Log("CLSAddon::OnVesselWasModified");
            RebuildCLSVessel();
        }
        private void OnVesselChange(Vessel data)
        {
            Debug.Log("CLSAddon::OnVesselChange");
        }



        private void OnDraw()
        {
            windowPosition = GUI.Window(1234, windowPosition, OnWindow, "Connected Living Space", windowStyle);
        }

        private void RebuildCLSVessel()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                this.vessel = new CLSVessel();
                this.vessel.Populate(FlightGlobals.ActiveVessel);
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                this.vessel = new CLSVessel();
                this.vessel.Populate(EditorLogic.startPod);
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
                    String[] spaceNames = new String[vessel.Spaces.Count];
                    int counter = 0;

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

                    this.selectedSpace = GUILayout.SelectionGrid(this.selectedSpace, spaceNames, counter);

                    // If one of the spaces has been selected then display a list of parts that make it up
                    if (-1 != this.selectedSpace)
                    {
                        List<Part> parts;
                        if (HighLogic.LoadedSceneIsEditor) { parts = EditorLogic.SortedShipList; }
                        else { parts = FlightGlobals.ActiveVessel.Parts;}
                        
                        // First unhightlight all the parts in the vessel
                        foreach (Part p in parts)
                        {
                            p.SetHighlightDefault();
                            p.SetHighlight(false);
                        }

                        foreach (CLSPart p in vessel.Spaces[this.selectedSpace].Parts)
                        {
                            Part part = (Part)p;
                            part.SetHighlightDefault();
                            part.SetHighlightColor(Color.magenta);
                            part.SetHighlight(true);
                            
                            partsList += part.partInfo.title + "\n";
                        }

                        // Display the text box that allows the space name to be changed
                        vessel.Spaces[this.selectedSpace].Name = GUILayout.TextField(vessel.Spaces[this.selectedSpace].Name);

                        // Display the crew capacity of the space.
                        GUILayout.Label("Crew Capacity: " + vessel.Spaces[this.selectedSpace].MaxCrew);

                        // Display the list of component parts.
                        GUILayout.Label(partsList);
                    }
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
            // Debug.Log("CLSAddon:FixedUpdate");

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
                    this.RebuildCLSVessel();
                    this.editorPartCount = currentPartCount;
                }
            }
        }

        public void OnDestroy()
        {
            Debug.Log("CLSAddon::OnDestroy");
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
        }


    }
}
