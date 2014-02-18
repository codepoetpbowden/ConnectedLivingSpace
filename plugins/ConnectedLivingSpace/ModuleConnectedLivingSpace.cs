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
        [KSPField]
        public bool passable = false;
        [KSPField]
        public string passablenodes = "";
        [KSPField]
        public string impassablenodes = "";
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
    }
}
