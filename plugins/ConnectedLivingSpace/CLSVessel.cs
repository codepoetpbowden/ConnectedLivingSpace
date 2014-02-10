using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConnectedLivingSpace
{
    // A class that contains all the living space data for a particular vessel

    
    class CLSVessel
    {
        List<CLSPart> listParts;  // A list of parts in this vessel
        List<CLSSpace> listSpaces; // A list of seperate habitable spaces in this vessel.

        public CLSVessel()
        {
            listParts = new List<CLSPart>();
            listSpaces = new List<CLSSpace>();
        }

        public List<CLSSpace> Spaces
        {
            get
            {
                return listSpaces;
            }
        }

        public void Populate(Vessel vessel)
        {
            Debug.Log("Populate{");
            // Discard any currently held data.
            this.listParts.Clear();
            this.listSpaces.Clear();

            ProcessPart(vessel.rootPart, null);
            Debug.Log("Populate}");
        }

        // A method that is called recursively to walk the part tree, and allocate parts to habitable spaces
        private void ProcessPart(Part p, CLSSpace currentSpace)
        {
            CLSSpace thisSpace = null;

            CLSPart newPart = new CLSPart(p);

            Debug.Log("Processing part: " + p.name + " Navigable:"+newPart.Navigable + " Habitable:" + newPart.Habitable);

            // Is the part capable of containing kerbals? If not then just att the part to the null space, but if it is then add it to the current space, or a new space if there is no current space.
            if (newPart.Navigable)
            {
                thisSpace = currentSpace;

                if (null == thisSpace)
                {
                    thisSpace = AddPartToNewSpace(newPart);
                }
                else
                {
                    thisSpace = AddPartToSpace(newPart, thisSpace);
                }
            }
            else
            {
                thisSpace = AddPartToSpace(newPart, null);                
            }
            

            // Now loop through each of the part's children, consider if there is a navigable connection between them, and then make a recursive call.
            foreach (Part child in p.children)
            {
                // Get the attchment nodes
                AttachNode node = p.findAttachNodeByPart(child);
                AttachNode childNode = child.findAttachNodeByPart(p);

                if (null != node && null != childNode) // TODO in the case of a surface attached part I believe that we will not have two attachemnt nodes, only one (or is it even posible to have none?) Test for this and work out what to do.
                {
                    // So do these two nodes together create a navigable passage?
                    if (IsNodeNavigable(node, p) && IsNodeNavigable(childNode, child))
                    {
                        ProcessPart(child, thisSpace); // It looks like the connection is navigable. Process the child part and pass in the current space of this part.
                    }
                    else
                    {
                        ProcessPart(child, null); // There does not seem to be a way of Jeb accessing the child part. Sorry buddy - you will have to go EVA from here. Process the child part, bu pass in null. 
                    }
                }
                else
                {
                    ProcessPart(child, null); // There does not seem to be a way of Jeb accessing the child part. Sorry buddy - you will have to go EVA from here. Process the child part, bu pass in null. // TODO is this true?
                }
            }
        }

  

        // Decides is an attachment node on a part could allow a kerbal to pass through it.
        public bool IsNodeNavigable(AttachNode node, Part p)
        {
            String passablenodes ="";
            String impassablenodes="";

            // Get the config for this part
            foreach (PartModule pm in p.Modules)
            {
                Debug.Log("Part:" + p.name + " has module " + pm.moduleName + " " + pm.name);
                if (pm.moduleName == "ModuleConnectedLivingSpace")
                {
                    // This part does have a CLSmodule
                    ModuleConnectedLivingSpace CLSMod = (ModuleConnectedLivingSpace)pm;

                    Debug.Log("ModuleConnectedLivingSpace.navigable: " + CLSMod.passablenodes);

                    passablenodes = CLSMod.passablenodes;
                    impassablenodes = CLSMod.impassablenodes;

                    break;
                }
            }

            // TODO remove
            Debug.Log("passablenodes:" + passablenodes + " impassablenodes:"+impassablenodes +" node.id:"+ node.id);

            if (passablenodes.Contains(node.id))
            {
                return true;
            }

            if (impassablenodes.Contains(node.id))
            {
                return false;
            }

            // TODO Look up an answer in the set of config provided by this mod rather than the part.

            return true;
        }

        CLSSpace AddPartToNewSpace(CLSPart p)
        {
            Debug.Log("AddPartToNewSpace " + ((Part)p).name);
            Debug.Log("Number of spaces:" + this.listSpaces.Count);
            CLSSpace newSpace = new CLSSpace();

            this.listSpaces.Add(newSpace);

            newSpace.AddPart(p);

            Debug.Log("New Number of spaces:" + this.listSpaces.Count);
            

            // TODO we need to set the space for the part. NOTE this will create a circular reference that needs to be handled.

            return newSpace;

        }

        CLSSpace AddPartToSpace(CLSPart p, CLSSpace space)
        {
            Debug.Log("AddPartToSpace " + ((Part)p).name);

            if(null !=space)
            {
                space.AddPart(p);
            }
            // TODO we need to set the space for the part. NOTE this will create a circular reference that needs to be handled.

            return space;
        }


    }
}
