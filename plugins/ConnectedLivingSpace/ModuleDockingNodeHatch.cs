using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace ConnectedLivingSpace
{
    // Module that is added to every part with a ModuleDockingPort with one per ModuleDockingPort. It will add the functionality of a closable hatch to each docking port.
    public class ModuleDockingNodeHatch : ModuleDockingNode
    {
        [KSPField(isPersistant = true)]
        private bool hatchOpen;

        public bool HatchOpen
        {
            get
            {
                return this.hatchOpen;
            }
        }

        [KSPField(isPersistant=false, guiActive = true, guiName = "Hatch status")]
        public string HatchStatus
        {
            get
            {
                if (hatchOpen)
                {
                    return "Open";
                }
                else
                {
                    return "Closed";
                }
            }
        }


        [KSPEvent(active = true, guiActive = true, guiName = "Open Hatch")]
        private void OpenHatch()
        {
            bool docked = isInDockedState();

            if (docked)
            {
                this.hatchOpen = true;
                this.Events["CloseHatch"].active = true;
                this.Events["OpenHatch"].active = false;
            }
            else
            {
                this.hatchOpen = false;
                this.Events["CloseHatch"].active = false;
                this.Events["OpenHatch"].active = false;
            }

            // Finally fire the VesselChange event to cause the CLSAddon to re-evaluate everything. ActiveVEssel is only available in flight, but then it should only be possible to open and close hatches in flight so we should be OK.
            GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
        }

        [KSPEvent(active = true, guiActive = true, guiName = "Close Hatch")]
        private void CloseHatch()
        {
            bool docked = isInDockedState();
            
            this.hatchOpen = false;

            this.Events["CloseHatch"].active = false;
            if (isInDockedState())
            {
                this.Events["OpenHatch"].active = true;
            }
            else
            {
                this.Events["OpenHatch"].active = false;
            }

            // Finally fire the VesselChange event to cause the CLSAddon to re-evaluate everything. ActiveVEssel is only available in flight, but then it should only be possible to open and close hatches in flight so we should be OK.
            GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
        }

        public override void OnLoad(ConfigNode node)
        {
            // Call the base class
            base.OnLoad(node);

            // Set the GUI state of the open/close hatch events as appropriate
            if (isInDockedState())
            {
                if (hatchOpen)
                {
                    this.Events["CloseHatch"].active = true;
                    this.Events["OpenHatch"].active = false;
                }
                else
                {
                    this.Events["CloseHatch"].active = false;
                    this.Events["OpenHatch"].active = true;
                }
            }
            else
            {
                this.Events["CloseHatch"].active = false;
                this.Events["OpenHatch"].active = false; 
            }
        }

        // Called every physics frame. Make sure that the menu options are valid for the state that we are in. 
        private void FixedUpdate()
        {
            // Call the base class implimentation (Calling some reflection dark magic)
            CallModuleDockingNodeFixedUpdate(this);

            if (isInDockedState())
            {
                if (!hatchOpen)
                {
                    // We are docked, but the hatch is closed. Make sure that it is possible to open the hatch
                    this.Events["CloseHatch"].active = false;
                    this.Events["OpenHatch"].active = true; 
                }
            }
            else
            {
                // We are not docked - close up the hatch if it is open!
                if (this.hatchOpen)
                {
                    this.hatchOpen = false;
                    this.Events["CloseHatch"].active = false;
                    this.Events["OpenHatch"].active = false; 
                }
            }
        }

        public override void OnStart(PartModule.StartState st)
        {
            Debug.Log("ModuleDockingNodeHatch::OnStart");

            base.OnStart(st);

            // As long as we have not started in the editor, ensure the module is active / enabled.
            if (st != StartState.Editor)
            {
                Debug.Log("ModuleDockingNodeHatch::OnStart setting enabled = true");
                this.enabled = true;
            }
        }

        // tries to work out if the docking port is docked based on the state
        private bool isInDockedState()
        {
            if (this.state == "Docked (dockee)" || this.state == "Docked (docker)")
            {
                return true;
            }
            return false;
        }

        //This method uses reflection to call the FixedUpdate private method in ModuleDockingNode. 
        public static void CallModuleDockingNodeFixedUpdate(ModuleDockingNode dockNode)
        {
            object[] paramList = new object[] { };
            MethodInfo FixedUpdateMethod = typeof(ModuleDockingNode).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

            if (FixedUpdateMethod == null)
            {
                Debug.Log("Failed to get ModuleDockingNode::FixedUpdate");
            }
            FixedUpdateMethod.Invoke(dockNode, paramList);
        }
    }
}
