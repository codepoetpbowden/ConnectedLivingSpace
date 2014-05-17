using UnityEngine;

/*
 * Create a dropdown list
 * based on http://wiki.unity3d.com/index.php?title=PopupList
 */

namespace ConnectedLivingSpace
{
    class GUIDropdown
    {
        private static bool forceToUnShow = false; 
        private static int useControlID = -1;
        private bool isClickedComboButton = false;
        private int selectedItemIndex = 0;
 
	    private GUIContent buttonContent;
	    private GUIContent[] listContent;
	    private string buttonStyle;
	    private string boxStyle;
	    private GUIStyle listStyle;
 
        public GUIDropdown( GUIContent buttonContent, GUIContent[] listContent, GUIStyle listStyle )
        {
		    this.buttonContent = buttonContent;
		    this.listContent = listContent;
		    this.buttonStyle = "button";
		    this.boxStyle = "box";
		    this.listStyle = listStyle;
        }

        public GUIDropdown(GUIContent buttonContent, GUIContent[] listContent, string buttonStyle, string boxStyle, GUIStyle listStyle)
        {
		    this.buttonContent = buttonContent;
		    this.listContent = listContent;
		    this.buttonStyle = buttonStyle;
		    this.boxStyle = boxStyle;
		    this.listStyle = listStyle;
	    }
 
        public int Show(Rect rect)
        {
            if( forceToUnShow )
            {
                forceToUnShow = false;
                isClickedComboButton = false;
            }
 
            bool done = false;
            int controlID = GUIUtility.GetControlID( FocusType.Passive );       
 
            switch( Event.current.GetTypeForControl(controlID) )
            {
                case EventType.mouseUp:
                {
                    if( isClickedComboButton )
                    {
                        done = true;
                    }
                }
                break;
            }       
 
            if( GUI.Button( rect, buttonContent, buttonStyle ) )
            {
                if( useControlID == -1 )
                {
                    useControlID = controlID;
                    isClickedComboButton = false;
                }
 
                if( useControlID != controlID )
                {
                    forceToUnShow = true;
                    useControlID = controlID;
                }
                isClickedComboButton = true;
            }
 
            if( isClickedComboButton )
            {
/*                Debug.Log("GUI Depth before set: " + GUI.depth);
                GUI.depth = GUI.depth - 1;
                Debug.Log("GUI Depth after set: " + GUI.depth);
*/
                Rect listRect = new Rect( rect.x, rect.y + listStyle.CalcHeight(listContent[0], 1.0f),
                          rect.width, listStyle.CalcHeight(listContent[0], 1.0f) * listContent.Length );
 
                GUI.Box( listRect, "", boxStyle );
                int newSelectedItemIndex = GUI.SelectionGrid( listRect, selectedItemIndex, listContent, 1, listStyle );
                if( newSelectedItemIndex != selectedItemIndex )
                {
                    selectedItemIndex = newSelectedItemIndex;
                    buttonContent = listContent[selectedItemIndex];
                }
/*
                Debug.Log("GUI Depth before reset: " + GUI.depth);
                GUI.depth = GUI.depth + 1;
                Debug.Log("GUI Depth after reset: " + GUI.depth);
*/
            }
 
            if( done )
                isClickedComboButton = false;
 
            return selectedItemIndex;
        }
 
        public int SelectedItemIndex
        {
		    get{
        	    return selectedItemIndex;
		    }
		    set{
			    selectedItemIndex = value;
                buttonContent = listContent[selectedItemIndex];
		    }
        }

        public bool IsOpen { get { return isClickedComboButton; } }
    } //class GUIDropdown
}
