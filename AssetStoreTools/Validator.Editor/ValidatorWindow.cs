using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    [InitializeOnLoad]
    public class ValidatorWindow : EditorWindow
    {
        private const float ChecklistSpacing = 6f;

        private static ValidatorWindow window;

        private Vector2 scrollPos;

        private Texture2D errorIcon;

        private Texture2D warningIcon;

        private Texture2D checkIcon;

        private bool showErrorItems = true;

        private bool showWarningItems = true;

        private bool showPassItems = true;

        private bool showChecklist;

        public string PackagePath
        {
            get;
            set;
        }

        public ValidatorWindow()
        {
        }

        private void ChecklistAssetsGUI(ChecklistItem check, List<string> paths)
        {
            if (!paths.Any<string>())
            {
                return;
            }
            this.Indent(2);
            check.FoldoutPaths = EditorGUILayout.Foldout(check.FoldoutPaths, " Related Assets:");
            if (GUILayout.Button("Select All", new GUILayoutOption[] { GUILayout.MaxWidth(80f), GUILayout.MinWidth(80f), GUILayout.MaxHeight(20f), GUILayout.MinHeight(20f) }))
            {
                IEnumerable<UnityEngine.Object> objs =
                    from f in paths
                    select AssetDatabase.LoadMainAssetAtPath(f);
                Selection.objects = objs.ToArray<UnityEngine.Object>();
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            if (check.FoldoutPaths)
            {
                foreach (string path in paths)
                {
                    GUILayout.EndHorizontal();
                    this.Indent(3);
                    UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                    EditorGUILayout.ObjectField(obj, typeof(UnityEngine.Object), false, new GUILayoutOption[0]);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ChecklistItemGUI(ChecklistItem check)
        {
            if (!check.Active)
            {
                return;
            }
            this.Indent(1);
            GUIStyle gUIStyle = new GUIStyle(EditorStyles.foldout);
            string title = ValidatorData.ItemData[(int)check.Type].Title;
            check.Foldout = EditorGUILayout.Foldout(check.Foldout, string.Concat(" ", title), gUIStyle);
            GUILayout.EndHorizontal();
            if (check.Foldout)
            {
                string message = ValidatorData.ItemData[(int)check.Type].Message;
                if (check.Failed)
                {
                    message = string.Concat("<color=#", ColorUtility.ToHtmlStringRGB(GUIUtil.ErrorColor), ">An exception occurred when performing this check! Please view the Console for more information.</color>\n\n", message);
                }
                this.ChecklistMessageGUI(check, message);
                this.ChecklistAssetsGUI(check, check.AssetPaths);
            }
        }

        private void ChecklistMessageGUI(ChecklistItem check, string message)
        {
            this.Indent(2);
            GUILayout.EndHorizontal();
            this.Indent(3);
            GUIStyle gUIStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = true
            };
            EditorGUILayout.LabelField(message, gUIStyle, new GUILayoutOption[0]);
            GUILayout.EndHorizontal();
        }

        private void DisplayIncorrectPathPopup(string message)
        {
            EditorUtility.DisplayDialog("Incorrect Path", message, "Close");
        }

        private void DrawEntryIcon(Texture2D icon, int iconSize)
        {
            Rect rect = GUILayoutUtility.GetRect((float)iconSize, (float)iconSize, new GUILayoutOption[] { GUILayout.MaxWidth((float)iconSize) });
            rect.x = 18.5f;
            rect.y = rect.y + 1.5f;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }

        public static ValidatorWindow GetAndShowWindow()
        {
            if (ValidatorWindow.window == null)
            {
                ValidatorWindow.window = EditorWindow.GetWindow<ValidatorWindow>();
                ValidatorWindow.window.titleContent = new GUIContent("Validator");
                ValidatorWindow.window.minSize = new Vector2(600f, 500f);
            }
            ValidatorWindow.window.Show();
            return ValidatorWindow.window;
        }

        private void Indent(int indentions = 1)
        {
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Space((float)(15 * indentions));
        }

        private bool IsValidPath(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                this.DisplayIncorrectPathPopup("The selected path is outside of Project's Assets Folder.\n\nPlease select a location within Assets Folder.");
                return false;
            }
            if ((int)Directory.GetFileSystemEntries(path).Length != 0)
            {
                return true;
            }
            this.DisplayIncorrectPathPopup("The selected path is an empty folder.\n\nPlease ensure that the selected folder is not empty.");
            return false;
        }

        public void OnEnable()
        {
            if (!this.checkIcon)
            {
                this.checkIcon = EditorGUIUtility.IconContent("lightMeter/greenLight").image as Texture2D;
            }
            if (!this.errorIcon)
            {
                this.errorIcon = EditorGUIUtility.IconContent("lightMeter/redLight").image as Texture2D;
            }
            if (!this.warningIcon)
            {
                this.warningIcon = EditorGUIUtility.IconContent("lightMeter/orangeLight").image as Texture2D;
            }
        }

        public void OnGUI()
        {
            Checklist checkList = Checklist.GetCheckList();
            GUILayout.Space(10f);
            EditorGUILayout.LabelField("Package Validator", EditorStyles.boldLabel, new GUILayoutOption[0]);
            GUILayout.Space(2f);
            GUIStyle gUIStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };
            EditorGUILayout.LabelField("Scan your package to check that it does not have common package submission mistakes. Passing this scan does not guarantee that your package will get accepted as the final decision is made by the Unity Asset Store team.\n\nYou can upload your package even if it does not pass some of the criteria as it depends on the type of assets that you upload. For more information, view the messages next to the criteria in the checklist or contact our support team.", gUIStyle, new GUILayoutOption[0]);
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
            this.PackagePath = EditorGUILayout.TextField("Package path:", this.PackagePath, new GUILayoutOption[0]);
            GUI.SetNextControlName("Set Path");
            if (GUILayout.Button("Set Path", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                string str = EditorUtility.OpenFolderPanel("Select Package Folder", string.Empty, string.Empty);
                if (!string.IsNullOrEmpty(str))
                {
                    string projectRelativePath = ValidatorData.ToProjectRelativePath(str);
                    if (this.IsValidPath(projectRelativePath))
                    {
                        this.PackagePath = projectRelativePath;
                        GUI.FocusControl("Set Path");
                    }
                }
            }
            if (!string.IsNullOrEmpty(this.PackagePath))
            {
                ValidatorData.SetScanPath(this.PackagePath);
            }
            GUILayout.Space(15f);
            if (GUILayout.Button("Scan", new GUILayoutOption[] { GUILayout.Width(100f) }) && this.IsValidPath(this.PackagePath))
            {
                this.showChecklist = true;
                checkList.Scan();
                this.showErrorItems = true;
                this.showWarningItems = true;
                this.showPassItems = false;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider, new GUILayoutOption[0]);
            GUIStyle gUIStyle1 = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            if (!this.showChecklist)
            {
                GUILayout.Label("Scan the selected Package path to receive validation feedback.", gUIStyle1, new GUILayoutOption[0]);
            }
            else
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[0]);
                EditorGUILayout.LabelField("Checklist", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.MaxWidth(75f) });
                GUILayout.FlexibleSpace();
                this.showPassItems = GUILayout.Toggle(this.showPassItems, new GUIContent(this.checkIcon, "Passed"), EditorStyles.toolbarButton, new GUILayoutOption[] { GUILayout.Width(30f) });
                this.showWarningItems = GUILayout.Toggle(this.showWarningItems, new GUIContent(this.warningIcon, "Warnings"), EditorStyles.toolbarButton, new GUILayoutOption[] { GUILayout.Width(30f) });
                this.showErrorItems = GUILayout.Toggle(this.showErrorItems, new GUIContent(this.errorIcon, "Errors"), EditorStyles.toolbarButton, new GUILayoutOption[] { GUILayout.Width(30f) });
                EditorGUILayout.EndHorizontal();
                this.scrollPos = EditorGUILayout.BeginScrollView(this.scrollPos, new GUILayoutOption[0]);
                GUILayout.Space(6f);
                GUIStyle gUIStyle2 = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
                IEnumerable<ChecklistItem> checks =
                    from c in checkList.Checks
                    where c.Status == CheckStatus.Error & !c.Failed
                    select c;
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                this.showErrorItems = EditorGUILayout.Foldout(this.showErrorItems, string.Concat("     Errors (", checks.Count<ChecklistItem>(), ")"), gUIStyle2);
                this.DrawEntryIcon(this.errorIcon, 16);
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
                if (this.showErrorItems)
                {
                    IEnumerator<ChecklistItem> enumerator = checks.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            this.ChecklistItemGUI(enumerator.Current);
                            GUILayout.Space(6f);
                        }
                    }
                    finally
                    {
                        if (enumerator == null)
                        {
                        }
                        enumerator.Dispose();
                    }
                }
                checks =
                    from c in checkList.Checks
                    where (c.Status == CheckStatus.Warning ? true : c.Failed)
                    orderby c.Failed
                    select c;
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                this.showWarningItems = EditorGUILayout.Foldout(this.showWarningItems, string.Concat("     Warnings (", checks.Count<ChecklistItem>(), ")"), gUIStyle2);
                this.DrawEntryIcon(this.warningIcon, 16);
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
                if (this.showWarningItems)
                {
                    IEnumerator<ChecklistItem> enumerator1 = checks.GetEnumerator();
                    try
                    {
                        while (enumerator1.MoveNext())
                        {
                            this.ChecklistItemGUI(enumerator1.Current);
                            GUILayout.Space(6f);
                        }
                    }
                    finally
                    {
                        if (enumerator1 == null)
                        {
                        }
                        enumerator1.Dispose();
                    }
                }
                checks =
                    from c in checkList.Checks
                    where c.Status == CheckStatus.Pass & !c.Failed
                    select c;
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                this.showPassItems = EditorGUILayout.Foldout(this.showPassItems, string.Concat("     Passed (", checks.Count<ChecklistItem>(), ")"), gUIStyle2);
                this.DrawEntryIcon(this.checkIcon, 16);
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
                if (this.showPassItems)
                {
                    IEnumerator<ChecklistItem> enumerator2 = checks.GetEnumerator();
                    try
                    {
                        while (enumerator2.MoveNext())
                        {
                            this.ChecklistItemGUI(enumerator2.Current);
                            GUILayout.Space(6f);
                        }
                    }
                    finally
                    {
                        if (enumerator2 == null)
                        {
                        }
                        enumerator2.Dispose();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void SelectAndPing(List<string> paths)
        {
            IEnumerable<UnityEngine.Object> objs =
                from f in paths
                select AssetDatabase.LoadMainAssetAtPath(f);
            Selection.objects = objs.ToArray<UnityEngine.Object>();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        [MenuItem("Asset Store Tools/Package Validator", false, 3)]
        public static void ShowWindow()
        {
            if (ValidatorWindow.window == null)
            {
                ValidatorWindow.window = EditorWindow.GetWindow<ValidatorWindow>();
                ValidatorWindow.window.titleContent = new GUIContent("Validator");
                ValidatorWindow.window.minSize = new Vector2(600f, 500f);
            }
            ValidatorWindow.window.Show();
        }
    }
}