using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConnectedLivingSpace
{
    public enum DockingPortHatchStatus
    {
        NOT_DOCKING_PORT = 0,
        DOCKING_PORT_HATCH_CLOSED = 1,
        DOCKING_PORT_HATCH_OPEN = 2
    }

    public class CLSPart
    {
        bool habitable = false;
        bool navigable = false;
        Part part;
        CLSSpace space;
        bool docked = false;
        List<CLSKerbal> crew;
        DockingPortHatchStatus hatchStatus = DockingPortHatchStatus.NOT_DOCKING_PORT; // For parts that are docking ports, indicates if the hatch is open or closed.   
        bool highlighted = false; // This allows us to remember if a part is SUPPOSED to be highlighted by CLS. We can then use appropriate moments to ensure that it either is or is not.

        public CLSPart(Part p)
        {
            this.part = p;

            habitable = IsHabitable(this.part);
            navigable = IsNavigable(this.part);
            space = null;

            this.crew = new List<CLSKerbal>();
            foreach (ProtoCrewMember crewMember in p.protoModuleCrew)
            {
                CLSKerbal kerbal = new CLSKerbal(crewMember, this);
                this.crew.Add(kerbal);
            }

            // Does the part have a CLSModule on it? If so then give the module a reference to ourselves to make its life a bit easier.
            {
                ModuleConnectedLivingSpace m = (ModuleConnectedLivingSpace)this;
                if (null != m)
                {
                    m.clsPart = this;
                    this.hatchStatus = m.hatchStatus;
                }
                else
                {
                    // This part does not have a CLSModule. If it is habitable or navigable then it will not be possible to persist the name of the space in the savefile. Log a warning.
                    if (this.habitable)
                    {
                        Debug.LogWarning("Part " + this.part.partInfo.title + " is habitable but does not have ModuleConnectedLivingSpace defined in the config. It would be better if it did as some infomation used by CLS will not be saved in the savefile.");
                    }
                    else if (this.navigable)
                    {
                        Debug.LogWarning("Part " + this.part.partInfo.title + " is passable but does not have ModuleConnectedLivingSpace defined in the config. It would be better if it did as some infomation used by CLS will not be saved in the savefile.");
                    }                
                }
            }
        }

        public CLSSpace Space
        {
            get
            {
                return this.space;
            }

            set
            {
                this.space = value;
            }
        }

        public DockingPortHatchStatus HatchStatus
        {
            get
            {
                return this.hatchStatus;
            }

            internal set
            {
                this.hatchStatus = value;
            }
        }

        public bool Docked
        {
            get
            {
                return this.docked;
            }
        }

        public List<CLSKerbal> Crew 
        {
            get
            {
                return this.crew;
            }
        }

        // Allow a CLSPart to be cast into a Part
        public static implicit operator Part(CLSPart _p)
        {
            return _p.part;
        }

        // Allow a CLSPart to be cast into a ModuleConnectedLivingSpace. Note that this might fail, if the part in question does not have the CLS module configured.
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
            // Set the variable to mark if this part is SUPPOSED to be hightlighted or not.
            this.highlighted = val;


            if (this.highlighted)
            {
                this.SetHighlighting(); 
  
                // Set up an event handler to handle the mouse being moved away from this part while it is supposed to be being highlighted.
                Part.OnActionDelegate OnMouseExit = MouseExit;
                part.AddOnMouseExit(OnMouseExit);
            }
            else
            {
                // Remove the event handler that picks up on the mouse being moved away from the part when it is highlighted.
                Part.OnActionDelegate OnMouseExit = MouseExit;
                part.RemoveOnMouseExit(OnMouseExit);

                part.SetHighlightDefault();
                this.part.SetHighlight(false);
            }
        }




        // Actually set this part to be highlighted
        private void SetHighlighting()
        {
            part.SetHighlightDefault();

            // Choose the colour based in the type of part!
            if (this.Habitable)
            {
                this.part.SetHighlightColor(Color.green);
            }
            else if (this.docked)
            {
                if (this.HatchStatus == DockingPortHatchStatus.DOCKING_PORT_HATCH_OPEN)
                {
                    this.part.SetHighlightColor(Color.cyan);
                }
                else
                {
                    this.part.SetHighlightColor(Color.magenta);
                }
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

            ModuleConnectedLivingSpace CLSMod = (ModuleConnectedLivingSpace)this;
            if(null != CLSMod)
            {
                return (CLSMod.passable);
            }

            return false;
        }

        internal void SetDocked(bool val)
        {
            this.docked = val;
        }

        // Throw away all potentially circular references in preparation this object to be thrown away
        internal void Clear()
        {
            this.space = null;
            foreach (CLSKerbal k in crew)
            {
                k.Clear();
            }
            this.crew.Clear();
        }

        #region Event handlers
        // this is the delagate needed to support the part event handlers
        // extern is needed, as the addon is considered external to KSP, and is expected by the part delagate call.
        extern Part.OnActionDelegate OnMouseExit(Part part);

        // this is the method used with the delagate
        void MouseExit(Part part)
        {
            Debug.Log("MouseExit from part: " + part.partInfo.title);
            // When the mouse moves away from a part, if it is supposed to be highlighted by us, then highlight it!
            if (this.highlighted)
            {
                this.SetHighlighting(); 
            }
        }
        #endregion
    }
}
