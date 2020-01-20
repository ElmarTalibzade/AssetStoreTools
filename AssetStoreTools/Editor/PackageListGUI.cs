using System;
using UnityEditor;
using UnityEngine;

internal class PackageListGUI : DefaultListViewGUI<Package>
{
    private static PackageListGUI.GUIStyles s_Styles;

    private static PackageListGUI.GUIStyles Styles
    {
        get
        {
            if (PackageListGUI.s_Styles == null)
            {
                PackageListGUI.s_Styles = new PackageListGUI.GUIStyles();
            }
            return PackageListGUI.s_Styles;
        }
    }

    public PackageListGUI()
    {
    }

    protected override Texture GetDisplayIcon(Package node)
    {
        return node.Icon;
    }

    protected override string GetDisplayName(Package node)
    {
        return node.Name;
    }

    protected override GUIStyle GetLineStyle()
    {
        GUIStyle listNodeTextField = PackageListGUI.Styles.ListNodeTextField;
        listNodeTextField.padding.left = 5;
        return listNodeTextField;
    }

    public override Vector2 GetNodeArea(Package node)
    {
        return PackageListGUI.Styles.ListNodeTextField.CalcSize(GUIContent.none);
    }

    public override Rect OnRowGUI(Package node, Vector2 contentSize, bool selected, bool focus)
    {
        Vector2 nodeArea = this.GetNodeArea(node);
        Rect rect = new Rect(0f, (float)this.m_HeightOffset, contentSize.x, nodeArea.y);
        this.m_HeightOffset += (int)rect.height;
        if (Event.current.type == EventType.Repaint)
        {
            GUIStyle gUIStyle = EditorStyles.label;
            gUIStyle.padding.right = 5;
            GUIContent gUIContent = new GUIContent(node.Status.ToString());
            Vector2 vector2 = gUIStyle.CalcSize(gUIContent);
            Rect rect1 = new Rect(rect.x + rect.width - vector2.x, rect.y + (rect.height - vector2.y) / 2, vector2.x, vector2.y);

            GUIContent displayName = new GUIContent(string.Empty);
            GUIStyle lineStyle = this.GetLineStyle();
            displayName.text = this.GetDisplayName(node);
            displayName.image = this.GetDisplayIcon(node);
            lineStyle.Draw(rect, displayName, false, selected, selected, focus);
            gUIStyle.Draw(rect1, gUIContent, false, false, false, false);
        }
        return rect;
    }

    private class GUIStyles
    {
        internal GUIStyle MarginBox = new GUIStyle();

        internal GUIStyle AreaBox = new GUIStyle("GroupBox");

        internal GUIStyle ListNodeTextField = new GUIStyle("PR Label");

        public GUIStyles()
        {
            this.MarginBox.padding.top = 5;
            this.MarginBox.padding.right = 15;
            this.MarginBox.padding.bottom = 5;
            this.MarginBox.padding.left = 15;
            this.AreaBox.padding.top = 0;
            this.AreaBox.padding.right = 0;
            this.AreaBox.padding.bottom = 1;
            this.AreaBox.padding.left = 0;
            this.AreaBox.margin.top = 0;
            this.AreaBox.margin.right = 0;
            this.AreaBox.margin.bottom = 0;
            this.AreaBox.margin.left = 0;
            this.ListNodeTextField.margin.left = 1;
            this.ListNodeTextField.margin.right = 1;
            this.ListNodeTextField.fixedHeight = 50f;
            this.ListNodeTextField.alignment = TextAnchor.MiddleLeft;
            this.ListNodeTextField.padding.top = Mathf.FloorToInt((this.ListNodeTextField.fixedHeight - 32f) / 2f);
            this.ListNodeTextField.padding.bottom = this.ListNodeTextField.padding.top;
        }
    }
}