using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectedLivingSpace
{
    public interface ICLSVessel
    {
        List<ICLSSpace> Spaces { get; }
        List<ICLSPart> Parts { get; }
        void Highlight(bool arg);
    }

    public interface ICLSSpace
    {
        List<ICLSPart> Parts {get;}
        int MaxCrew {get;}
        String Name {get;set;}
        ICLSVessel Vessel {get;}
        List<ICLSKerbal> Crew {get;}
        void Highlight(bool val);
    }

    public interface ICLSPart
    {
        ICLSSpace Space { get; }
        bool Docked { get; }
        List<ICLSKerbal> Crew { get; }
        Part Part { get; }
        void Highlight(bool val, bool force);
        bool Habitable { get; }
        bool Navigable { get; }
    }

    public interface IModuleDockingHatch
    {
        bool HatchOpen { get; set; }
        string HatchStatus { get; }
        bool IsDocked { get; }
        ModuleDockingNode ModDockNode { get; }
        BaseEventList HatchEvents { get; }
    }

    public interface ICLSKerbal
    {
        ICLSPart Part { get;}
        ProtoCrewMember Kerbal { get; }
    }

    public interface ICLSAddon
    {
        ICLSVessel Vessel { get; }
    }


}
