using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal class LoginWindow : EditorWindow
{
    public const float kDefaultWidth = 360f;

    public const float kDefaultHeight = 180f;

    public const float Padding = 3f;

    public const float Margin = 10f;

    private const int m_MaxMessageLength = 80;

    public static bool IsVisible;

    private string m_LoginReason;

    private string m_LoginRemoteMessage;

    private string m_Email = string.Empty;

    private string m_Password = string.Empty;

    private LoginWindow.LoginCallback m_LoginCallback;

    public static bool IsLoggedIn
    {
        get { return AssetStoreClient.HasActiveSessionID; }
    }

    static LoginWindow()
    {
    }

    public LoginWindow()
    {
    }

    private bool IsValidEmail(string email)
    {
        bool flag;
        try
        {
            flag = Regex.IsMatch(email, "^(.+)@(.+){1,}\\.(.+)$");
        }
        catch
        {
            flag = false;
        }

        return flag;
    }

    public static void Login(string loginReason, LoginWindow.LoginCallback callback)
    {
        LoginWindow.Login(loginReason, callback, new Rect(100f, 100f, 360f, 180f));
    }

    public static void Login(string loginReason, LoginWindow.LoginCallback callback, Rect windowRect)
    {
        if (AssetStoreClient.HasActiveSessionID)
        {
            AssetStoreClient.Logout();
        }

        if (AssetStoreClient.RememberSession && AssetStoreClient.HasSavedSessionID)
        {
            AssetStoreClient.LoginWithRememberedSession((string errorMessage) =>
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    LoginWindow.ShowLoginWindow(loginReason, callback, windowRect);
                }
                else
                {
                    callback(errorMessage);
                }
            });
            return;
        }

        LoginWindow.ShowLoginWindow(loginReason, callback, windowRect);
    }

    private void Login()
    {
        this.m_LoginRemoteMessage = null;
        if (AssetStoreClient.HasActiveSessionID)
        {
            AssetStoreClient.Logout();
        }

        if (string.IsNullOrEmpty(this.m_Email))
        {
            this.m_LoginRemoteMessage = "Please enter your email address.";
            this.Repaint();
            return;
        }

        if (!this.IsValidEmail(this.m_Email))
        {
            this.m_LoginRemoteMessage = "Invalid email address.";
            this.Repaint();
            return;
        }

        if (string.IsNullOrEmpty(this.m_Password))
        {
            this.m_LoginRemoteMessage = "Please enter your password.";
            this.Repaint();
            return;
        }

        AssetStoreClient.LoginWithCredentials(this.m_Email, this.m_Password, AssetStoreClient.RememberSession, (string errorMessage) =>
        {
            this.m_LoginRemoteMessage = errorMessage;
            if (errorMessage != null)
            {
                this.Repaint();
            }
            else
            {
                if (this.m_LoginCallback != null)
                {
                    this.m_LoginCallback(this.m_LoginRemoteMessage);
                }

                base.Close();
            }
        });
    }

    public static void Logout()
    {
        AssetStoreClient.Logout();
    }

    public void OnDisable()
    {
        LoginWindow.IsVisible = false;
        this.m_LoginRemoteMessage = "Cancelled";
        this.m_LoginCallback = null;
        this.m_Password = null;
    }

    public void OnEnabled()
    {
        LoginWindow.IsVisible = true;
    }

    public void OnGUI()
    {
        if (AssetStoreClient.LoginInProgress() || LoginWindow.IsLoggedIn)
        {
            GUI.enabled = false;
        }

        GUILayout.BeginVertical(new GUILayoutOption[0]);
        GUILayout.Space(10f);
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.Space(10f);
        GUILayout.BeginVertical(new GUILayoutOption[0]);
        GUILayout.FlexibleSpace();
        GUILayout.Label(GUIUtil.Logo, GUIStyle.none, new GUILayoutOption[] {GUILayout.Width(80f), GUILayout.Height(80f)});
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.BeginVertical(new GUILayoutOption[0]);
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        if (this.m_LoginReason.Length > 80)
        {
            this.m_LoginReason = string.Concat(this.m_LoginReason.Substring(0, 80), "...");
        }

        GUILayout.Label(this.m_LoginReason, EditorStyles.wordWrappedLabel, new GUILayoutOption[0]);
        GUILayout.EndHorizontal();
        GUILayout.Space(3f);
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        if (this.m_LoginRemoteMessage != null)
        {
            Color color = GUI.color;
            GUI.color = GUIUtil.ErrorColor;
            if (this.m_LoginRemoteMessage.Length > 80)
            {
                this.m_LoginRemoteMessage = string.Concat(this.m_LoginRemoteMessage.Substring(0, 80), "...");
            }

            GUILayout.Label(this.m_LoginRemoteMessage, EditorStyles.wordWrappedLabel, new GUILayoutOption[0]);
            GUI.color = color;
        }

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.Label("Email", new GUILayoutOption[] {GUILayout.Width(62f)});
        this.m_Email = EditorGUILayout.TextField(this.m_Email, new GUILayoutOption[0]);
        GUILayout.EndHorizontal();
        GUILayout.Space(3f);
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.Label("Password", new GUILayoutOption[] {GUILayout.Width(62f)});
        this.m_Password = EditorGUILayout.PasswordField(this.m_Password, new GUILayoutOption[0]);
        GUILayout.EndHorizontal();
        GUILayout.Space(3f);
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.BeginVertical(new GUILayoutOption[0]);
        GUILayout.FlexibleSpace();
        bool rememberSession = AssetStoreClient.RememberSession;
        bool flag = EditorGUILayout.ToggleLeft("Remember me", rememberSession, new GUILayoutOption[] {GUILayout.MaxWidth(160f)});
        if (flag != rememberSession)
        {
            AssetStoreClient.RememberSession = flag;
        }

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical(new GUILayoutOption[0]);
        Color color1 = GUI.color;
        GUI.color = Color.grey;
        if (GUILayout.Button("Forgot password?", EditorStyles.miniLabel, new GUILayoutOption[0]))
        {
            Application.OpenURL("https://accounts.unity3d.com/password/new");
        }

        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
        GUI.color = color1;
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.Space(10f);
        GUILayout.EndHorizontal();
        GUILayout.Space(3f);
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.Space(10f);
        if (GUILayout.Button("Create Publisher ID", new GUILayoutOption[0]))
        {
            Application.OpenURL(string.Concat("https://publisher.assetstore.unity3d.com/?xunitysession=", AssetStoreClient.XUnitySession));
            this.m_LoginRemoteMessage = "Cancelled - creating Publisher ID";
            base.Close();
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Cancel", new GUILayoutOption[0]))
        {
            this.m_LoginRemoteMessage = "Cancelled";
            base.Close();
        }

        GUILayout.Space(3f);
        if (GUILayout.Button("Login", new GUILayoutOption[0]))
        {
            this.Login();
            this.Repaint();
        }

        GUILayout.Space(10f);
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);
        GUILayout.EndVertical();
        if (Event.current.Equals(Event.KeyboardEvent("return")))
        {
            this.Login();
            this.Repaint();
        }
    }

    public static void ShowLoginWindow(string loginReason, string errorMessage, LoginWindow.LoginCallback callback)
    {
        LoginWindow.ShowLoginWindow(loginReason, errorMessage, callback, new Rect(100f, 100f, 360f, 180f));
    }

    private static void ShowLoginWindow(string loginReason, LoginWindow.LoginCallback callback, Rect windowRect)
    {
        LoginWindow.ShowLoginWindow(loginReason, null, callback, new Rect(100f, 100f, 360f, 180f));
    }

    private static void ShowLoginWindow(string loginReason, string errorMessage, LoginWindow.LoginCallback callback, Rect windowRect)
    {
        LoginWindow.IsVisible = true;
        LoginWindow windowWithRect = (LoginWindow) EditorWindow.GetWindowWithRect(typeof(LoginWindow), windowRect, true, "Publisher Login");
        windowWithRect.position = windowRect;
        windowWithRect.m_Password = string.Empty;
        windowWithRect.m_LoginCallback = callback;
        windowWithRect.m_LoginReason = loginReason;
        windowWithRect.m_LoginRemoteMessage = errorMessage;
        windowWithRect.Show();
    }

    public delegate void LoginCallback(string errorMessage);
}