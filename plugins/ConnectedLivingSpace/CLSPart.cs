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
            part.SetHighlightDefault();
            
            if(val)
            {
                // Choose the colour based in the type of part!
                if(this.Habitable)
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
    }
}
