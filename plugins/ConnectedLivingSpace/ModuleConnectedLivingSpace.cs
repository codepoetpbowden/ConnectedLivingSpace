using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConnectedLivingSpace
{
    // A module that can be added to a part in order to be able to set and read in part specific config that relates to ConnectedLivingSpace
    public class ModuleConnectedLivingSpace : PartModule
    {


        internal CLSPart clsPart; // reference back to the CLS Part that refers to the part that this is a module on. Tghis value might well be null, but the CLSPart will attempt to set it when the CLS part is created.

        [KSPField]
        public bool passable = false;
        [KSPField]
        public string passablenodes = "";
        [KSPField]
        public string impassablenodes = "";
        [KSPField(isPersistant = true)]
        public string spaceName;
        [KSPField(isPersistant = true)]
        private int savedHatchStatus = (int)DockingPortHatchStatus.DOCKING_PORT_HATCH_CLOSED;// Default to closed hatches. Id the part has been saved this will be overritten when it is loaded. If the part is not saved, and it is not a docking port, then it will be overritten in OnStart.
        internal bool isDockingPort = false; // indicates if this part has docking port functionality. it will be used to determine if to allow open/close hatch funationality

        public DockingPortHatchStatus hatchStatus
        {
            get
            {
                return (DockingPortHatchStatus)savedHatchStatus;
            }

            set
            {
                this.savedHatchStatus = (int)value;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Open Hatch", active = false)]
        private void OpenHatch()
        {
            if (!this.isDockingPort)
            {
                this.hatchStatus = DockingPortHatchStatus.NOT_DOCKING_PORT;
                this.clsPart.HatchStatus = this.hatchStatus;
                Events["OpenHatch"].active = false;
                Events["CloseHatch"].active = false;
            }
            else
            {
                // Is this a docked part? If not then just disable both the open and close KSPEvents and leave it at that.
                if (this.clsPart.Docked)
                {
                    this.hatchStatus = DockingPortHatchStatus.DOCKING_PORT_HATCH_OPEN;
                    this.clsPart.HatchStatus = this.hatchStatus;
                    Events["OpenHatch"].active = false;
                    Events["CloseHatch"].active = true;
                }
                else
                {
                    this.hatchStatus = DockingPortHatchStatus.DOCKING_PORT_HATCH_CLOSED;
                    this.clsPart.HatchStatus = this.hatchStatus;
                    Events["OpenHatch"].active = false;
                    Events["CloseHatch"].active = false;
                }
            }

            // Finally fire the VesselChange event to cause the CLSAddon to re-evaluate everything. ActiveVEssel is only available in flight, but then it should only be possible to open and close hatches in flight so we should be OK.
            GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
        }


        [KSPEvent(guiActive = true, guiName = "Close Hatch", active = false)]
        private void CloseHatch()
        {
            if (!this.isDockingPort)
            {
                this.hatchStatus = DockingPortHatchStatus.NOT_DOCKING_PORT;
                this.clsPart.HatchStatus = this.hatchStatus;
                Events["OpenHatch"].active = false;
                Events["CloseHatch"].active = false;
            }
            else
            {
                // Is this a docked part? If not then just disable both the open and close KSPEvents and leave it at that.
                if (this.clsPart.Docked)
                {
                    this.hatchStatus = DockingPortHatchStatus.DOCKING_PORT_HATCH_CLOSED;
                    this.clsPart.HatchStatus = this.hatchStatus;
                    Events["OpenHatch"].active = true;
                    Events["CloseHatch"].active = false;
                }
                else
                {
                    this.hatchStatus = DockingPortHatchStatus.DOCKING_PORT_HATCH_CLOSED;
                    this.clsPart.HatchStatus = this.hatchStatus;
                    Events["OpenHatch"].active = false;
                    Events["CloseHatch"].active = false;
                }
            }

            // Finally fire the VesselChange event to cause the CLSAddon to re-evaluate everything. ActiveVEssel is only available in flight, but then it should only be possible to open and close hatches in flight so we should be OK.
            GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
        }
 
        public override void OnAwake()
        {
            try
            {               

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Called during the Part startup.
        /// StartState gives flag values of initial state
        /// </summary>
        public override void OnStart(StartState state)
        {
            //Debug.Log("CLS::OnStart state=" + state.ToString());

            try
            {
                // If the CLS Space name for this part is not set or empty, then set it to the title of this part.
                if (null == this.spaceName)
                {
                    this.spaceName = this.part.partInfo.title;
                }
                else if ("" == this.spaceName)
                {
                    this.spaceName = this.part.partInfo.title;
                }

                // Is this part a docking Port? check to see if it has a ModuleDockingPort
                this.isDockingPort = this.part.Modules.Contains("ModuleDockingNode");
                if (false == this.isDockingPort)
                {
                    // As this is not a docking port, set the hatch status appropriately.
                    this.hatchStatus = DockingPortHatchStatus.NOT_DOCKING_PORT;
                    Events["OpenHatch"].active = false;
                    Events["CloseHatch"].active = false;
                }
                else
                {
                    // The part is a docking port. Is it possible to figure out if it is docked or not?
                    if (null != this.clsPart)
                    {
                        if (this.clsPart.Docked)
                        {
                            if (this.hatchStatus == DockingPortHatchStatus.DOCKING_PORT_HATCH_OPEN)
                            {
                                Events["OpenHatch"].active = false;
                                Events["CloseHatch"].active = true;
                            }
                            else
                            {
                                Events["OpenHatch"].active = true;
                                Events["CloseHatch"].active = false;
                            }
                            this.clsPart.HatchStatus = this.hatchStatus;
                        }
                        else
                        {
                            // Docking port is not docked. ensure that the hatch is closed, and hide the option to open it.
                            this.clsPart.HatchStatus = DockingPortHatchStatus.DOCKING_PORT_HATCH_CLOSED;
                            this.hatchStatus = DockingPortHatchStatus.DOCKING_PORT_HATCH_CLOSED;
                            Events["OpenHatch"].active = false;
                            Events["CloseHatch"].active = false;
                        }
                    }
                    else
                    {
                        // We can't tell if the docking port is docked or not. This is not ideal so log a warning, so we can find a way of avoiding this scenerio.
                        //Debug.LogWarning("Unable to tell if docking port is docked or not.");
                        if (this.hatchStatus == DockingPortHatchStatus.DOCKING_PORT_HATCH_OPEN)
                        {
                            Events["OpenHatch"].active = false;
                            Events["CloseHatch"].active = true;
                        }
                        else
                        {
                            Events["OpenHatch"].active = true;
                            Events["CloseHatch"].active = false;
                        }                    
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // Intended to be called by CLSPart. Therefore we do not bother to try to update the CLSPart, as it is assumed it has already been handled.
        internal void SetHatchStatus(DockingPortHatchStatus newStatus)
        {
            if (newStatus == DockingPortHatchStatus.NOT_DOCKING_PORT)
            {
                Events["OpenHatch"].active = false;
                Events["CloseHatch"].active = false;
                this.hatchStatus = newStatus;
            }
            else if (newStatus == DockingPortHatchStatus.DOCKING_PORT_HATCH_OPEN)
            {
                Events["OpenHatch"].active = false;
                Events["CloseHatch"].active = true;
                this.hatchStatus = newStatus;
            }
            else if (newStatus == DockingPortHatchStatus.DOCKING_PORT_HATCH_CLOSED)
            {
                Events["OpenHatch"].active = true;
                Events["CloseHatch"].active = false;
                this.hatchStatus = newStatus;
            }
        }

        /// <summary>
        /// Called when PartModule is asked to save its values.
        /// Can save additional data here.
        /// </summary>
        /// <param name='node'>The node to save in to</param>
        public override void OnSave(ConfigNode node)
        {

        }

        /// <summary>
        /// Called when PartModule is asked to load its values.
        /// Can load additional data here.
        /// </summary>
        /// <param name='node'>The node to load from</param>
        public override void OnLoad(ConfigNode node)
        {

        }

        // Allow a CLSPart to be cast into a ModuleConnectedLivingSpace. Note that this might fail, if the part in question does not have the CLS module configured.
        public static implicit operator ModuleConnectedLivingSpace(Part _p)
        {
            foreach (PartModule pm in _p.Modules)
            {
                if (pm.moduleName == "ModuleConnectedLivingSpace")
                {
                    // This part does have a CLSmodule
                    ModuleConnectedLivingSpace CLSMod = (ModuleConnectedLivingSpace)pm;

                    return (CLSMod);
                }
            }
            return null;
        }

        // Method to provide extra infomation about the part on response to the RMB
        public override string GetInfo()
        {
            String returnValue = String.Empty;

            if (this.part.CrewCapacity > 0)
            {
                returnValue = "Kerbals are able to stay in this part ";

                if (this.passable)
                {
                    returnValue += "and can pass into it from any attachment node.";
                }
                else
                {
                    if (this.impassablenodes != "")
                    {
                        returnValue += "but can not get access to it through the nodes " + this.impassablenodes;
                    }
                    else
                    {
                        returnValue += "and can pass into it from any attachment node."; 
                    }
                }
            }
            else
            {
                returnValue = "Kerbals are not able to stay in this part ";

                if (this.passable)
                {
                    returnValue += "but can pass through it from any attachment node.";
                }
                else
                {
                    if (this.impassablenodes != "")
                    {
                        returnValue += " but can pass through it through on all nodes except for " + this.impassablenodes;
                    }
                    else
                    {
                        returnValue += "but can pass through it from any attachment node.";
                    }
                }
            }

            return returnValue;
        }
    }
}
