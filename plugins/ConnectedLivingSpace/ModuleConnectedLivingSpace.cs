using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConnectedLivingSpace
{
    // A module that can be added to a part in order to be able to set and read in part specific config that relates to ConnectedLivingSpace
    class ModuleConnectedLivingSpace : PartModule
    {
        [KSPField]
        public bool passable = false;
        [KSPField]
        public string passablenodes = "";
        [KSPField]
        public string impassablenodes = "";
        
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
            Debug.Log("CLS::OnStart state=" + state.ToString());

            try
            {

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

            // TODO read the config here, including the attachment nodes, som we have got all the infomation that is needed to decide on questions of navigability


        }
    }
}
