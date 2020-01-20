using System;
using UnityEditor;
using UnityEngine;

public class HelpWindow : EditorWindow
{
    internal readonly static Vector2 MIN_SIZE;

    private Vector2 scrollPos;

    private bool foldoutAbout = true;

    private bool foldoutSupport = true;

    static HelpWindow()
    {
        HelpWindow.MIN_SIZE = new Vector2(420f, 475f);
    }

    public HelpWindow()
    {
    }

    private void BeginRenderSection(string title, string content, ref bool shouldFoldout)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, new GUILayoutOption[0]);
        EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUIStyle gUIStyle = new GUIStyle(EditorStyles.foldout)
        {
            margin = new RectOffset(10, 0, 15, 0)
        };
        if (!shouldFoldout)
        {
            gUIStyle.normal.background = gUIStyle.active.background;
        }
        else
        {
            gUIStyle.normal.background = gUIStyle.onActive.background;
        }

        GUILayout.Label(string.Empty, gUIStyle, new GUILayoutOption[0]);
        GUIStyle color = new GUIStyle(EditorStyles.helpBox);
        color.onHover.background = Texture2D.blackTexture;
        color.normal.background = Texture2D.blackTexture;
        color.fontSize = 16;
        if (EditorGUIUtility.isProSkin)
        {
            color.normal.textColor = new Color(1f, 1f, 1f);
        }

        EditorGUILayout.LabelField(new GUIContent(title, EditorGUIUtility.IconContent("d_console.infoicon").image), color, new GUILayoutOption[0]);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
        if (GUIUtil.IsClickedOnLastRect())
        {
            shouldFoldout = !shouldFoldout;
        }

        if (shouldFoldout)
        {
            HelpWindow.RenderHorizontalLine(new Color32(37, 37, 37, 255));
            if (!string.IsNullOrEmpty(content))
            {
                GUIStyle gUIStyle1 = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    richText = true
                };
                EditorGUILayout.LabelField(content, gUIStyle1, new GUILayoutOption[0]);
            }
        }
    }

    private void EndRenderSection()
    {
        EditorGUILayout.EndVertical();
    }

    private void OnGUI()
    {
        this.scrollPos = EditorGUILayout.BeginScrollView(this.scrollPos, new GUILayoutOption[0]);
        GUILayout.Space(8f);
        this.BeginRenderSection("About Asset Store Tools", "Using the \"Asset Store Tools\" you can easily upload your content to the Unity Asset Store.\n\n  • To upload your content, click on the \"Package Upload\" tab in the Asset Store Tools menu, login to your Publisher Account, and follow the steps in the \"Package Upload\" window.\n  • To avoid the most common submission mistakes, you can scan the package that you want to upload using the  \"Package Validator\" window.\n\nIf you are facing any difficulties or have any further questions, please check the links in the \"Support\" section below.", ref this.foldoutAbout);
        this.EndRenderSection();
        GUILayout.Space(4f);
        this.BeginRenderSection("Support", string.Empty, ref this.foldoutSupport);
        if (this.foldoutSupport)
        {
            GUILayout.Space(8f);
            this.RenderHyperlink("https://unity3d.com/asset-store/sell-assets/submission-guidelines", "https://unity3d.com/asset-store/sell-assets/submission-guidelines", "Submission Guidelines:");
            GUILayout.Space(8f);
            this.RenderHyperlink("https://docs.unity3d.com/Manual/AssetStorePublishing.html", "https://docs.unity3d.com/Manual/AssetStorePublishing.html", "Asset Store Publishing:");
            GUILayout.Space(8f);
            this.RenderHyperlink("https://docs.unity3d.com/Manual/AssetStoreFAQ.html", "https://docs.unity3d.com/Manual/AssetStoreFAQ.html", "Asset Store FAQ:");
            GUILayout.Space(8f);
            this.RenderHyperlink("https://unity.com/support-services", "https://unity.com/support-services", "Support:");
            GUILayout.Space(4f);
        }

        this.EndRenderSection();
        EditorGUILayout.EndScrollView();
    }

    private static void RenderHorizontalLine(Color color)
    {
        GUIStyle gUIStyle = new GUIStyle();
        gUIStyle.normal.background = EditorGUIUtility.whiteTexture;
        gUIStyle.margin = new RectOffset(6, 6, 4, 8);
        gUIStyle.fixedHeight = 1f;
        Color color1 = GUI.color;
        GUI.color = color;
        GUILayout.Box(GUIContent.none, gUIStyle, new GUILayoutOption[0]);
        GUI.color = color1;
    }

    private void RenderHyperlink(string text, string hyperlink, string label = "")
    {
        EditorGUILayout.BeginVertical(new GUILayoutOption[0]);
        GUIStyle gUIStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            padding = new RectOffset(2, 0, 0, 0),
            margin = new RectOffset(4, 0, 0, 0)
        };
        GUILayout.Label(label, gUIStyle, new GUILayoutOption[0]);
        GUIStyle gUIStyle1 = new GUIStyle(EditorStyles.label)
        {
            richText = true
        };
        if (GUILayout.Button(new GUIContent(string.Format("<color=#409a9b>{0}</color>", text)), gUIStyle1, new GUILayoutOption[0]))
        {
            Application.OpenURL(hyperlink);
        }

        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
        EditorGUILayout.EndVertical();
    }

    [MenuItem("Asset Store Tools/Help", false, 50)]
    public static void ShowHelpWindow()
    {
        HelpWindow window = EditorWindow.GetWindow<HelpWindow>();
        GUIContent gUIContent = EditorGUIUtility.IconContent("_Help");
        gUIContent.text = "Help";
        window.titleContent = gUIContent;
        window.minSize = HelpWindow.MIN_SIZE;
        window.Show();
    }
}