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

        [KSPField(isPersistant = true)]
        public bool passable = false;
        [KSPField(isPersistant = true)]
        public bool passableWhenSurfaceAttached = false;
        [KSPField(isPersistant = true)]
        public bool surfaceAttachmentsPassable = false;
        [KSPField(isPersistant = true)]
        public string passablenodes = "";
        [KSPField(isPersistant = true)]
        public string impassablenodes = "";
        [KSPField]
        public string impassableDockingNodeTypes = "";
        [KSPField]
        public string passableDockingNodeTypes = "";
        [KSPField(isPersistant = true)]
        public string spaceName;

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
                SetEventState();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Called when PartModule is asked to save its values.
        /// Can save additional data here.
        /// </summary>
        /// <param name='node'>The node to save in to</param>
        public override void OnSave(ConfigNode node)
        {
            //node.AddValue("passable", passable);
            //node.AddValue("passableWhenSurfaceAttached", passableWhenSurfaceAttached);
            //node.AddValue("surfaceAttachmentsPassable", surfaceAttachmentsPassable);
            //node.AddValue("passablenodes", passablenodes);
            //node.AddValue("impassablenodes", passable);
            //node.AddValue("spaceName", spaceName);
        }

        /// <summary>
        /// Called when PartModule is asked to load its values.
        /// Can load additional data here.
        /// </summary>
        /// <param name='node'>The node to load from</param>
        public override void OnLoad(ConfigNode node)
        {
            SetEventState();
        }

        private void SetEventState()
        {
            if (this.passable)
            {
                Events["EnablePassable"].active = false;
                Events["DisablePassable"].active = true;
            }
            else
            {
                Events["EnablePassable"].active = true;
                Events["DisablePassable"].active = false;
            }
            if (this.passableWhenSurfaceAttached)
            {
                Events["EnableSurfaceAttachable"].active = false;
                Events["DisableSurfaceAttachable"].active = true;
            }
            else
            {
                Events["EnableSurfaceAttachable"].active = true;
                Events["DisableSurfaceAttachable"].active = false;
            }
            if (this.surfaceAttachmentsPassable)
            {
                Events["EnableAttachableSurface"].active = false;
                Events["DisableAttachableSurface"].active = true;
            }
            else
            {
                Events["EnableAttachableSurface"].active = true;
                Events["DisableAttachableSurface"].active = false;
            }
        }

        // Allow a CLSPart to be cast into a ModuleConnectedLivingSpace. Note that this might fail, if the part in question does not have the CLS module configured.
        public static implicit operator ModuleConnectedLivingSpace(Part _p)
        {
            foreach(ModuleConnectedLivingSpace modcls in _p.Modules.OfType<ModuleConnectedLivingSpace>())
            {
                return (modcls);                
            }
            return null;
        }

        // Method to provide extra infomation about the part on response to the RMB
        public override string GetInfo()
        {
            String returnValue = String.Empty;
            returnValue = "Crewable:  " + (this.part.CrewCapacity > 0 ? "Yes" : "No");
            returnValue += "\r\nPassable:  " + (this.passable ? "Yes" : "No");
            returnValue += "\r\nImpassable Nodes:  " + (this.impassablenodes != "" ? this.impassablenodes : (this.passable ? "None" : "All"));
            returnValue += "\r\nPassable Nodes:  " + (this.passablenodes != "" ? this.passablenodes : (this.passable ? "All": "None"));
            returnValue += "\r\nPass when Surface Attached:  " + (this.passableWhenSurfaceAttached ? "Yes" : "No");
            returnValue += "\r\nSurface Attached Parts Pass:  " + (this.surfaceAttachmentsPassable ? "Yes" : "No");
            return returnValue;
        }


        [KSPEvent(guiActive = false, guiActiveEditor = true, name = "DisablePassable", guiName = "CLS Passable: Yes")]
        public void DisablePassable()
        {
            this.passable = false;
            Events["EnablePassable"].active = true;
            Events["DisablePassable"].active = false;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, name = "EnablePassable", guiName = "CLS Passable: No")]
        public void EnablePassable()
        {
            this.passable = true;
            Events["EnablePassable"].active = false;
            Events["DisablePassable"].active = true;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, name = "DisableSurfaceAttachable", guiName = "CLS Surface Attachable: Yes")]
        public void DisableSurfaceAttachable()
        {
            this.passableWhenSurfaceAttached = false;
            Events["EnableSurfaceAttachable"].active = true;
            Events["DisableSurfaceAttachable"].active = false;
        }
        [KSPEvent(guiActive = false, guiActiveEditor = true, name = "EnableSurfaceAttachable", guiName = "CLS Surface Attachable: No")]
        public void EnableSurfaceAttachable()
        {
            this.passableWhenSurfaceAttached = true;
            Events["EnableSurfaceAttachable"].active = false;
            Events["DisableSurfaceAttachable"].active = true;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, name = "DisableAttachableSurface", guiName = "CLS Attachable Surface: Yes")]
        public void DisableAttachableSurface()
        {
            this.surfaceAttachmentsPassable = false;
            Events["EnableAttachableSurface"].active = true;
            Events["DisableAttachableSurface"].active = false;
        }
        [KSPEvent(guiActive = false, guiActiveEditor = true, name = "EnableAttachableSurface", guiName = "CLS Attachable Surface: No")]
        public void EnableAttachableSurface()
        {
            this.surfaceAttachmentsPassable = true;
            Events["EnableAttachableSurface"].active = false;
            Events["DisableAttachableSurface"].active = true;
        }

    }
}
