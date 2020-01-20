using System;
using System.Collections.Generic;
using UnityEngine;

internal interface IListViewGUI<T>
{
    void BeginRowsGUI();

    void EndRowsGUI();

    Vector2 GetNodeArea(T node);

    Vector2 GetTopLeftNodePixel(int index, IList<T> visibleRows);

    Vector2 GetTotalSize(IList<T> visibleRows, Vector2 displayArea);

    Rect OnRowGUI(T node, Vector2 contentSize, bool selected, bool focus);
}