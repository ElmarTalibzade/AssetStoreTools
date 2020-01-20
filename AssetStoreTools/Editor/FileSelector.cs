using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FileSelector : EditorWindow
{
	private string m_Directory;

	private FileSelector.FileNode m_RootDir;

	private LinkedList<FileSelector.FileNode> m_SelectedFiles;

	private Vector2 m_FileScrollPos;

	private Vector2 m_FileSelectedScrollPos;

	private FileSelector.DoneCallback m_OnFinishSelecting;

	public FileSelector()
	{
	}

	public void Accept()
	{
		List<string> strs = new List<string>();
		foreach (FileSelector.FileNode mSelectedFile in this.m_SelectedFiles)
		{
			strs.Add(mSelectedFile.Name);
		}
		this.m_OnFinishSelecting(strs);
		base.Close();
	}

	private LinkedList<FileSelector.FileNode> GetFileListByName(IList<string> selected)
	{
		int num = 0;
		LinkedList<FileSelector.FileNode> fileNodes = new LinkedList<FileSelector.FileNode>();
		LinkedList<FileSelector.FileNode> fileNodes1 = new LinkedList<FileSelector.FileNode>();
		fileNodes1.AddFirst(this.m_RootDir);
		LinkedList<FileSelector.FileNode> fileNodes2 = new LinkedList<FileSelector.FileNode>();
		while (fileNodes1.Count > 0 && num < selected.Count)
		{
			LinkedListNode<FileSelector.FileNode> first = fileNodes1.First;
			fileNodes2.Clear();
			if (!first.Value.isDirectory)
			{
				if (first.Value.Name == selected[num])
				{
					num++;
					fileNodes.AddLast(first.Value);
				}
				else if (first.Value.Name.CompareTo(selected[num]) > 0)
				{
					num++;
				}
				if (num >= selected.Count)
				{
					return fileNodes;
				}
			}
			else
			{
				foreach (FileSelector.FileNode children in first.Value.Childrens)
				{
					fileNodes2.AddFirst(children);
				}
			}
			fileNodes1.RemoveFirst();
			foreach (FileSelector.FileNode fileNode in fileNodes2)
			{
				fileNodes1.AddFirst(fileNode);
			}
		}
		return fileNodes;
	}

	public void Init(string directory, List<string> preSelectedFiles, FileSelector.DoneCallback onFinishSelecting)
	{
		this.m_Directory = directory;
		this.m_FileScrollPos = new Vector2();
		this.m_FileSelectedScrollPos = new Vector2();
		this.m_OnFinishSelecting = onFinishSelecting;
		string str = string.Concat(Application.dataPath, this.m_Directory);
		this.m_RootDir = new FileSelector.FileNode(new DirectoryInfo(str), 0);
		preSelectedFiles.Sort();
		this.SelectFiles(preSelectedFiles);
	}

	public void OnGUI()
	{
		Rect rect = base.position;
		int num = (int)Math.Floor((double)(rect.width / 2f));
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		GUILayout.Label("Main Assets", EditorStyles.boldLabel, new GUILayoutOption[0]);
		GUILayout.Label("Please select from the list below the main assets in your package. You should select items that you consider to be the central parts of you package, and that would showcase your package. The Asset Store will generate previews for the selected items. If you are uploading a Character, the Character prefab would be a good candidate for instance", EditorStyles.wordWrappedLabel, new GUILayoutOption[0]);
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width((float)num) });
		GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
		GUILayout.Label("Package Files", EditorStyles.miniLabel, new GUILayoutOption[0]);
		GUILayout.EndHorizontal();
		this.m_FileScrollPos = EditorGUILayout.BeginScrollView(this.m_FileScrollPos, new GUILayoutOption[0]);
		this.RenderFileTree();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.Box(GUIContent.none, GUIUtil.Styles.verticalDelimiter, new GUILayoutOption[] { GUILayout.MinWidth(1f), GUILayout.ExpandHeight(true) });
		GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width((float)num) });
		GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
		GUILayout.Label("Selected Files", EditorStyles.miniLabel, new GUILayoutOption[0]);
		GUILayout.EndHorizontal();
		this.m_FileSelectedScrollPos = EditorGUILayout.BeginScrollView(this.m_FileSelectedScrollPos, new GUILayoutOption[0]);
		this.RenderSelectedFileList();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.Box(GUIContent.none, GUIUtil.Styles.delimiter, new GUILayoutOption[] { GUILayout.MinHeight(1f), GUILayout.ExpandWidth(true) });
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Done", new GUILayoutOption[] { GUILayout.Width(100f), GUILayout.Height(30f) }))
		{
			this.Accept();
		}
		GUILayout.EndHorizontal();
	}

	private void RenderFileTree()
	{
		LinkedList<FileSelector.FileNode> fileNodes = new LinkedList<FileSelector.FileNode>();
		fileNodes.AddFirst(this.m_RootDir);
		LinkedList<FileSelector.FileNode> fileNodes1 = new LinkedList<FileSelector.FileNode>();
		while (fileNodes.Count > 0)
		{
			LinkedListNode<FileSelector.FileNode> first = fileNodes.First;
			fileNodes1.Clear();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Space((float)(20 * first.Value.Depth));
			if (!first.Value.isDirectory)
			{
				bool flag = GUILayout.Toggle(first.Value.Selected, GUIContent.none, new GUILayoutOption[0]);
				if (flag != first.Value.Selected)
				{
					if (!flag)
					{
						this.m_SelectedFiles.Remove(first.Value);
					}
					else
					{
						this.m_SelectedFiles.AddLast(first.Value);
					}
					first.Value.Selected = flag;
				}
			}
			else
			{
				GUIStyle gUIStyle = "IN foldout";
				first.Value.Expanded = GUILayout.Toggle(first.Value.Expanded, GUIContent.none, gUIStyle, new GUILayoutOption[0]);
			}
			first.Value.RenderIconText();
			GUILayout.EndHorizontal();
			if (first.Value.Expanded)
			{
				foreach (FileSelector.FileNode subDirectory in first.Value.SubDirectories)
				{
					fileNodes1.AddFirst(subDirectory);
				}
				foreach (FileSelector.FileNode file in first.Value.Files)
				{
					fileNodes1.AddFirst(file);
				}
			}
			fileNodes.RemoveFirst();
			foreach (FileSelector.FileNode fileNode in fileNodes1)
			{
				fileNodes.AddFirst(fileNode);
			}
		}
	}

	private void RenderSelectedFileList()
	{
		LinkedListNode<FileSelector.FileNode> first = this.m_SelectedFiles.First;
		while (first != null)
		{
			FileSelector.FileNode value = first.Value;
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			value.Selected = GUILayout.Toggle(value.Selected, GUIContent.none, new GUILayoutOption[0]);
			value.RenderIconText();
			if (MainAssetsUtil.CanPreview && GUILayout.Button("Preview", new GUILayoutOption[0]))
			{
				MainAssetsUtil.Preview(value.Name);
			}
			if (value.Selected)
			{
				first = first.Next;
				GUILayout.EndHorizontal();
			}
			else
			{
				LinkedListNode<FileSelector.FileNode> linkedListNode = first;
				first = first.Next;
				this.m_SelectedFiles.Remove(linkedListNode);
			}
		}
	}

	public void SelectFiles(IList<string> toBeSelected)
	{
		this.m_SelectedFiles = this.GetFileListByName(toBeSelected);
		foreach (FileSelector.FileNode mSelectedFile in this.m_SelectedFiles)
		{
			mSelectedFile.Selected = true;
		}
	}

	public static FileSelector Show(string directory, List<string> preSelectedFiles, FileSelector.DoneCallback onFinishSelecting)
	{
		FileSelector window = EditorWindow.GetWindow(typeof(FileSelector), true, "Please Select Main Assets") as FileSelector;
		window.minSize = new Vector2(400f, 300f);
		if (!directory.EndsWith("/"))
		{
			directory = string.Concat(directory, "/");
		}
		window.Init(directory, preSelectedFiles, onFinishSelecting);
		window.Show();
		return window;
	}

	private class AlphabeticComparer : IComparer<FileSelector.FileNode>
	{
		public AlphabeticComparer()
		{
		}

		public int Compare(FileSelector.FileNode a, FileSelector.FileNode b)
		{
			return a.Name.CompareTo(b.Name);
		}
	}

	public delegate void DoneCallback(List<string> updatedMainAssets);

	private class FileNode
	{
		private static IComparer<FileSelector.FileNode> alphabeticalComparer;

		private string m_RelativePath;

		private string m_Name;

		private bool m_isDir;

		private List<FileSelector.FileNode> m_SubDirectories;

		private List<FileSelector.FileNode> m_SubFiles;

		private List<FileSelector.FileNode> m_Children;

		private Texture m_Icon;

		private bool m_Expanded;

		private bool m_Selected;

		private int m_Depth;

		public List<FileSelector.FileNode> Childrens
		{
			get
			{
				return this.m_Children;
			}
		}

		public int Depth
		{
			get
			{
				return this.m_Depth;
			}
		}

		public bool Expanded
		{
			get
			{
				return this.m_Expanded;
			}
			set
			{
				this.m_Expanded = value;
			}
		}

		public List<FileSelector.FileNode> Files
		{
			get
			{
				return this.m_SubFiles;
			}
		}

		public bool isDirectory
		{
			get
			{
				return this.m_isDir;
			}
		}

		public string Name
		{
			get
			{
				return this.m_RelativePath;
			}
		}

		public bool Selected
		{
			get
			{
				return this.m_Selected;
			}
			set
			{
				this.m_Selected = value;
			}
		}

		public List<FileSelector.FileNode> SubDirectories
		{
			get
			{
				return this.m_SubDirectories;
			}
		}

		static FileNode()
		{
			FileSelector.FileNode.alphabeticalComparer = new FileSelector.AlphabeticComparer();
		}

		public FileNode(FileSystemInfo fileInfo, int depth = 0)
		{
			string fullName = fileInfo.FullName;
			this.m_Name = fileInfo.Name;
			this.m_Depth = depth;
			this.m_RelativePath = string.Concat("Assets/", fullName.Substring(Application.dataPath.Length + 1));
			this.m_RelativePath = this.m_RelativePath.Replace("\\", "/");
			this.m_SubFiles = new List<FileSelector.FileNode>();
			this.m_SubDirectories = new List<FileSelector.FileNode>();
			this.m_Children = new List<FileSelector.FileNode>();
			if (!(fileInfo is DirectoryInfo))
			{
				this.m_Icon = AssetDatabase.GetCachedIcon(this.m_RelativePath) as Texture2D;
				if (!this.m_Icon)
				{
					this.m_Icon = EditorGUIUtility.ObjectContent(null, typeof(MonoBehaviour)).image;
				}
				return;
			}
			this.m_Icon = EditorGUIUtility.FindTexture("_Folder");
			if (this.m_Icon == null)
			{
				this.m_Icon = EditorGUIUtility.FindTexture("Folder Icon");
			}
			this.m_isDir = true;
			FileSystemInfo[] fileSystemInfos = (fileInfo as DirectoryInfo).GetFileSystemInfos();
			for (int i = 0; i < (int)fileSystemInfos.Length; i++)
			{
				FileSystemInfo fileSystemInfo = fileSystemInfos[i];
				if (!fileSystemInfo.Name.EndsWith(".meta") && !fileSystemInfo.Name.StartsWith(".") && !fileSystemInfo.Name.EndsWith(".unity"))
				{
					FileSelector.FileNode fileNode = new FileSelector.FileNode(fileSystemInfo, this.m_Depth + 1);
					if (!(fileSystemInfo is DirectoryInfo))
					{
						this.m_SubFiles.Add(fileNode);
					}
					else
					{
						this.m_SubDirectories.Add(fileNode);
					}
					this.m_Children.Add(fileNode);
				}
			}
			this.m_Children.Sort(FileSelector.FileNode.alphabeticalComparer);
		}

		public void RenderIconText()
		{
			GUIContent gUIContent = new GUIContent()
			{
				image = this.m_Icon,
				text = this.m_Name
			};
			GUILayout.Label(gUIContent.image, new GUILayoutOption[] { GUILayout.Height(21f), GUILayout.Width(21f) });
			GUILayout.Label(gUIContent.text, new GUILayoutOption[] { GUILayout.Height(21f) });
			GUILayout.FlexibleSpace();
		}
	}
}