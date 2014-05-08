using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ConnectedLivingSpace
{
    // A class that contains all the living space data for a particular vessel
       
    public class CLSVessel
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

        public List<CLSPart> Parts
        {
            get
            {
                return listParts;
            }
        }

        internal void Populate(Vessel vessel)
        {
            Populate(vessel.rootPart);
        }

        internal void Populate(Part rootPart)
        {
            // Discard any currently held data.
            this.listParts.Clear();
            this.listSpaces.Clear();

            // Check that there is a root part, as if this was called in the EditorContext, the Editor.startPod will have been passed in, and that can be null.
            if (null != rootPart)
            {
                ProcessPart(rootPart, null); // This is called recursively going through the entire part tree.
                TidySpaces(); // This method might remove some of the spaces that have just been created if there are no habitable parts in them.
            }
        }

        // Method to go through each space once all the sapces are complete, and remove any that do not make sense, such as not having capacity for any crew.
        private void TidySpaces()
        {
            List<CLSSpace> listSpacesToRemove = new List<CLSSpace>();

            foreach(CLSSpace space in this.listSpaces)
            {
                if(space.MaxCrew == 0)
                {
                    listSpacesToRemove.Add(space);
                }
            }

            foreach(CLSSpace spaceToRemove in listSpacesToRemove)
            {
                spaceToRemove.Clear();
                listSpaces.Remove(spaceToRemove);
            }

        }

        // A method that is called recursively to walk the part tree, and allocate parts to habitable spaces
        private void ProcessPart(Part p, CLSSpace currentSpace , bool dockedToParent=false)
        {
            CLSSpace thisSpace = null;
            CLSPart newPart = new CLSPart(p);

            //Debug.Log("Processing part: " + p.name + " Navigable:"+newPart.Navigable + " Habitable:" + newPart.Habitable);

            // First add this part to the list of all parts for the vessel.
            this.listParts.Add(newPart);

            // Is the part capable of allowing kerbals to pass? If it is add it to the current space, or if there is no current space, to a new space.
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

            // Now loop through each of the part's children, consider if there is a navigable connection between them, and then make a recursive call.
            foreach (Part child in p.children)
            {
                // Get the attachment nodes
                AttachNode node = p.findAttachNodeByPart(child);
                AttachNode childNode = child.findAttachNodeByPart(p);
                bool attachmentIsPassable = false;
                bool childAttachmentIsPassable = false;
                bool dockingConnection = false;
                CLSSpace spaceForChild = thisSpace;

                // TODO removed debugging
                //Debug.Log("Considering the connection between " + p.partInfo.title + "(" + p.uid + ") and " + child.partInfo.title + "(" + child.uid + ")");
                {
                    // Is the attachment on "this" part passable?
                    if (null != node)
                    {
                        // The attachment is in the form of an AttachNode - use it to work out if the attachment is passable.
                        attachmentIsPassable = IsNodeNavigable(node, p);
                        //Debug.Log("the attachment on 'this' part is defined by attachment node " + node.id + " and had been given passable=" + attachmentIsPassable);
                    }
                    else
                    {
                        // Could it be that we are dealling with a docked connection?
                        dockingConnection = CheckForDockedPair(p, child);

                        if (true == dockingConnection)
                        {
                            //Debug.Log("The two parts are considered to be docked together.");
                            // The parts are docked, but we still need to have a think about if the docking port is passable.
                            attachmentIsPassable = IsDockedDockingPortPassable(p, child);
                            //Debug.Log("the docked attachment on 'this' part has been given passable=" + attachmentIsPassable);
                        }
                        else
                        {
                            //Debug.Log("The two parts are NOT considered to be docked together - concluding that this part is suface attached");
                            // It is not a AttachNode attachment, and it is not a docked connection either. The only other option is that we are dealing with a surface attachment. Does this part allow surfact attachments to be passable?
                            if (PartHasPassableSurfaceAttachments(p))
                            {
                                attachmentIsPassable = true;
                                //Debug.Log("This part is surface attached and is considered to be passable");
                            }
                        }
                    }
                }

                // Repeat the above block for the child part.
                {
                    // Is the attachment on "this" part passable?
                    if (null != childNode)
                    {
                        // The attachment is in the form of an AttachNode - use it to work out if the attachment is passable.
                        childAttachmentIsPassable = IsNodeNavigable(childNode, child);
                        //Debug.Log("the attachment on the child part is defined by attachment node " + childNode.id + " and had been given passable=" + attachmentIsPassable);
                    }
                    else
                    {
                        if (true == dockingConnection)
                        {
                            //Debug.Log("The two parts are considered to be docked together.");
                            // The parts are docked, but we still need to have a think about if the docking port is passable.
                            childAttachmentIsPassable = IsDockedDockingPortPassable(child, p);
                            //Debug.Log("the docked attachment on the child part has been given passable=" + attachmentIsPassable);
                        }
                        else
                        {
                            //Debug.Log("The two parts are NOT considered to be docked together - concluding that the child part is suface attached");
                            // It is not a AttachNode attachment, and it is not a docked connection either. The only other option is that we are dealing with a surface attachment. Does this part allow surfact attachments to be passable?
                            if (PartHasPassableSurfaceAttachments(child))
                            {
                                childAttachmentIsPassable = true;
                                //Debug.Log("The child part is surface attached and is considered to be passable");
                            }
                        }
                    }
                }

                // So, is it possible to get from this part to the child part?
                if (attachmentIsPassable && childAttachmentIsPassable)
                {
                    // It is possible to pass between this part and the child part - so the child needs to be in the same space as this part.
                    //Debug.Log("The connection between 'this' part and the child part s passable in both directions, so the child part will be added to the same space as this part.");
                    spaceForChild = thisSpace;
                }
                else
                {
                    // it is not possible to get into the child part from this part - it will need to be in a new space.
                    //Debug.Log("The connection between 'this' part and the child part is NOT passable in both directions, so the child part will be added to a new space.");
                    spaceForChild = null;
                }

                // Having work out all the variables, make the recursive call
                ProcessPart(child, spaceForChild, dockingConnection);

                // Was the connection a docking connection - if so we ought to mark the relevant CLSParts
                if (dockingConnection || dockedToParent)
                {
                    newPart.SetDocked(true);
                }
            }
        }

        // Helper method that figures out if surfaceAttachmentsPassable is set for a CLSModule on the specified part.
        private bool PartHasPassableSurfaceAttachments(Part p)
        {
            ModuleConnectedLivingSpace clsMod = (ModuleConnectedLivingSpace)p;
            if (null == clsMod)
            {
                // No CLS mod. Therefore surface attachments are definately not passable
                return false;
            }
            else
            {
                return clsMod.surfaceAttachmentsPassable;
            }
        }

        private bool CheckForDockedPair(Part thisPart, Part otherPart)
        {
            bool thisDockedToOther = false;
            bool otherDockedToThis = false;

            // Loop through all the ModuleDockingNodes for this part and check if any are docked to the other part.
            foreach (ModuleDockingNode docNode in thisPart.Modules.OfType<ModuleDockingNode>())
            {
                if (CheckForNodeDockedToPart(docNode, otherPart))
                {
                    thisDockedToOther = true;
                    break;
                }
            }

            // Loop through all the ModuleDockingNodes for the other part and check if any are docked to this part.
            foreach (ModuleDockingNode docNode in otherPart.Modules.OfType<ModuleDockingNode>())
            {
                if (CheckForNodeDockedToPart(docNode, thisPart))
                {
                    otherDockedToThis = true;
                    break;
                }
            }

            // Return that this part and the other part are docked together if they are both considered docked to each other.
            return (thisDockedToOther && otherDockedToThis);
        }

        private bool IsDockedDockingPortPassable(Part thisPart, Part otherPart)
        {
            bool retVal = false;

            // First things first - does this part even support CLS? If it does not then the dockingPort is certain to be impassable.
            ModuleConnectedLivingSpace clsModThis = (ModuleConnectedLivingSpace)thisPart;
            if (null == clsModThis)
            {
                //Debug.Log("Part " + thisPart.partInfo.title + "(" + thisPart.uid + ") does not seem to support CLS. Setting it as impassable.");
                return false;
            }
            else
            {
                // As it does support CLS, first set the passable value to the the "passable" field for this part
                retVal = clsModThis.passable;
            }

            // Loop through all the ModuleDockingNodes for this part and check if any are docked to the other part.
            foreach (ModuleDockingNode docNode in thisPart.Modules.OfType<ModuleDockingNode>())
            {
                if (CheckForNodeDockedToPart(docNode, otherPart))
                {
                    // We have found the ModuleDockingNode that represents the docking connection on this part.
                    //Debug.Log("Found docking node that represents the docking connection to the 'other' part");

                    // First consider if this docked connection has an accompanying AttachNode may be defined as (im)passable by CLS. 
                    if (docNode.referenceAttachNode != string.Empty)
                    {
                        //Debug.Log("docking node uses a referenceAttachNode called: " + docNode.referenceAttachNode + " In the meantime, passablenodes: " + clsModThis.passablenodes + " impassablenodes: " + clsModThis.impassablenodes);
                        if (clsModThis.passablenodes.Contains(docNode.referenceAttachNode))
                        {
                            retVal = true;
                        }

                        if (clsModThis.impassablenodes.Contains(docNode.referenceAttachNode))
                        {
                            retVal = false;
                        }
                    }
                    // Second, if there is no AttachNode, what about the type / size of the docking port
                    else
                    {
                        //Debug.Log("docking node does not use referenceAttachNode, instead considering the nodeType: " + docNode.nodeType + " In the meantime, impassableDockingNodeTypes:" + clsModThis.impassableDockingNodeTypes + " passableDockingNodeTypes:" + clsModThis.passableDockingNodeTypes);
                        if (clsModThis.impassableDockingNodeTypes.Contains(docNode.nodeType))
                        {
                            retVal = false; // Docking node is of an impassable type.
                        }
                        if (clsModThis.passableDockingNodeTypes.Contains(docNode.nodeType))
                        {
                            retVal = true; // Docking node is of a passable type.
                        }
                    }

                    // third, consider if there is an open / closed hatch
                    {
                        ModuleDockingHatch docHatch = GetHatchForDockingNode(docNode);
                        if (docHatch != null)
                        {
                            // The dockingNode is actually a DockingNodeHatch :)
                            if (!docHatch.HatchOpen)
                            {
                                //Debug.Log("DockingNodeHatch is closed and so can not be passed through");
                                retVal = false; // Hatch in the docking node is closed, so it is impassable
                            }
                        }
                    }
                    break;
                }
            }
            //Debug.Log("returning " + retVal);
            return retVal;
        }

        private ModuleDockingHatch GetHatchForDockingNode(ModuleDockingNode dockNode)
        {
            foreach (ModuleDockingHatch dockHatch in dockNode.part.Modules.OfType<ModuleDockingHatch>())
            {
                if (dockHatch.modDockNode == dockNode)
                {
                    return dockHatch;
                }
            }
            return null;
        }

        private bool CheckForNodeDockedToPart(ModuleDockingNode thisNode, Part otherPart)
        {
            bool retVal = false;

            // TODO remove debugging
            //Debug.Log("thisNode.dockedPartUId=" + thisNode.dockedPartUId + " otherPart.flightID=" + otherPart.flightID + " thisNode.state:" + thisNode.state);

            // if (otherPart == thisNode.part.vessel[thisNode.dockedPartUId])
            if (thisNode.dockedPartUId == otherPart.flightID)
            {
                //Debug.Log("IDs match");
                if(thisNode.state == "Docked (dockee)")
                {
                    //Debug.Log("this module is docked (dockee) to the other part");
                    retVal = true;
                }
                else if (thisNode.state == "Docked (docker)")
                {
                    //Debug.Log("this module is docked (docker) to the other part");
                    retVal = true;
                }
                else if (thisNode.state == "Acquire")
                {
                    Debug.LogWarning("this module is in the Acquire state, which might mean it is in the process of docking.");
                    retVal = true;
                }
            }
            return retVal;
        }

        // Decides is an attachment node on a part could allow a kerbal to pass through it.
        private bool IsNodeNavigable(AttachNode node, Part p)
        {
            String passablenodes ="";
            String impassablenodes="";
            bool passableWhenSurfaceAttached = false;
            bool closedHatch = false;
            bool retVal = false;

            // Get the config for this part
            foreach (ModuleConnectedLivingSpace CLSMod in p.Modules.OfType<ModuleConnectedLivingSpace>())
            {
                // This part does have a CLSmodule
                passablenodes = CLSMod.passablenodes;
                impassablenodes = CLSMod.impassablenodes;
                passableWhenSurfaceAttached = CLSMod.passableWhenSurfaceAttached;
                retVal = CLSMod.passable;
                break;
            }

            // Is there a DockingHatch that relates to this node? This would occur in a situation where a docking node was assembled onto another part in the VAB.
            foreach (ModuleDockingHatch hatchMod in p.Modules.OfType<ModuleDockingHatch>())
            {
                // This part does have a Docking Hatch - (it might have several) Consider if this hatch relates to the attachment node in question
                if (hatchMod.docNodeAttachmentNodeName == node.id)
                {
                    // This hatch relates to this attachment node
                    if (!hatchMod.HatchOpen)
                    {
                        closedHatch = true;
                    }

                    break;
                }
            }

            if (node.nodeType == AttachNode.NodeType.Surface)
            {
                //Debug.Log("node is a surface attachment node. Considering if the part is configured to allow passing when it is surface attached. - " + passableWhenSurfaceAttached);
                retVal = passableWhenSurfaceAttached;
            }
            else
            {
                if (passablenodes.Contains(node.id))
                {
                    retVal = true;
                }

                if (impassablenodes.Contains(node.id))
                {
                    retVal = false;
                }
            }

            // Finally  - have we concluded that passage is blocked by a closed hatch?
            if (closedHatch)
            {
                retVal = false;
            }

            return retVal;
        }

        CLSSpace AddPartToNewSpace(CLSPart p)
        {
            CLSSpace newSpace = new CLSSpace(this);

            this.listSpaces.Add(newSpace);

            newSpace.AddPart(p);

            return newSpace;
        }

        CLSSpace AddPartToSpace(CLSPart p, CLSSpace space)
        {
            //Debug.Log("AddPartToSpace " + ((Part)p).name);

            if (null != space)
            {
                space.AddPart(p);
            }
            else
            {
                Debug.LogError("Can't add part " + ((Part)p).partInfo.title +" to null space");
            }

            return space;
        }

        // Method to throw away potential circular references before the object is disposed of
        public void Clear()
        {
            foreach (CLSSpace s in this.listSpaces)
            {
                s.Clear();
            }
            listSpaces.Clear();
        }

        // Method to highlight on unhighlight all the habitable spaces in this vessel
        public void Highlight(bool arg)
        {
            foreach (CLSSpace space in this.listSpaces)
            {
                space.Highlight(arg);
            }
        }
    }
}
