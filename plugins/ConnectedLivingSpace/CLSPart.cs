using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectedLivingSpace
{
    class CLSPart
    {
        public CLSPart(Part p)
        {
            this.part = p;

            habitable = IsHabitable(this.part);
            navigable = IsNavigable(this.part);
            space = null;
        }

        bool habitable;
        bool navigable;
        Part part;
        CLSSpace space;

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
                return IsNavigable(this.part);
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

            // Next test - read the parts own config and look for a field defined by this mod that means to us that it is navigable
            {
                Object CLSPassable = p.Fields.GetValue("CLSPassable");
                if (null != CLSPassable)
                {
                    if (1 == (int)CLSPassable) { return true; }
                    else if ("1" == (string)CLSPassable) { return true; }
                    else if ("yes" == (string)CLSPassable) { return true; }
                    else if ("true" == (string)CLSPassable) { return true; }
                    else if ("passable" == (string)CLSPassable) { return true; }
                    else if ("navigable" == (string)CLSPassable) { return true; }
                }
            }

            // TODO
            // Next try looking up the part in a list of predefines that ship with this mod.
            {

            }

            return false;
        }


    }
}
