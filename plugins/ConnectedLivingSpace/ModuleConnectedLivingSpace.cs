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
        public bool passableWhenSurfaceAttached = false;
        [KSPField]
        public bool surfaceAttachmentsPassable = false;
        [KSPField]
        public bool passable = false;
        [KSPField]
        public string passablenodes = "";
        [KSPField]
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
