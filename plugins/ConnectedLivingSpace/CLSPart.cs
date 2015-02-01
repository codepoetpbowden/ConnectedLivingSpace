using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConnectedLivingSpace
{
    public class CLSPart : ICLSPart
    {
        bool habitable = false;
        bool navigable = false;
        Part part;
        CLSSpace space;
        bool docked = false;
        List<ICLSKerbal> crew;
        public bool highlighted = false; // This allows us to remember if a part is SUPPOSED to be highlighted by CLS. We can then use appropriate moments to ensure that it either is or is not.

        public CLSPart(Part p)
        {
            this.part = p;

            habitable = IsHabitable(this.part);
            navigable = IsNavigable(this.part);
            space = null;

            this.crew = new List<ICLSKerbal>();
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
                }
                else
                {
                    // Do not bother logging warnings about EVAs not being configured for CLS!
                    if(this.part.Modules.OfType<KerbalEVA>().Count()  == 0)
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
        }

        public ICLSSpace Space
        {
            get
            {
                return this.space;
            }

            internal set
            {
                this.space = (CLSSpace) value;
            }
        }
        public bool Docked
        {
            get
            {
                return this.docked;
            }
        }

        public List<ICLSKerbal> Crew 
        {
            get
            {
                return this.crew;
            }
        }

        public ModuleConnectedLivingSpace modCLS
        {
            get
            {
                foreach (ModuleConnectedLivingSpace retVal in this.part.Modules.OfType<ModuleConnectedLivingSpace>())
                {
                    return retVal;
                }
                return null;
            }
        }

        public Part Part
        {
            get
            {
                return this.part;
            }
        }

        // Allow a CLSPart to be cast into a Part
        public static implicit operator Part(CLSPart _p)
        {
            return _p.Part;
        }

        // Allow a CLSPart to be cast into a ModuleConnectedLivingSpace. Note that this might fail, if the part in question does not have the CLS module configured.
        public static implicit operator ModuleConnectedLivingSpace(CLSPart _p)
        {
            return _p.modCLS;
        }
        
        public void Highlight(bool val, bool force = false)
        {
            // Set the variable to mark if this part is SUPPOSED to be hightlighted or not.
            if (val && (!this.highlighted || force))
            {
                this.SetHighlighting(force);
                this.highlighted = val;
                this.part.highlightType = Part.HighlightType.AlwaysOn;
            }
            else
            {
                if(!val && (this.highlighted || force))
                {
                    this.highlighted = val;
                    this.part.SetHighlight(false, false);
                    part.SetHighlightDefault();
                    this.part.highlightType = Part.HighlightType.OnMouseOver;
                }
            }
        }

        // Actually set this part to be highlighted
        private void SetHighlighting(bool force)
        {
            part.SetHighlightDefault();

            // Choose the colour based in the type of part!
            if (this.Habitable)
            {
                this.part.SetHighlightColor(Color.green);
            }
            else if (this.docked)
            {
                // The part has at least one docked dockingnode. If any of the docking nodes for this part support hatches, and any of the hatches are closed then we wil colour magenta rather than cyan.

                Color docNodeColor = Color.cyan;

                foreach (ModuleDockingHatch docNodeHatch in this.part.Modules.OfType<ModuleDockingHatch>())
                {
                    if (!docNodeHatch.HatchOpen)
                    {
                        docNodeColor = Color.magenta;
                        break;
                    }
                }
                this.part.SetHighlightColor(docNodeColor);
            }
            else if (this.Navigable)
            {
                // A navigable part might be an undocked docking port, in which case it still might have a closed/open hatch. check for this and colour apropriately.
                Color docNodeColor = Color.yellow;

                foreach (ModuleDockingHatch docNodeHatch in this.part.Modules.OfType<ModuleDockingHatch>())
                {
                    if (!docNodeHatch.HatchOpen)
                    {
                        docNodeColor.g = docNodeColor.g *0.66f; // This will turn my yellow into orange.
                        break;
                    }
                }
                this.part.SetHighlightColor(docNodeColor);
            }
            else
            {
                this.part.SetHighlightColor(Color.red);
            }
            this.part.SetHighlight(true, force);
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
