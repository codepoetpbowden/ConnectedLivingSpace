using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectedLivingSpace
{
    class CLSSpace
    {
        List<CLSPart> parts;

        public List<CLSPart> Parts
        {
            get
            {
                return parts;
            }
        }

        public CLSSpace()
        {
            parts = new List<CLSPart>();
        }

        public void AddPart(CLSPart p)
        {
            // Add the part to the space,a nd the space to the part.
            p.Space = this;
            this.parts.Add(p);
        }

        // A function to throw aware all the parts references, and so break the circular reference. This should be called before throwing a CLSSpace away.
        public void Clear()
        {
            this.parts.Clear();
        }

    }
}
