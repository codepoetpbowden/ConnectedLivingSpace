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

        public void Awake() 
        {
            Debug.Log("CLSAddon:Awake");

            
        }

        public void Start() 
        {
            Debug.Log("CLSAddon:Start");

            windowStyle = new GUIStyle(HighLogic.Skin.window);


            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                RenderingManager.AddToPostDrawQueue(0, OnDraw);
            }
            else
            {
                RenderingManager.RemoveFromPostDrawQueue(0, OnDraw);
            }
        }

        private void OnDraw()
        {
            windowPosition = GUI.Window(1234, windowPosition, OnWindow, "Connected Living Space", windowStyle);
        }


        

        private void OnWindow(int windowID)
        {
            try
            {
                if (GUILayout.Button("Process"))
                {
                    // Do something as they pressed the button!
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
                }

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

                    this.selectedSpace = GUILayout.SelectionGrid(this.selectedSpace, spaceNames, counter,GUILayout.ExpandHeight(true));

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

            // Walk the part tree, and build up a list of living spaces.
        }


        public static string DumpConfigNode(ConfigNode cn)
        {
            string output = "Confignode name:" + cn.name + " id:" + cn.id + "\n{";

            foreach (ConfigNode child in cn.nodes)
            {
                output += DumpConfigNode(child);
            }

            foreach (String name in cn.values.DistinctNames())
            {
                foreach (String value in cn.values.GetValues(name))
                {
                    output += name + ":"+value+"\n";
                }
            }
            output += "}\n";

            return output;
        }
    }
}
