using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectedLivingSpace
{
  public class CLSKerbal : ICLSKerbal
  {
    ProtoCrewMember kerbal;
    CLSPart part;

    public CLSKerbal(ProtoCrewMember k, CLSPart p)
    {
      kerbal = k;
      part = p;
    }

    // Allow a CLSKerbal to be cast into a ProtoCrewMember
    public static implicit operator ProtoCrewMember(CLSKerbal _k)
    {
      return _k.kerbal;
    }

    public ICLSPart Part
    {
      get
      {
        return part;
      }
    }

    public ProtoCrewMember Kerbal
    {
      get
      {
        return kerbal;
      }
    }

    internal void Clear()
    {
      kerbal = null;
      part = null;
    }
  }
}