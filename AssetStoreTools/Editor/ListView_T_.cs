using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class ListView<T>
{
    protected IDataSource<T> m_DataSource;

    protected IListViewGUI<T> m_GUI;

    protected int m_KeyboardControlID;

    protected Vector2 m_ScrollPosition;

    protected Rect m_ScrollWindow;

    protected T m_Selected;

    protected ListView<T>.SelectionCallback m_SelectionConfirmCallback;

    protected ListView<T>.SelectionCallback m_SelectionChangeCallback;

    public T Selected
    {
        get { return this.m_Selected; }
        set
        {
            this.m_Selected = value;
            if (this.m_Selected != null)
            {
                this.EnsureSelectionIsInView();
            }
        }
    }

    public ListView(IDataSource<T> dataSource, IListViewGUI<T> gui)
    {
        this.m_DataSource = dataSource;
        this.m_GUI = gui;
    }

    public ListView(IDataSource<T> dataSource, IListViewGUI<T> gui, ListView<T>.SelectionCallback selectionChangeCallback, ListView<T>.SelectionCallback selectionConfirmCallback) : this(dataSource, gui)
    {
        this.m_SelectionConfirmCallback = selectionConfirmCallback;
        this.m_SelectionChangeCallback = selectionChangeCallback;
    }

    private void ChangeSelection(T node)
    {
        this.m_Selected = node;
        if (this.m_SelectionChangeCallback == null)
        {
            return;
        }

        this.m_SelectionChangeCallback(this.Selected);
    }

    protected void ConfirmSelection()
    {
        if (this.m_SelectionConfirmCallback == null)
        {
            return;
        }

        this.m_SelectionConfirmCallback(this.Selected);
    }

    public virtual void EnsureSelectionIsInView()
    {
        IList<T> visibleRows = this.m_DataSource.GetVisibleRows();
        if (visibleRows.Count == 0)
        {
            return;
        }

        int num = visibleRows.IndexOf(this.m_Selected);
        if (num < 0)
        {
            return;
        }

        Vector2 topLeftNodePixel = this.m_GUI.GetTopLeftNodePixel(num, visibleRows);
        int num1 = (int) topLeftNodePixel.y;
        int num2 = num1 - Mathf.FloorToInt(this.m_ScrollWindow.height);
        Vector2 nodeArea = this.m_GUI.GetNodeArea(this.m_Selected);
        int num3 = num2 + (int) nodeArea.y;
        this.m_ScrollPosition.y = Mathf.Clamp(this.m_ScrollPosition.y, (float) num3, (float) num1);
    }

    protected void GetKeyboardControl()
    {
        GUIUtility.keyboardControl = this.m_KeyboardControlID;
    }

    protected virtual void HandleNodeEvent(T node, Rect nodeArea)
    {
        Event @event = Event.current;
        EventType eventType = @event.type;
        if (eventType != EventType.MouseDown)
        {
            if (eventType == EventType.MouseUp)
            {
                if (GUIUtility.hotControl == this.m_KeyboardControlID)
                {
                    GUIUtility.hotControl = 0;
                    @event.Use();
                }
            }
        }
        else if (nodeArea.Contains(Event.current.mousePosition))
        {
            this.ChangeSelection(node);
            this.GetKeyboardControl();
            GUIUtility.hotControl = this.m_KeyboardControlID;
            @event.Use();
            if (Event.current.clickCount >= 2)
            {
                this.ConfirmSelection();
            }
        }
    }

    protected bool hasFocus()
    {
        if (GUIUtility.keyboardControl == this.m_KeyboardControlID)
        {
            return true;
        }

        return false;
    }

    protected virtual void KeyboardHandling()
    {
        if (Event.current.type == EventType.KeyDown && this.m_KeyboardControlID == GUIUtility.keyboardControl)
        {
            KeyCode keyCode = Event.current.keyCode;
            if (keyCode == KeyCode.UpArrow)
            {
                this.OffsetSelection(-1);
                this.EnsureSelectionIsInView();
                this.ConfirmSelection();
                Event.current.Use();
            }
            else if (keyCode == KeyCode.DownArrow)
            {
                this.OffsetSelection(1);
                this.EnsureSelectionIsInView();
                this.ConfirmSelection();
                Event.current.Use();
            }
        }
    }

    protected void OffsetSelection(int delta)
    {
        IList<T> visibleRows = this.m_DataSource.GetVisibleRows();
        if (visibleRows.Count == 0)
        {
            return;
        }

        int num = visibleRows.IndexOf(this.m_Selected) + delta;
        num = Mathf.Clamp(num, 0, visibleRows.Count - 1);
        this.m_Selected = visibleRows[num];
    }

    public virtual void OnGUI(Rect rect)
    {
        Vector2 vector2 = new Vector2(rect.width, rect.height);
        IList<T> visibleRows = this.m_DataSource.GetVisibleRows();
        Vector2 totalSize = this.m_GUI.GetTotalSize(visibleRows, vector2);
        Rect rect1 = new Rect(0f, 0f, totalSize.x, totalSize.y);
        this.m_KeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
        this.m_GUI.BeginRowsGUI();
        this.m_ScrollPosition = GUI.BeginScrollView(rect, this.m_ScrollPosition, rect1);
        IEnumerator<T> enumerator = visibleRows.GetEnumerator();
        try
        {
            while (enumerator.MoveNext())
            {
                T current = enumerator.Current;
                bool flag = current.Equals(this.m_Selected);
                Rect rect2 = this.m_GUI.OnRowGUI(current, totalSize, flag, this.hasFocus());
                this.HandleNodeEvent(current, rect2);
            }
        }
        finally
        {
            if (enumerator == null)
            {
            }

            enumerator.Dispose();
        }

        GUI.EndScrollView();
        this.m_GUI.EndRowsGUI();
        if (Event.current.type != EventType.Layout)
        {
            this.m_ScrollWindow = rect;
        }

        if (this.m_ScrollWindow.Contains(Event.current.mousePosition))
        {
            EventType eventType = Event.current.type;
            if (eventType == EventType.MouseDown)
            {
                this.GetKeyboardControl();
            }
            else if (eventType == EventType.ScrollWheel)
            {
                this.m_ScrollPosition += Event.current.delta;
                DebugUtils.LogWarning(Event.current.delta.ToString());
            }
        }

        this.KeyboardHandling();
    }

    public delegate void SelectionCallback(T selected);
}