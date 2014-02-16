using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectedLivingSpace
{
    class CLSSpace
    {
        List<CLSPart> parts;
        List<CLSKerbal> crew;
        String name;
        int maxCrew=0;
        CLSVessel vessel = null;

        public List<CLSPart> Parts
        {
            get
            {
                return parts;
            }
        }

        public int MaxCrew
        {
            get
            {
                return maxCrew;
            }
        }

        public String Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
                // If we change the name of the space, we need to also change the spaceName of all the parts that make it up.
                foreach (CLSPart p in this.parts)
                {
                    ModuleConnectedLivingSpace modCLS = (ModuleConnectedLivingSpace)p;
                    if (modCLS)
                    {
                        modCLS.spaceName = value;
                    }
                }
            }
        }

        public CLSVessel Vessel
        {
            get
            {
                return this.vessel;
            }
        }

        public List<CLSKerbal> Crew
        {
            get
            {
                return crew;
            }
        }

        public CLSSpace(CLSVessel v)
        {
            this.parts = new List<CLSPart>();
            this.crew = new List<CLSKerbal>();
            this.name = "";
            this.vessel = v;
        }

        internal void Highlight(bool val)
        {
            // Iterate through each CLSPart in this space and turn highlighting on or off.
            foreach (CLSPart p in this.parts)
            {
                p.Highlight(val);
            }
        }

        internal void AddPart(CLSPart p)
        {
            // Add the part to the space, and the space to the part.
            p.Space = this;
            
            // If this space does not have a name, take the name from the part we just added.
            if("" == this.name) 
            {
                ModuleConnectedLivingSpace modCLS = (ModuleConnectedLivingSpace)p;

                if(null != modCLS)
                {
                    this.name = modCLS.spaceName;
                }
            }
 
            this.parts.Add(p);

            this.maxCrew += ((Part)p).CrewCapacity;

            foreach(CLSKerbal crewMember in p.Crew) {
                this.crew.Add(crewMember);
            }
        }

        // A function to throw away all the parts references, and so break the circular reference. This should be called before throwing a CLSSpace away.
        internal void Clear()
        {
            foreach (CLSPart p in this.parts)
            {
                p.Clear();
            }
            this.parts.Clear();
            this.vessel = null;
        }

    }
}
