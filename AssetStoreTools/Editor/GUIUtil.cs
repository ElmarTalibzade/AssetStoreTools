using System;
using UnityEditor;
using UnityEngine;

internal static class GUIUtil
{
    private static GUIUtil.GUIStyles s_Styles;

    private static Texture2D sIconWarningSmall;

    private static GUIContent[] sStatusWheel;

    public static Color ErrorColor
    {
        get
        {
            return new Color(1f, 0.2f, 0.2f);
        }
    }

    public static GUIContent Logo
    {
        get
        {
            return EditorGUIUtility.IconContent("UnityLogo");
        }
    }

    public static GUIContent StatusWheel
    {
        get
        {
            if (GUIUtil.sStatusWheel == null)
            {
                GUIUtil.sStatusWheel = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                {
                    GUIContent gUIContent = new GUIContent()
                    {
                        image = GUIUtil.LoadRequiredIcon(string.Concat("WaitSpin", i.ToString("00"), ".png"))
                    };
                    GUIUtil.sStatusWheel[i] = gUIContent;
                }
            }
            int num = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10f, 11.99f);
            return GUIUtil.sStatusWheel[num];
        }
    }

    public static GUIUtil.GUIStyles Styles
    {
        get
        {
            if (GUIUtil.s_Styles == null)
            {
                GUIUtil.s_Styles = new GUIUtil.GUIStyles();
            }
            return GUIUtil.s_Styles;
        }
    }

    public static Texture2D WarningIcon
    {
        get
        {
            if (GUIUtil.sIconWarningSmall == null)
            {
                GUIUtil.sIconWarningSmall = GUIUtil.LoadRequiredIcon("console.warnicon.sml.png");
            }
            return GUIUtil.sIconWarningSmall;
        }
    }

    static GUIUtil()
    {
    }

    public static bool IsClickedOnLastRect()
    {
        Event @event = Event.current;
        if (@event.type != EventType.MouseDown || @event.button != 0 || !GUILayoutUtility.GetLastRect().Contains(@event.mousePosition))
        {
            return false;
        }
        @event.Use();
        return true;
    }

    private static Texture2D LoadRequiredIcon(string name)
    {
        Texture2D texture2D = EditorGUIUtility.Load(string.Concat("Icons/", name)) as Texture2D;
        return (texture2D == null ? EditorGUIUtility.LoadRequired(string.Concat("Builtin Skins/Icons/", name)) as Texture2D : texture2D);
    }

    public static Rect RectOnRect(float width, float height, Rect target)
    {
        float single = target.x;
        if (target.width >= width)
        {
            single = single + (target.width - width) * 0.5f;
        }
        float single1 = 100f;
        if (target.height >= height)
        {
            single1 = target.y;
        }
        return new Rect(single, single1, width, height);
    }

    public class GUIStyles
    {
        internal readonly GUIStyle delimiter = new GUIStyle("GroupBox");

        internal readonly GUIStyle verticalDelimiter = new GUIStyle("GroupBox");

        internal readonly GUIStyle dimmedTextArea;

        internal GUIStyles()
        {
            this.delimiter = new GUIStyle(this.delimiter)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 1, 0),
                fixedHeight = 1f
            };
            this.verticalDelimiter = new GUIStyle(this.verticalDelimiter)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(1, 0, 0, 0),
                fixedWidth = 1f
            };
            this.dimmedTextArea = new GUIStyle(GUI.skin.textArea);
            this.dimmedTextArea.normal.textColor = Color.gray;
        }
    }
}