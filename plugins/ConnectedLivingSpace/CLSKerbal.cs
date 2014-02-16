using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectedLivingSpace
{
    class CLSKerbal
    {
    	ProtoCrewMember kerbal;

    	public CLSKerbal(ProtoCrewMember kerbal) {
    		this.kerbal = kerbal;
    	}

        // Allow a CLSKerbal to be cast into a ProtoCrewMember
        public static implicit operator ProtoCrewMember(CLSKerbal _k)
        {
            return _k.kerbal;
        }

    }
}