using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectedLivingSpace
{
    class CLSSpace
    {
        List<CLSPart> parts;
        String name;
        int maxCrew=0;

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

        public CLSSpace()
        {
            parts = new List<CLSPart>();
            name = "";
        }

        public void AddPart(CLSPart p)
        {
            // Add the part to the space,a nd the space to the part.
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
        }

        // A function to throw aware all the parts references, and so break the circular reference. This should be called before throwing a CLSSpace away.
        public void Clear()
        {
            this.parts.Clear();
        }

    }
}
