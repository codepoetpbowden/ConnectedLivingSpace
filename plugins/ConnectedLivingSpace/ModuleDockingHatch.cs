using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace ConnectedLivingSpace
{
    // This module will be added at runtime to any part that also has a ModuleDockingNode. There will be a one to one relationship between ModuleDockingHatch and ModuleDockingNode
    public class ModuleDockingHatch : PartModule
    {
        [KSPField(isPersistant = true)]
        private bool hatchOpen;

        [KSPField(isPersistant = true)]
        internal string docNodeAttachmentNodeName;
        [KSPField(isPersistant = true)]
        internal string docNodeTransformName;
        internal  ModuleDockingNode modDockNode;

        public bool HatchOpen
        {
            get
            {
                return this.hatchOpen;
            }

            set
            {
                this.hatchOpen = value;

                if (value)
                {
                    this.hatchStatus = "Open";
                }
                else
                {
                    this.hatchStatus = "Closed";
                }
            }
        }

        [KSPField(isPersistant = false, guiActive = true, guiName = "Hatch status")]
        private string hatchStatus = "";

        [KSPEvent(active = true, guiActive = true, guiName = "Open Hatch")]
        private void OpenHatch()
        {
            bool docked = isInDockedState();

            if (docked)
            {
                this.HatchOpen = true;
                this.Events["CloseHatch"].active = true;
                this.Events["OpenHatch"].active = false;
            }
            else
            {
                this.HatchOpen = false;
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
            
            this.HatchOpen = false;

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
            //Debug.Log("this.docNodeAttachmentNodeName: " + this.docNodeAttachmentNodeName);
            //Debug.Log("this.docNodeTransformName: " + this.docNodeTransformName);
            //Debug.Log("node.GetValue(docNodeTransformName): " + node.GetValue("docNodeTransformName"));
            //Debug.Log("node.GetValue(docNodeAttachmentNodeName): " + node.GetValue("docNodeAttachmentNodeName"));
 
            // The Loader with have set hatchOpen, but not via the Property HatchOpen, so we need to re-do it to ensure that hatchStatus gets properly set.
            this.HatchOpen = this.hatchOpen;

            // Set the GUI state of the open/close hatch events as appropriate
            if (isInDockedState())
            {
                if (this.HatchOpen)
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
            if (isInDockedState())
            {
                if (!this.HatchOpen)
                {
                    // We are docked, but the hatch is closed. Make sure that it is possible to open the hatch
                    this.Events["CloseHatch"].active = false;
                    this.Events["OpenHatch"].active = true; 
                }
            }
            else
            {
                // We are not docked - close up the hatch if it is open!
                if (this.HatchOpen)
                {
                    this.HatchOpen = false;
                    this.Events["CloseHatch"].active = false;
                    this.Events["OpenHatch"].active = false; 
                }
            }
        }

        // TODO is this necassery now that we ar eusing FixedUpdate and no OnFixedUpdate?
        public override void OnStart(PartModule.StartState st)
        {
            //Debug.Log("ModuleDockingNodeHatch::OnStart");

            // As long as we have not started in the editor, ensure the module is active / enabled.
            if (st != StartState.Editor)
            {
                //Debug.Log("ModuleDockingNodeHatch::OnStart setting enabled = true");
                this.enabled = true;
            }
        }

        private bool CheckModuleDockingNode()
        {
            if (null == this.modDockNode)
            {
                // We do not know which ModuleDockingNode we are attached to yet. Try to find one.
                foreach (ModuleDockingNode dockNode in this.part.Modules.OfType<ModuleDockingNode>())
                {
                    if (IsRelatedDockingNode(dockNode))
                    {
                        this.modDockNode = dockNode;
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        // This method allows us to check if a specified ModuleDockingNode is one that this hatch is attached to
        internal bool IsRelatedDockingNode(ModuleDockingNode dockNode)
        {
            if (dockNode.nodeTransformName == this.docNodeTransformName)
            {
                this.modDockNode = dockNode;
                return true;
            }
            if (dockNode.referenceAttachNode == this.docNodeAttachmentNodeName)
            {
                this.modDockNode = dockNode;
                return true;
            }
            return false;
        }

        // tries to work out if the docking port is docked based on the state
        private bool isInDockedState()
        {
            // First ensure that we know which ModuleDockingNode we are reffering to.
            if (CheckModuleDockingNode())
            {
                if (this.modDockNode.state == "Docked (dockee)" || this.modDockNode.state == "Docked (docker)")
                {
                    return true;
                }
            }
            else
            {
                // This is bad - it means there is a hatch that we can not match to a docking node. This should not happen. We will log an error but it will likely spam the log.
                Debug.LogError(" Error - Docking port hatch can not find its ModuleDockingNode docNodeTransformName:" + this.docNodeTransformName + " docNodeAttachmentNodeName " + this.docNodeAttachmentNodeName);
            }

            return false;
        }

        // Method that cna be used to set up the ModuleDockingNode that this ModuleDockingHatch reffers to.
        public void AttachModuleDockingNode(ModuleDockingNode _modDocNode)
        {
            this.modDockNode = _modDocNode;

            this.docNodeTransformName = _modDocNode.nodeTransformName;
            this.docNodeAttachmentNodeName = _modDocNode.referenceAttachNode;
        }
    }
}
