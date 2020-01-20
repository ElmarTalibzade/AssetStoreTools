using System;
using System.Collections.Generic;
using UnityEngine;

internal class DefaultListViewGUI<T> : IListViewGUI<T>
{
    private static DefaultListViewGUI<T>.GUIStyles s_Styles;

    protected int m_HeightOffset;

    private static DefaultListViewGUI<T>.GUIStyles Styles
    {
        get
        {
            if (DefaultListViewGUI<T>.s_Styles == null)
            {
                DefaultListViewGUI<T>.s_Styles = new DefaultListViewGUI<T>.GUIStyles();
            }

            return DefaultListViewGUI<T>.s_Styles;
        }
    }

    public DefaultListViewGUI()
    {
    }

    public virtual void BeginRowsGUI()
    {
        this.m_HeightOffset = 0;
    }

    public virtual void EndRowsGUI()
    {
        this.m_HeightOffset = 0;
    }

    protected virtual Texture GetDisplayIcon(T node)
    {
        return null;
    }

    protected virtual string GetDisplayName(T node)
    {
        return string.Empty;
    }

    protected virtual GUIStyle GetLineStyle()
    {
        GUIStyle listNodeTextField = DefaultListViewGUI<T>.Styles.ListNodeTextField;
        listNodeTextField.padding.left = 5;
        return listNodeTextField;
    }

    public virtual Vector2 GetNodeArea(T node)
    {
        return DefaultListViewGUI<T>.Styles.ListNodeTextField.CalcSize(GUIContent.none);
    }

    public virtual Vector2 GetTopLeftNodePixel(int index, IList<T> visibleRows)
    {
        Vector2 vector2 = new Vector2(0f, 0f);
        float nodeArea = this.GetNodeArea(visibleRows[index]).y;
        vector2.y = (float)index * nodeArea;
        return vector2;
    }

    public virtual Vector2 GetTotalSize(IList<T> visibleRows, Vector2 displayArea)
    {
        Vector2 vector2 = new Vector2(0f, 0f);
        if (visibleRows == null || visibleRows.Count == 0)
        {
            return vector2;
        }

        float nodeArea = this.GetNodeArea(visibleRows[0]).y;
        vector2.y = (float)visibleRows.Count * nodeArea;
        if (vector2.x == 0f)
        {
            vector2.x = displayArea.x;
            if (vector2.y > displayArea.y)
            {
                float single = vector2.x;
                Vector2 vector21 = DefaultListViewGUI<T>.Styles.VerticalScrollBar.CalcSize(GUIContent.none);
                vector2.x = single - vector21.x;
            }
        }

        if (vector2.y == 0f)
        {
            vector2.y = displayArea.y;
            if (vector2.x > displayArea.x)
            {
                float single1 = vector2.y;
                Vector2 vector22 = DefaultListViewGUI<T>.Styles.HorizontalScrollbar.CalcSize(GUIContent.none);
                vector2.y = single1 - vector22.y;
            }
        }

        return vector2;
    }

    public virtual Rect OnRowGUI(T node, Vector2 contentSize, bool selected, bool focus)
    {
        Vector2 nodeArea = this.GetNodeArea(node);
        Rect rect = new Rect(0f, (float)this.m_HeightOffset, contentSize.x, nodeArea.y);
        this.m_HeightOffset += (int)rect.height;
        if (Event.current.type == EventType.Repaint)
        {
            GUIContent gUIContent = new GUIContent(string.Empty);
            GUIStyle lineStyle = this.GetLineStyle();
            gUIContent.text = this.GetDisplayName(node);
            gUIContent.image = this.GetDisplayIcon(node);
            lineStyle.Draw(rect, gUIContent, false, selected, selected, focus);
        }

        return rect;
    }

    private class GUIStyles
    {
        internal GUIStyle ListNodeTextField = new GUIStyle("PR Label");

        internal GUIStyle VerticalScrollBar = new GUIStyle("verticalScrollbar");

        internal GUIStyle HorizontalScrollbar = new GUIStyle("horizontalScrollbar");

        public GUIStyles()
        {
            this.ListNodeTextField.alignment = TextAnchor.MiddleLeft;
            this.ListNodeTextField.padding.top = 2;
            this.ListNodeTextField.padding.bottom = 2;
        }
    }
}