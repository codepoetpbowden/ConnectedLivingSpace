using UnityEngine;

namespace ConnectedLivingSpace
{
  // ReSharper disable once InconsistentNaming
  internal static class CLSStyles
  {
    internal static GUIStyle WindowStyle;
    internal static GUIStyle IconStyle;
    internal static GUIStyle ButtonStyle;
    internal static GUIStyle ButtonToggledStyle;
    internal static GUIStyle ButtonStyleLeft;
    internal static GUIStyle ButtonToggledStyleLeft;
    internal static GUIStyle ToggleStyleHeader;
    internal static GUIStyle ErrorLabelRedStyle;
    internal static GUIStyle LabelStyle;
    internal static GUIStyle LabelTabHeader;
    internal static GUIStyle LabelStyleBold;
    internal static GUIStyle LabelStyleRed;
    internal static GUIStyle LabelStyleCyan;
    internal static GUIStyle LabelStyleYellow;
    internal static GUIStyle LabelStyleGreen;
    internal static GUIStyle LabelStyleNoWrap;
    internal static GUIStyle LabelStyleNoPad;
    internal static GUIStyle LabelStyleHardRule;
    internal static GUIStyle ToolTipStyle;
    internal static GUIStyle ScrollStyle;

    internal static void SetupGuiStyles()
    {
      if (CLSAddon.IsStyleSet) return;
      SetStyles();
      CLSAddon.IsStyleSet = true;
    }

    internal static void SetStyles()
    {
      WindowStyle = new GUIStyle(HighLogic.Skin.window);
      IconStyle = new GUIStyle();

      ButtonStyle = new GUIStyle(GUI.skin.button)
      {
        normal = { textColor = Color.white },
        hover = { textColor = Color.blue },
        fontSize = 12,
        padding =
        {
          top = 0,
          bottom = 0
        },
        fontStyle = FontStyle.Normal,
        alignment = TextAnchor.MiddleCenter,
        clipping = TextClipping.Clip
      };

      ButtonToggledStyle = new GUIStyle(GUI.skin.button)
      {
        normal = { textColor = Color.green },
        fontSize = 12,
        padding =
        {
          top = 0,
          bottom = 0
        },
        hover = { textColor = Color.blue }
      };

      ButtonToggledStyle.normal.background = ButtonToggledStyle.onActive.background;
      ButtonToggledStyle.fontStyle = FontStyle.Normal;
      ButtonToggledStyle.alignment = TextAnchor.MiddleCenter;
      ButtonToggledStyle.clipping = TextClipping.Clip;

      ButtonStyleLeft = new GUIStyle(GUI.skin.button)
      {
        normal = { textColor = Color.white },
        hover = { textColor = Color.green },
        fontSize = 12,
        padding =
        {
          top = 0,
          bottom = 0
        },
        fontStyle = FontStyle.Normal,
        alignment = TextAnchor.MiddleLeft,
        clipping = TextClipping.Clip
      };

      ButtonToggledStyleLeft = new GUIStyle(GUI.skin.button)
      {
        normal = { textColor = Color.green },
        fontSize = 12,
        padding =
        {
          top = 0,
          bottom = 0
        },
        hover = { textColor = Color.white }
      };
      ButtonToggledStyleLeft.normal.background = ButtonToggledStyleLeft.onActive.background;
      ButtonToggledStyleLeft.fontStyle = FontStyle.Normal;
      ButtonToggledStyleLeft.alignment = TextAnchor.MiddleLeft;
      ButtonToggledStyleLeft.clipping = TextClipping.Clip;

      ToggleStyleHeader = new GUIStyle(GUI.skin.toggle)
      {
        padding =
        {
          top = 10,
          bottom = 6
        },
        wordWrap = false,
        fontStyle = FontStyle.Bold,
        margin = new RectOffset(0, 0, 0, 0),
        alignment = TextAnchor.LowerLeft
      };

      ErrorLabelRedStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };

      LabelStyle = new GUIStyle(GUI.skin.label);

      LabelTabHeader = new GUIStyle(GUI.skin.label)
      {
        padding =
        {
          top = 10,
          bottom = 6
        },
        wordWrap = false,
        fontStyle = FontStyle.Bold,
        margin = new RectOffset(0, 0, 0, 0)
      };

      LabelStyleHardRule = new GUIStyle(GUI.skin.label)
      {
        padding =
        {
          top = 0,
          bottom = 6
        },
        wordWrap = false,
        alignment = TextAnchor.LowerLeft,
        fontStyle = FontStyle.Bold,
        margin = new RectOffset(0, 0, 0, 0)
      };

      LabelStyleNoWrap = new GUIStyle(GUI.skin.label)
      {
        wordWrap = false,
        clipping = TextClipping.Clip
      };

      LabelStyleNoPad = new GUIStyle(GUI.skin.label)
      {
        padding =
        {
          top = 0,
          bottom = 0
        },
        wordWrap = false
      };

      LabelStyleBold = new GUIStyle(GUI.skin.label)
      {
        fontSize = 18,
        fontStyle = FontStyle.Bold,
        alignment = TextAnchor.MiddleCenter
      };

      LabelStyleRed = new GUIStyle(LabelStyle) { normal = { textColor = Color.red } };

      LabelStyleCyan = new GUIStyle(LabelStyle) { normal = { textColor = Color.cyan } };

      LabelStyleYellow = new GUIStyle(LabelStyle) { normal = { textColor = Color.yellow } };

      LabelStyleGreen = new GUIStyle(LabelStyle) { normal = { textColor = Color.green } };
      ScrollStyle = new GUIStyle(GUI.skin.box);

      if (GUI.skin != null)
        GUI.skin = null;

      // ReSharper disable once PossibleNullReferenceException
      ToolTipStyle = new GUIStyle(GUI.skin.textArea)
      {
        border = new RectOffset(4, 4, 4, 4),
        padding = new RectOffset(5, 5, 5, 5),
        alignment = TextAnchor.MiddleLeft,
        fontStyle = FontStyle.Italic,
        wordWrap = false,
        normal = { textColor = Color.green },
        hover = { textColor = Color.green }
      };
      ToolTipStyle.hover.background = ToolTipStyle.normal.background;
    }
  }
}