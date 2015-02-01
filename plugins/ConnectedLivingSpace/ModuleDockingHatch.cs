using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace ConnectedLivingSpace
{
    // This module will be added at runtime to any part that also has a ModuleDockingNode. There will be a one to one relationship between ModuleDockingHatch and ModuleDockingNode
    public class ModuleDockingHatch : PartModule, IModuleDockingHatch
    {
        [KSPField(isPersistant = true)]
        private bool hatchOpen;

        [KSPField(isPersistant = true)]
        internal string docNodeAttachmentNodeName;
        [KSPField(isPersistant = true)]
        internal string docNodeTransformName;
        internal ModuleDockingNode modDockNode;

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

        public string HatchStatus
        { 
            get
            { 
                return this.hatchStatus; 
            }
        }

        public bool IsDocked
        { 
            get
            {
                return isInDockedState() || isAttachedToDockingPort(); 
            }
        }

        public BaseEventList HatchEvents
        {
            get
            {
                return this.Events;
            }
        }

        public ModuleDockingNode ModDockNode
        {
            get
            {
                return this.modDockNode;
            }
        }

        [KSPField(isPersistant = false, guiActive = true, guiName = "Hatch status")]
        private string hatchStatus = "";

        [KSPEvent(active = true, guiActive = true, guiName = "Open Hatch")]
        public void OpenHatch()
        {
            this.Events["OpenHatch"].active = false;
            if (isInDockedState() || isAttachedToDockingPort())
            {
                this.HatchOpen = true;
                this.Events["CloseHatch"].active = true;
            }
            else
            {
                this.HatchOpen = false;
                this.Events["CloseHatch"].active = false;               
            }

            // Finally fire the VesselChange event to cause the CLSAddon to re-evaluate everything. ActiveVEssel is only available in flight, but then it should only be possible to open and close hatches in flight so we should be OK.
            GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
        }

        [KSPEvent(active = true, guiActive = true, guiName = "Close Hatch")]
        public void CloseHatch()
        {
            bool docked = isInDockedState();
            
            this.HatchOpen = false;

            this.Events["CloseHatch"].active = false;
            if (isInDockedState() || isAttachedToDockingPort())
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
            //Debug.Log("ModuleDockingHatch::OnLoad");
            //Debug.Log("this.docNodeAttachmentNodeName: " + this.docNodeAttachmentNodeName);
            //Debug.Log("this.docNodeTransformName: " + this.docNodeTransformName);
            //Debug.Log("node.GetValue(docNodeTransformName): " + node.GetValue("docNodeTransformName"));
            //Debug.Log("node.GetValue(docNodeAttachmentNodeName): " + node.GetValue("docNodeAttachmentNodeName"));
 
            // The Loader with have set hatchOpen, but not via the Property HatchOpen, so we need to re-do it to ensure that hatchStatus gets properly set.
            this.HatchOpen = this.hatchOpen;

            // Set the GUI state of the open/close hatch events as appropriate
            if (isInDockedState() || isAttachedToDockingPort())
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
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (FlightGlobals.ready)
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
                        if (isAttachedToDockingPort())
                        {
                            if (!this.HatchOpen)
                            {
                                // We are not docked, but attached to a docking port, and the hatch is closed. Make sure that it is possible to open the hatch
                                this.Events["CloseHatch"].active = false;
                                this.Events["OpenHatch"].active = true;
                            }
                            else
                            {
                                // We are not docked, but attached to a docking port, and the hatch is open. Make sure that it is possible to close the hatch
                                this.Events["CloseHatch"].active = true;
                                this.Events["OpenHatch"].active = false;
                            }
                        }
                        else
                        {
                            // We are not docked or attached to a docking port - close up the hatch if it is open!
                            if (this.HatchOpen)
                            {
                                Debug.Log("Closing a hatch because its corresponding docking port is in state: " + this.modDockNode.state);

                                this.HatchOpen = false;
                                this.Events["CloseHatch"].active = false;
                                this.Events["OpenHatch"].active = false;
                            }
                        }
                    }
                }
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                // In the editor force the hatches open for attached docking ports so it is possible to see the living spaces at design time.
                if (isAttachedToDockingPort())
                {
                    this.HatchOpen = true;
                }
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
            // First ensure that we know which ModuleDockingNode we are referring to.
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

        // tries to work out if the docking port is attached to another docking port (ie in the VAB) and therefore can be treated as if it is docked (for example by not requiring the hatch to be closed)
        private bool isAttachedToDockingPort()
        {
            // First - this is only possible if we have an reference attachmentNode
            if (this.docNodeAttachmentNodeName != null && this.docNodeAttachmentNodeName != "" && docNodeAttachmentNodeName != string.Empty)
            {
                AttachNode thisNode = this.part.attachNodes.Find(x => x.id == this.docNodeAttachmentNodeName);
                if (null != thisNode)
                {
                    Part attachedPart = thisNode.attachedPart;
                    if (null != attachedPart)
                    {
                        // What is the attachNode in the attachedPart that links back to us?
                        AttachNode reverseNode = attachedPart.findAttachNodeByPart(this.part);
                        if (null != reverseNode)
                        {
                            // Now the big question - is the attached part a docking node that is centred on the reverseNode?
                            foreach (ModuleDockingNode n in attachedPart.Modules.OfType<ModuleDockingNode>())
                            {
                                if (n.referenceAttachNode == reverseNode.id)
                                {
                                    // The part has a docking node that references the attachnode that connects back to our part - this is what we have been looking for!
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        // Method that can be used to set up the ModuleDockingNode that this ModuleDockingHatch refers to.
        public void AttachModuleDockingNode(ModuleDockingNode _modDocNode)
        {
            this.modDockNode = _modDocNode;

            this.docNodeTransformName = _modDocNode.nodeTransformName;
            this.docNodeAttachmentNodeName = _modDocNode.referenceAttachNode;
        }
    }
}
