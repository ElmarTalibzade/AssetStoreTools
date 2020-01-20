using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class PackageSelector
{
    private static PackageSelector.GUIStyles s_Styles;

    private string m_SearchTerm = string.Empty;

    private ListView<Package> m_PackageList;

    private PackageDataSource m_PkgDataSource;

    public Package Selected
    {
        get { return this.m_PackageList.Selected; }
        set { this.m_PackageList.Selected = value; }
    }

    private static PackageSelector.GUIStyles Styles
    {
        get
        {
            if (PackageSelector.s_Styles == null)
            {
                PackageSelector.s_Styles = new PackageSelector.GUIStyles();
            }

            return PackageSelector.s_Styles;
        }
    }

    public PackageSelector(PackageDataSource pkgDataSource, ListView<Package>.SelectionCallback selectionCallback)
    {
        this.m_PkgDataSource = pkgDataSource;
        PackageListGUI packageListGUI = new PackageListGUI();
        this.m_PackageList = new ListView<Package>(this.m_PkgDataSource, packageListGUI, selectionCallback, selectionCallback);
    }

    public void Render(int height)
    {
        GUILayout.Label(new GUIContent("1. Select a package", "From the list below, select a package that you want to upload to the Asset Store. If you want to add a new package draft, please create it using the Publisher Portal."), new GUILayoutOption[0]);
        GUILayout.BeginVertical(PackageSelector.Styles.AreaBox, new GUILayoutOption[] {GUILayout.ExpandWidth(true)});
        GUILayout.Space(4f);
        this.RenderSearch();
        GUILayout.Space(3f);
        GUILayout.Box(GUIContent.none, GUIUtil.Styles.delimiter, new GUILayoutOption[] {GUILayout.MinHeight(1f), GUILayout.ExpandWidth(true)});
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, PackageSelector.Styles.AreaBox, new GUILayoutOption[] {GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight((float) height)});
        rect.x = rect.x + 1f;
        rect.width = rect.width - 2f;
        if (this.m_PkgDataSource.GetAllPackages().Count <= 0)
        {
            GUIStyle gUIStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                padding = new RectOffset(20, 20, 20, 20)
            };
            GUI.Label(rect, "There are no package drafts to be shown.\nTo add a new package draft, please use the Publisher Portal.", gUIStyle);
        }
        else if (Event.current.type != EventType.Layout)
        {
            this.m_PackageList.OnGUI(rect);
        }

        GUILayout.EndVertical();
    }

    private void RenderSearch()
    {
        bool flag = false;
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.Space(3f);
        string empty = GUILayout.TextField(this.m_SearchTerm, PackageSelector.Styles.ToolbarSearchTextField, new GUILayoutOption[0]);
        if (GUILayout.Button(GUIContent.none, (empty == string.Empty ? PackageSelector.Styles.ToolbarSearchFieldCancelButtonEmpty : PackageSelector.Styles.ToolbarSearchFieldCancelButton), new GUILayoutOption[0]) && this.m_SearchTerm != string.Empty)
        {
            empty = string.Empty;
            flag = true;
        }

        GUILayout.Space(2f);
        if (this.m_SearchTerm != empty)
        {
            this.m_SearchTerm = empty;
            this.m_PkgDataSource.SetFilter(this.m_SearchTerm);
        }

        if (flag)
        {
            this.m_PackageList.EnsureSelectionIsInView();
        }

        GUILayout.EndHorizontal();
    }

    private class GUIStyles
    {
        internal readonly GUIStyle MarginBox = new GUIStyle();

        internal GUIStyle AreaBox = new GUIStyle("GroupBox");

        internal GUIStyle ToolbarSearchTextField;

        internal GUIStyle ToolbarSearchFieldCancelButton;

        internal GUIStyle ToolbarSearchFieldCancelButtonEmpty;

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
            this.ToolbarSearchTextField = GUI.skin.FindStyle("SearchTextField");
            this.ToolbarSearchFieldCancelButton = GUI.skin.FindStyle("SearchCancelButton");
            this.ToolbarSearchFieldCancelButtonEmpty = GUI.skin.FindStyle("SearchCancelButtonEmpty");
            this.ListNodeTextField.margin.left = 1;
            this.ListNodeTextField.margin.right = 1;
            this.ListNodeTextField.fixedHeight = 50f;
            this.ListNodeTextField.alignment = TextAnchor.MiddleLeft;
        }
    }
}