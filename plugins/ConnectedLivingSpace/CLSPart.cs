using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConnectedLivingSpace
{
    class CLSPart
    {
        bool habitable;
        bool navigable;
        Part part;
        CLSSpace space;

        public CLSPart(Part p)
        {
            this.part = p;

            habitable = IsHabitable(this.part);
            navigable = IsNavigable(this.part);
            space = null;
        }

        public CLSSpace Space
        {
            get
            {
                return this.space;
            }

            set
            {
                this.space = Space;
            }
        }

        // Allow a CLSPart to be cast into a Part
        public static implicit operator Part(CLSPart _p)
        {
            return _p.part;
        }

        // Allow a CLSPart to be cast into a ModueConnectedLivingSpace. Note that this might fail, if the part in question does not have the CLS module configured.
        public static implicit operator ModuleConnectedLivingSpace(CLSPart _p)
        {
            foreach (PartModule pm in _p.part.Modules)
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

        public void Highlight(bool val)
        {
            part.SetHighlightDefault();
            
            if(val)
            {
                // Choose the colour based in the type of part!
                if(this.Habitable)
                {
                    this.part.SetHighlightColor(Color.green);
                }
                else if (this.Navigable)
                {
                    this.part.SetHighlightColor(Color.yellow);
                }
                else
                {
                    this.part.SetHighlightColor(Color.red);
                }
                this.part.SetHighlight(true);
            }
            else
            {
                this.part.SetHighlight(false);
            }
        }

        public bool Habitable
        {
            get
            {
                return IsHabitable(this.part);
            }
        }

        public bool Navigable
        {
            get
            {
                return navigable;
            }
        }

        private bool IsHabitable(Part p)
        {
            return (p.CrewCapacity > 0);
        }

        private bool IsNavigable(Part p)
        {
            // first test - does it have a crew capacity?
            if (p.CrewCapacity > 0)
            {
                return true;
            }

            // Check to see if there is a CLSModule for this part. If there is then we can read the config for it.
            foreach(PartModule pm in this.part.Modules)
            {
                //Debug.Log("Part:" + this.part.name + " has module " + pm.moduleName + " " + pm.name);
                if(pm.moduleName =="ModuleConnectedLivingSpace")
                {
                    // This part does have a CLSmodule
                    ModuleConnectedLivingSpace CLSMod = (ModuleConnectedLivingSpace)pm;

                    //Debug.Log("ModuleConnectedLivingSpace.navigable: " + CLSMod.passable);

                    return(CLSMod.passable);
                }
            }

            return false;
        }


    }
}
