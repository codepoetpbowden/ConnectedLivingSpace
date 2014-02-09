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
        private static Rect windowPosition = new Rect(0,0,320,240);
        private static GUIStyle windowStyle = null;
        private static bool buttonState = false;

        private CLSVessel vessel = null;

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
                GUILayout.BeginHorizontal();
                GUILayout.Label("ABC-");
                GUILayout.Label("123");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                buttonState = GUILayout.Toggle(buttonState, "Button State: " + buttonState);
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Process"))
                {
                    // Do something as they pressed the button!
                    this.vessel = new CLSVessel();
                    this.vessel.Populate(FlightGlobals.ActiveVessel);
                }

                // Build a string descibing the contents of each of the spaces.
                if (null != this.vessel)
                {
                    String output = "";
                    foreach (CLSSpace space in vessel.Spaces)
                    {
                        output += "Space\n{\n";
                        foreach (CLSPart p in space.Parts)
                        {
                            output += " " + ((Part)p).name + "\n";
                        }
                        output += "}\n";
                    }

                    Debug.Log(output);

                    GUILayout.Label(output);
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

    }
}
