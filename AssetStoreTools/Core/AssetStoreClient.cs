using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

internal static class AssetStoreClient
{
    public const string TOOL_VERSION = "V5.0.0";

    private const string ASSET_STORE_PROD_URL = "https://kharma.unity3d.com";

    private const string UNAUTH_SESSION_ID = "26c4202eb475d02864b40827dfff11a14657aa41";

    private const int kClientPoolSize = 5;

    private const int kSendBufferSize = 32768;

    private static string sActiveSessionIdBackwardsCompatibility;

    private static AssetStoreClient.LoginState sLoginState;

    private static string sLoginErrorMessage;

    private static Stack<AssetStoreWebClient> sClientPool;

    private static List<AssetStoreClient.LargeFilePending> s_PendingLargeFiles;

    private static AssetStoreClient.LargeFilePending s_UploadingLargeFile;

    private static List<AssetStoreClient.Pending> pending;

    private static string ActiveOrUnauthSessionID
    {
        get
        {
            string activeSessionID = AssetStoreClient.ActiveSessionID;
            if (activeSessionID == string.Empty)
            {
                return "26c4202eb475d02864b40827dfff11a14657aa41";
            }
            return activeSessionID;
        }
    }

    private static string ActiveSessionID
    {
        get
        {
            Assembly assembly = Assembly.Load("UnityEditor");
            MethodInfo method = assembly.GetType("UnityEditor.AssetStoreContext").GetMethod("SessionGetString");
            string savedSessionID = AssetStoreClient.SavedSessionID;
            if (method != null)
            {
                savedSessionID = (string)method.Invoke(null, new object[] { "kharma.active_sessionid" });
            }
            else if (string.IsNullOrEmpty(savedSessionID))
            {
                savedSessionID = AssetStoreClient.sActiveSessionIdBackwardsCompatibility;
            }
            if (savedSessionID != null)
            {
                return savedSessionID;
            }
            return string.Empty;
        }
        set
        {
            Assembly assembly = Assembly.Load("UnityEditor");
            MethodInfo method = assembly.GetType("UnityEditor.AssetStoreContext").GetMethod("SessionSetString");
            if (method == null)
            {
                if (AssetStoreManager.sDbg && string.IsNullOrEmpty(AssetStoreClient.sActiveSessionIdBackwardsCompatibility))
                {
                    DebugUtils.Log("Backwards compatibility mode asset store set session");
                }
                AssetStoreClient.sActiveSessionIdBackwardsCompatibility = value;
            }
            else
            {
                method.Invoke(null, new object[] { "kharma.active_sessionid", value });
            }
        }
    }

    private static string AssetStoreUrl
    {
        get
        {
            DebugUtils.Log(EditorPrefs.GetString("kharma.server", string.Empty));
            if (!string.IsNullOrEmpty(EditorPrefs.GetString("kharma.server", string.Empty)))
            {
                Match match = Regex.Match(EditorPrefs.GetString("kharma.server", string.Empty), "(.*?//[^/]+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            string str = File.ReadAllText(string.Concat(EditorApplication.applicationContentsPath, "/Resources/loader.html"));
            Match match1 = Regex.Match(str, "location.href.*?=.*?'(.*?//[^/']+)");
            if (!match1.Success)
            {
                return "https://kharma.unity3d.com";
            }
            return match1.Groups[1].Value;
        }
    }

    public static bool HasActiveSessionID
    {
        get
        {
            return !string.IsNullOrEmpty(AssetStoreClient.ActiveSessionID);
        }
    }

    public static bool HasSavedSessionID
    {
        get
        {
            return !string.IsNullOrEmpty(AssetStoreClient.SavedSessionID);
        }
    }

    public static string LoginErrorMessage
    {
        get
        {
            return AssetStoreClient.sLoginErrorMessage;
        }
    }

    public static bool RememberSession
    {
        get
        {
            return EditorPrefs.GetString("kharma.remember_session") == "1";
        }
        set
        {
            EditorPrefs.SetString("kharma.remember_session", (!value ? "0" : "1"));
        }
    }

    private static string SavedSessionID
    {
        get
        {
            if (!AssetStoreClient.RememberSession)
            {
                return string.Empty;
            }
            return EditorPrefs.GetString("kharma.sessionid", string.Empty);
        }
        set
        {
            EditorPrefs.SetString("kharma.sessionid", value);
        }
    }

    private static string UserIconUrl
    {
        get;
        set;
    }

    public static string XUnitySession
    {
        get
        {
            return AssetStoreClient.ActiveOrUnauthSessionID;
        }
    }

    static AssetStoreClient()
    {
        AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGGED_OUT;
        AssetStoreClient.sLoginErrorMessage = null;
        AssetStoreClient.s_PendingLargeFiles = new List<AssetStoreClient.LargeFilePending>();
        AssetStoreClient.s_UploadingLargeFile = null;
        AssetStoreClient.pending = new List<AssetStoreClient.Pending>();
        ServicePointManager.ServerCertificateValidationCallback = (object argument0, X509Certificate argument1, X509Chain argument2, SslPolicyErrors argument3) => true;
    }

    public static void Abort(string name)
    {
        AssetStoreClient.pending.RemoveAll((AssetStoreClient.Pending p) =>
        {
            if (p.id != name)
            {
                return false;
            }
            if (p.conn.IsBusy)
            {
                p.conn.CancelAsync();
            }
            return true;
        });
    }

    public static void Abort(AssetStoreClient.Pending removePending)
    {
        AssetStoreClient.pending.RemoveAll((AssetStoreClient.Pending p) =>
        {
            if (p != removePending)
            {
                return false;
            }
            if (p.conn.IsBusy)
            {
                p.conn.CancelAsync();
            }
            return true;
        });
    }

    public static void AbortLargeFilesUpload()
    {
        if (AssetStoreClient.s_PendingLargeFiles.Count == 0)
        {
            return;
        }
        AssetStoreClient.s_PendingLargeFiles.RemoveAll((AssetStoreClient.LargeFilePending assetUpload) =>
        {
            if (assetUpload == null)
            {
                return true;
            }
            AssetStoreResponse assetStoreResponse = AssetStoreClient.parseAssetStoreResponse(null, null, null, null);
            assetStoreResponse.HttpStatusCode = -2;
            if (assetUpload.RequestDoneCallback != null)
            {
                assetUpload.RequestDoneCallback(assetStoreResponse);
            }
            assetUpload.Close();
            return true;
        });
        AssetStoreClient.s_UploadingLargeFile = null;
    }

    private static AssetStoreWebClient AcquireClient()
    {
        AssetStoreClient.InitClientPool();
        if (AssetStoreClient.sClientPool.Count == 0)
        {
            return null;
        }
        return AssetStoreClient.sClientPool.Pop();
    }

    private static Uri APIUri(string path)
    {
        return AssetStoreClient.APIUri(path, null);
    }

    private static Uri APIUri(string path, IDictionary<string, string> extraQuery)
    {
        Dictionary<string, string> strs;
        strs = (extraQuery == null ? new Dictionary<string, string>() : new Dictionary<string, string>(extraQuery));
        strs.Add("unityversion", Application.unityVersion);
        strs.Add("toolversion", "V5.0.0");
        strs.Add("xunitysession", AssetStoreClient.ActiveOrUnauthSessionID);
        UriBuilder uriBuilder = new UriBuilder(AssetStoreClient.GetProperPath(path));
        StringBuilder stringBuilder = new StringBuilder();
        foreach (KeyValuePair<string, string> keyValuePair in strs)
        {
            string key = keyValuePair.Key;
            string str = Uri.EscapeDataString(keyValuePair.Value);
            stringBuilder.AppendFormat("&{0}={1}", key, str);
        }
        if (string.IsNullOrEmpty(uriBuilder.Query))
        {
            uriBuilder.Query = stringBuilder.Remove(0, 1).ToString();
        }
        else
        {
            uriBuilder.Query = string.Concat(uriBuilder.Query.Substring(1), stringBuilder);
        }
        DebugUtils.Log(string.Concat("preparing: ", uriBuilder.Uri));
        return uriBuilder.Uri;
    }

    private static AssetStoreClient.Pending CreatePending(string name, AssetStoreClient.DoneCallback callback)
    {
        foreach (AssetStoreClient.Pending pending in AssetStoreClient.pending)
        {
            if (pending.id != name)
            {
                continue;
            }
            DebugUtils.Log("CreatePending name conflict!");
        }
        AssetStoreClient.Pending pending1 = new AssetStoreClient.Pending()
        {
            id = name,
            callback = callback
        };
        AssetStoreClient.pending.Add(pending1);
        return pending1;
    }

    public static AssetStoreClient.Pending CreatePendingGet(string name, string path, AssetStoreClient.DoneCallback callback)
    {
        AssetStoreClient.Pending pending = AssetStoreClient.CreatePending(name, callback);
        AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = () =>
        {
            bool flag;
            pending.conn = AssetStoreClient.AcquireClient();
            if (pending.conn == null)
            {
                return false;
            }
            try
            {
                pending.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                pending.conn.DownloadProgressChanged += new DownloadProgressChangedEventHandler(AssetStoreClient.DownloadProgressCallback);
                pending.conn.DownloadStringCompleted += new DownloadStringCompletedEventHandler(AssetStoreClient.DownloadStringCallback);
                pending.conn.DownloadStringAsync(AssetStoreClient.APIUri(path), pending);
                return true;
            }
            catch (WebException webException)
            {
                pending.ex = webException;
                flag = false;
            }
            return flag;
        };
        if (!pendingQueueDelegate())
        {
            pending.queueDelegate = pendingQueueDelegate;
        }
        return pending;
    }

    public static AssetStoreClient.Pending CreatePendingGetBinary(string name, string url, AssetStoreClient.DoneCallback callback)
    {
        AssetStoreClient.Pending pending = AssetStoreClient.CreatePending(name, callback);
        AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = () =>
        {
            bool flag;
            pending.conn = AssetStoreClient.AcquireClient();
            if (pending.conn == null)
            {
                return false;
            }
            try
            {
                pending.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                pending.conn.DownloadProgressChanged += new DownloadProgressChangedEventHandler(AssetStoreClient.DownloadProgressCallback);
                pending.conn.DownloadDataCompleted += new DownloadDataCompletedEventHandler(AssetStoreClient.DownloadDataCallback);
                pending.conn.DownloadDataAsync(new Uri(url), pending);
                return true;
            }
            catch (WebException webException)
            {
                pending.ex = webException;
                flag = false;
            }
            return flag;
        };
        if (!pendingQueueDelegate())
        {
            pending.queueDelegate = pendingQueueDelegate;
        }
        return pending;
    }

    public static AssetStoreClient.Pending CreatePendingPost(string name, string path, NameValueCollection param, AssetStoreClient.DoneCallback callback)
    {
        AssetStoreClient.Pending pending = AssetStoreClient.CreatePending(name, callback);
        AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = () =>
        {
            bool flag;
            pending.conn = AssetStoreClient.AcquireClient();
            if (pending.conn == null)
            {
                return false;
            }
            try
            {
                pending.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                pending.conn.UploadProgressChanged += new UploadProgressChangedEventHandler(AssetStoreClient.UploadProgressCallback);
                pending.conn.UploadValuesCompleted += new UploadValuesCompletedEventHandler(AssetStoreClient.UploadValuesCallback);
                pending.conn.UploadValuesAsync(AssetStoreClient.APIUri(path), "POST", param, pending);
                return true;
            }
            catch (WebException webException)
            {
                pending.ex = webException;
                flag = false;
            }
            return flag;
        };
        if (!pendingQueueDelegate())
        {
            pending.queueDelegate = pendingQueueDelegate;
        }
        return pending;
    }

    public static AssetStoreClient.Pending CreatePendingPost(string name, string path, string postData, AssetStoreClient.DoneCallback callback)
    {
        AssetStoreClient.Pending pending = AssetStoreClient.CreatePending(name, callback);
        AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = () =>
        {
            bool flag;
            pending.conn = AssetStoreClient.AcquireClient();
            if (pending.conn == null)
            {
                return false;
            }
            try
            {
                pending.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                pending.conn.UploadProgressChanged += new UploadProgressChangedEventHandler(AssetStoreClient.UploadProgressCallback);
                pending.conn.UploadStringCompleted += new UploadStringCompletedEventHandler(AssetStoreClient.UploadStringCallback);
                pending.conn.UploadStringAsync(AssetStoreClient.APIUri(path), "POST", postData, pending);
                return true;
            }
            catch (WebException webException)
            {
                pending.ex = webException;
                flag = false;
            }
            return flag;
        };
        if (!pendingQueueDelegate())
        {
            pending.queueDelegate = pendingQueueDelegate;
        }
        return pending;
    }

    public static AssetStoreClient.Pending CreatePendingUpload(string name, string path, string filepath, AssetStoreClient.DoneCallback callback)
    {
        DebugUtils.Log("CreatePendingUpload");
        AssetStoreClient.Pending pending = AssetStoreClient.CreatePending(name, callback);
        AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = () =>
        {
            bool flag;
            pending.conn = AssetStoreClient.AcquireClient();
            if (pending.conn == null)
            {
                return false;
            }
            try
            {
                pending.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                pending.conn.UploadProgressChanged += new UploadProgressChangedEventHandler(AssetStoreClient.UploadProgressCallback);
                pending.conn.UploadFileCompleted += new UploadFileCompletedEventHandler(AssetStoreClient.UploadFileCallback);
                pending.conn.UploadFileAsync(AssetStoreClient.APIUri(path), "PUT", filepath, pending);
                return true;
            }
            catch (WebException webException)
            {
                pending.ex = webException;
                flag = false;
            }
            return flag;
        };
        if (!pendingQueueDelegate())
        {
            pending.queueDelegate = pendingQueueDelegate;
        }
        return pending;
    }

    public static NameValueCollection Dict2Params(Dictionary<string, string> d)
    {
        NameValueCollection nameValueCollection = new NameValueCollection();
        foreach (KeyValuePair<string, string> keyValuePair in d)
        {
            nameValueCollection.Add(keyValuePair.Key, keyValuePair.Value);
        }
        return nameValueCollection;
    }

    private static void DownloadDataCallback(object sender, DownloadDataCompletedEventArgs e)
    {
        AssetStoreClient.Pending userState = (AssetStoreClient.Pending)e.UserState;
        if (e.Error != null)
        {
            userState.ex = e.Error;
            return;
        }
        if (e.Cancelled)
        {
            userState.binData = new byte[0];
        }
        else
        {
            userState.bytesReceived = userState.totalBytesToReceive;
            userState.statsUpdated = false;
            userState.binData = e.Result;
        }
    }

    private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
    {
        AssetStoreClient.Pending userState = (AssetStoreClient.Pending)e.UserState;
        userState.bytesReceived = (uint)e.BytesReceived;
        userState.totalBytesToReceive = (uint)e.TotalBytesToReceive;
        userState.bytesSend = 0;
        userState.totalBytesToSend = 0;
        userState.statsUpdated = true;
        Console.WriteLine("{0} downloaded {1} of {2} bytes. {3} % complete...", new object[] { userState.id, e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage });
    }

    private static void DownloadStringCallback(object sender, DownloadStringCompletedEventArgs e)
    {
        AssetStoreClient.Pending userState = (AssetStoreClient.Pending)e.UserState;
        if (e.Error != null)
        {
            userState.ex = e.Error;
            return;
        }
        if (e.Cancelled)
        {
            userState.data = string.Empty;
        }
        else
        {
            userState.bytesReceived = userState.totalBytesToReceive;
            userState.statsUpdated = false;
            userState.data = e.Result;
        }
    }

    private static string GetHardwareHash()
    {
        return InternalEditorUtility.GetAuthToken().Substring(40, 40);
    }

    private static string GetLicenseHash()
    {
        return InternalEditorUtility.GetAuthToken().Substring(0, 40);
    }

    private static string GetProperPath(string partialPath)
    {
        return string.Format("{0}/api/asset-store-tools{1}.json", AssetStoreClient.AssetStoreUrl, partialPath);
    }

    private static void InitClientPool()
    {
        if (AssetStoreClient.sClientPool != null)
        {
            return;
        }
        AssetStoreClient.sClientPool = new Stack<AssetStoreWebClient>(5);
        for (int i = 0; i < 5; i++)
        {
            AssetStoreWebClient assetStoreWebClient = new AssetStoreWebClient()
            {
                Encoding = Encoding.UTF8
            };
            AssetStoreClient.sClientPool.Push(assetStoreWebClient);
        }
    }

    public static void LoadFromUrl(string url, AssetStoreClient.DoneCallback callback, AssetStoreClient.ProgressCallback progress)
    {
        AssetStoreClient.CreatePendingGetBinary(url, url, callback).progressCallback = progress;
    }

    public static bool LoggedIn()
    {
        return AssetStoreClient.sLoginState == AssetStoreClient.LoginState.LOGGED_IN;
    }

    public static bool LoggedOut()
    {
        return AssetStoreClient.sLoginState == AssetStoreClient.LoginState.LOGGED_OUT;
    }

    public static bool LoginError()
    {
        return AssetStoreClient.sLoginState == AssetStoreClient.LoginState.LOGIN_ERROR;
    }

    public static bool LoginInProgress()
    {
        return AssetStoreClient.sLoginState == AssetStoreClient.LoginState.IN_PROGRESS;
    }

    internal static void LoginWithCredentials(string username, string password, bool rememberMe, AssetStoreClient.DoneLoginCallback callback)
    {
        if (AssetStoreClient.sLoginState == AssetStoreClient.LoginState.IN_PROGRESS)
        {
            DebugUtils.LogWarning("Tried to login with credentials while already in progress of logging in");
            return;
        }
        AssetStoreClient.sLoginState = AssetStoreClient.LoginState.IN_PROGRESS;
        AssetStoreClient.RememberSession = rememberMe;
        AssetStoreClient.sLoginErrorMessage = null;
        Uri uri = new Uri(string.Format("{0}/login", AssetStoreClient.AssetStoreUrl));
        AssetStoreWebClient assetStoreWebClient = new AssetStoreWebClient();
        NameValueCollection nameValueCollection = new NameValueCollection()
        {
            { "user", username },
            { "pass", password },
            { "unityversion", Application.unityVersion },
            { "toolversion", "V5.0.0" },
            { "license_hash", AssetStoreClient.GetLicenseHash() },
            { "hardware_hash", AssetStoreClient.GetHardwareHash() }
        };
        AssetStoreClient.Pending pending = new AssetStoreClient.Pending()
        {
            conn = assetStoreWebClient,
            id = "login",
            callback = AssetStoreClient.WrapLoginCallback(callback)
        };
        AssetStoreClient.pending.Add(pending);
        assetStoreWebClient.Headers.Add("Accept", "application/json");
        assetStoreWebClient.UploadValuesCompleted += new UploadValuesCompletedEventHandler(AssetStoreClient.UploadValuesCallback);
        try
        {
            assetStoreWebClient.UploadValuesAsync(uri, "POST", nameValueCollection, pending);
        }
        catch (WebException webException)
        {
            pending.ex = webException;
            AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGIN_ERROR;
        }
    }

    internal static void LoginWithRememberedSession(AssetStoreClient.DoneLoginCallback callback)
    {
        if (AssetStoreClient.sLoginState == AssetStoreClient.LoginState.IN_PROGRESS)
        {
            DebugUtils.LogWarning("Tried to login with remembered session while already in progress of logging in");
            return;
        }
        AssetStoreClient.sLoginState = AssetStoreClient.LoginState.IN_PROGRESS;
        AssetStoreClient.sLoginErrorMessage = null;
        if (!AssetStoreClient.RememberSession)
        {
            AssetStoreClient.SavedSessionID = string.Empty;
        }
        Uri uri = new Uri(string.Format("{0}/login?reuse_session={1}&unityversion={2}&toolversion={3}&xunitysession={4}", new object[] { AssetStoreClient.AssetStoreUrl, AssetStoreClient.SavedSessionID, Uri.EscapeDataString(Application.unityVersion), Uri.EscapeDataString("V5.0.0"), "26c4202eb475d02864b40827dfff11a14657aa41" }));
        AssetStoreWebClient assetStoreWebClient = new AssetStoreWebClient();
        AssetStoreClient.Pending pending = new AssetStoreClient.Pending()
        {
            conn = assetStoreWebClient,
            id = "login",
            callback = AssetStoreClient.WrapLoginCallback(callback)
        };
        AssetStoreClient.pending.Add(pending);
        assetStoreWebClient.Headers.Add("Accept", "application/json");
        assetStoreWebClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(AssetStoreClient.DownloadStringCallback);
        try
        {
            assetStoreWebClient.DownloadStringAsync(uri, pending);
        }
        catch (WebException webException)
        {
            pending.ex = webException;
            AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGIN_ERROR;
        }
    }

    public static void Logout()
    {
        AssetStoreClient.UserIconUrl = null;
        AssetStoreClient.ActiveSessionID = string.Empty;
        AssetStoreClient.SavedSessionID = string.Empty;
        AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGGED_OUT;
    }

    private static AssetStoreResponse parseAssetStoreResponse(string data, byte[] binData, Exception ex, WebHeaderCollection responseHeaders)
    {
        AssetStoreResponse headers = new AssetStoreResponse()
        {
            data = data,
            binData = binData,
            ok = true,
            HttpErrorMessage = null,
            HttpStatusCode = -1
        };
        if (ex == null)
        {
            headers.HttpStatusCode = 200;
            headers.HttpHeaders = responseHeaders;
        }
        else
        {
            WebException webException = null;
            try
            {
                webException = (WebException)ex;
            }
            catch (Exception exception)
            {
            }
            if (webException == null || webException.Response == null || webException.Response.Headers == null)
            {
                DebugUtils.LogError(string.Concat("Invalid server response ", ex.Message));
                DebugUtils.LogError(string.Concat("Stacktrace:", ex.StackTrace));
            }
            else
            {
                headers.HttpHeaders = webException.Response.Headers;
                headers.HttpStatusCode = (int)((HttpWebResponse)webException.Response).StatusCode;
                headers.HttpErrorMessage = webException.Message;
                if (headers.HttpStatusCode != 401 && AssetStoreManager.sDbg)
                {
                    WebHeaderCollection webHeaderCollection = webException.Response.Headers;
                    DebugUtils.LogError("\nDisplaying ex the response headers\n");
                    for (int i = 0; i < webHeaderCollection.Count; i++)
                    {
                        DebugUtils.LogError(string.Concat("\t", webHeaderCollection.GetKey(i), " = ", webHeaderCollection.Get(i)));
                    }
                    DebugUtils.Log(string.Concat("status code: ", headers.HttpStatusCode.ToString()));
                }
            }
        }
        if (headers.HttpStatusCode / 100 != 2)
        {
            headers.ok = false;
            if (AssetStoreManager.sDbg)
            {
                DebugUtils.LogError(string.Concat("Request statusCode: ", headers.HttpStatusCode.ToString()));
            }
            if (ex == null)
            {
                headers.HttpErrorMessage = string.Concat("Request status: ", headers.HttpStatusCode.ToString());
            }
            else
            {
                headers.HttpErrorMessage = ex.Message;
            }
        }
        if (ex != null)
        {
            headers.ok = false;
            if (AssetStoreManager.sDbg)
            {
                DebugUtils.LogError(string.Concat("Request exception: ", ex.GetType().ToString(), " - ", ex.Message));
            }
            headers.HttpErrorMessage = ex.Message;
        }
        return headers;
    }

    private static void ReleaseClient(AssetStoreWebClient client)
    {
        AssetStoreClient.InitClientPool();
        if (client != null)
        {
            client.Headers.Remove("X-HttpMethod");
            AssetStoreClient.sClientPool.Push(client);
        }
    }

    public static void Update()
    {
        List<AssetStoreClient.Pending> pendings = AssetStoreClient.pending;
        Monitor.Enter(pendings);
        try
        {
            AssetStoreClient.pending.RemoveAll((AssetStoreClient.Pending p) =>
            {
                if (p.conn == null)
                {
                    if (p.queueDelegate == null)
                    {
                        DebugUtils.LogWarning("Invalid pending state while communicating with asset store");
                        return true;
                    }
                    if (!p.queueDelegate() && p.conn == null)
                    {
                        return false;
                    }
                    p.queueDelegate = null;
                }
                if (p.conn.IsBusy || p.ex == null && p.data == null && p.binData == null)
                {
                    if (p.progressCallback != null && p.statsUpdated)
                    {
                        p.statsUpdated = false;
                        double num = ((double)((float)p.totalBytesToSend) <= 0 ? 0 : (double)((float)p.bytesSend) / (double)((float)p.totalBytesToSend) * 100);
                        double num1 = ((double)((float)p.totalBytesToReceive) <= 0 ? 0 : (double)((float)p.bytesReceived) / (double)((float)p.totalBytesToReceive) * 100);
                        try
                        {
                            p.progressCallback(num, num1);
                        }
                        catch (Exception exception)
                        {
                            DebugUtils.LogError(string.Concat("Uncaught exception in async net progress callback: ", exception.Message));
                        }
                    }
                    return false;
                }
                try
                {
                    AssetStoreResponse assetStoreResponse = AssetStoreClient.parseAssetStoreResponse(p.data, p.binData, p.ex, (p.conn != null ? p.conn.ResponseHeaders : null));
                    if (AssetStoreManager.sDbg)
                    {
                        string[] str = new string[] { "Pending done: ", null, null, null, null, null };
                        str[1] = Thread.CurrentThread.ManagedThreadId.ToString();
                        str[2] = " ";
                        str[3] = p.id;
                        str[4] = " ";
                        str[5] = assetStoreResponse.data ?? "<nodata>";
                        DebugUtils.Log(string.Concat(str));
                        if (assetStoreResponse.HttpHeaders != null && assetStoreResponse.HttpHeaders.Get("X-Unity-Reason") != null)
                        {
                            DebugUtils.LogWarning(string.Concat("X-Unity-Reason: ", assetStoreResponse.HttpHeaders.Get("X-Unity-Reason")));
                        }
                    }
                    p.callback(assetStoreResponse);
                }
                catch (Exception exception2)
                {
                    Exception exception1 = exception2;
                    DebugUtils.LogError(string.Concat("Uncaught exception in async net callback: ", exception1.Message));
                    DebugUtils.LogError(exception1.StackTrace);
                }
                AssetStoreClient.ReleaseClient(p.conn);
                p.conn = null;
                return true;
            });
        }
        finally
        {
            Monitor.Exit(pendings);
        }
        AssetStoreClient.UpdateLargeFilesUpload();
    }

    private static string UpdateLargeFilesUpload()
    {
        string end;
        string str;
        WebHeaderCollection headers;
        if (AssetStoreClient.s_UploadingLargeFile == null)
        {
            if (AssetStoreClient.s_PendingLargeFiles.Count == 0)
            {
                return null;
            }
            AssetStoreClient.s_UploadingLargeFile = AssetStoreClient.s_PendingLargeFiles[0];
            try
            {
                AssetStoreClient.s_UploadingLargeFile.Open();
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                DebugUtils.LogError(string.Concat("Unable to start uploading:", AssetStoreClient.s_UploadingLargeFile.FilePath, " Reason: ", exception.Message));
                AssetStoreClient.s_PendingLargeFiles.Remove(AssetStoreClient.s_UploadingLargeFile);
                AssetStoreClient.s_PendingLargeFiles = null;
                str = null;
                return str;
            }
        }
        AssetStoreClient.LargeFilePending sUploadingLargeFile = AssetStoreClient.s_UploadingLargeFile;
        StreamReader streamReader = null;
        WebResponse response = null;
        try
        {
            if (sUploadingLargeFile == null || sUploadingLargeFile.Request == null)
            {
                str = null;
            }
            else
            {
                byte[] buffer = sUploadingLargeFile.Buffer;
                int num = 0;
                int num1 = 0;
                while (num1 < 2)
                {
                    num = sUploadingLargeFile.RequestFileStream.Read(buffer, 0, (int)buffer.Length);
                    if (num != 0)
                    {
                        sUploadingLargeFile.RequestStream.Write(buffer, 0, num);
                        sUploadingLargeFile.BytesSent += (long)num;
                        num1++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (num == 0)
                {
                    AssetStoreClient.s_PendingLargeFiles.Remove(sUploadingLargeFile);
                    AssetStoreClient.s_UploadingLargeFile = null;
                    DebugUtils.Log(string.Concat("Finished Uploading: ", sUploadingLargeFile.Id));
                    response = sUploadingLargeFile.Request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    try
                    {
                        streamReader = new StreamReader(responseStream);
                        end = streamReader.ReadToEnd();
                        streamReader.Close();
                    }
                    catch (Exception exception3)
                    {
                        Exception exception2 = exception3;
                        DebugUtils.LogError("StreamReader sr");
                        throw exception2;
                    }
                    AssetStoreResponse assetStoreResponse = AssetStoreClient.parseAssetStoreResponse(end, null, null, response.Headers);
                    sUploadingLargeFile.Close();
                    sUploadingLargeFile.RequestDoneCallback(assetStoreResponse);
                    str = end;
                }
                else
                {
                    try
                    {
                        double bytesSent = (double)sUploadingLargeFile.BytesSent;
                        double bytesToSend = bytesSent / (double)sUploadingLargeFile.BytesToSend * 100;
                        if (sUploadingLargeFile.RequestProgressCallback != null)
                        {
                            sUploadingLargeFile.RequestProgressCallback(bytesToSend, 0);
                        }
                    }
                    catch (Exception exception4)
                    {
                        DebugUtils.LogWarning(string.Concat("Progress update error ", exception4.Message));
                    }
                    str = null;
                }
            }
        }
        catch (Exception exception7)
        {
            Exception exception5 = exception7;
            DebugUtils.LogError(string.Concat("UploadingLarge Files Exception:", exception5.Source));
            if (streamReader != null)
            {
                streamReader.Close();
            }
            Exception exception6 = exception5;
            if (response == null)
            {
                headers = null;
            }
            else
            {
                headers = response.Headers;
            }
            AssetStoreResponse assetStoreResponse1 = AssetStoreClient.parseAssetStoreResponse(null, null, exception6, headers);
            sUploadingLargeFile.RequestDoneCallback(assetStoreResponse1);
            sUploadingLargeFile.Close();
            AssetStoreClient.s_PendingLargeFiles.Remove(sUploadingLargeFile);
            return null;
        }
        return str;
    }

    private static void UploadFileCallback(object sender, UploadFileCompletedEventArgs e)
    {
        AssetStoreClient.Pending userState = (AssetStoreClient.Pending)e.UserState;
        if (e.Error != null)
        {
            userState.ex = e.Error;
            return;
        }
        if (e.Cancelled)
        {
            userState.data = string.Empty;
        }
        else
        {
            userState.bytesReceived = userState.totalBytesToReceive;
            userState.statsUpdated = false;
            userState.data = Encoding.UTF8.GetString(e.Result);
        }
    }

    public static void UploadLargeFile(string path, string filepath, Dictionary<string, string> extraParams, AssetStoreClient.DoneCallback callback, AssetStoreClient.ProgressCallback progressCallback)
    {
        AssetStoreClient.LargeFilePending largeFilePending = new AssetStoreClient.LargeFilePending(path, filepath, extraParams, callback, progressCallback);
        AssetStoreClient.s_PendingLargeFiles.Add(largeFilePending);
    }

    private static void UploadProgressCallback(object sender, UploadProgressChangedEventArgs e)
    {
        AssetStoreClient.Pending userState = (AssetStoreClient.Pending)e.UserState;
        userState.bytesSend = (uint)e.BytesSent;
        userState.totalBytesToSend = (uint)e.TotalBytesToSend;
        userState.bytesReceived = (uint)e.BytesReceived;
        userState.totalBytesToReceive = (uint)e.TotalBytesToReceive;
        userState.statsUpdated = true;
        Console.WriteLine("{0} uploaded {1} of {2} bytes. {3} % complete...", new object[] { userState.id, e.BytesSent, e.TotalBytesToSend, e.ProgressPercentage });
    }

    private static void UploadStringCallback(object sender, UploadStringCompletedEventArgs e)
    {
        AssetStoreClient.Pending userState = (AssetStoreClient.Pending)e.UserState;
        if (e.Error != null)
        {
            userState.ex = e.Error;
            return;
        }
        userState.data = string.Empty;
        if (e.Cancelled)
        {
            userState.data = string.Empty;
        }
        else
        {
            userState.bytesReceived = userState.totalBytesToReceive;
            userState.statsUpdated = false;
            userState.data = e.Result;
        }
    }

    private static void UploadValuesCallback(object sender, UploadValuesCompletedEventArgs e)
    {
        AssetStoreClient.Pending userState = (AssetStoreClient.Pending)e.UserState;
        if (e.Error != null)
        {
            userState.ex = e.Error;
            return;
        }
        if (e.Cancelled)
        {
            userState.data = string.Empty;
        }
        else
        {
            userState.bytesReceived = userState.totalBytesToReceive;
            userState.statsUpdated = false;
            userState.data = Encoding.UTF8.GetString(e.Result);
        }
    }

    private static AssetStoreClient.DoneCallback WrapLoginCallback(AssetStoreClient.DoneLoginCallback callback)
    {
        return (AssetStoreResponse resp) =>
        {
            AssetStoreClient.UserIconUrl = null;
            int num = -1;
            if (resp.HttpHeaders != null)
            {
                num = Array.IndexOf<string>(resp.HttpHeaders.AllKeys, "X-Unity-Reason");
            }
            if (!resp.ok)
            {
                AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGIN_ERROR;
                AssetStoreClient.sLoginErrorMessage = (num < 0 ? resp.HttpErrorMessage ?? "Failed communication" : resp.HttpHeaders.Get(num));
                AssetStoreClient.sLoginErrorMessage = (resp.HttpStatusCode != 401 ? AssetStoreClient.sLoginErrorMessage : "The email and/or password you entered are incorrect. Please try again.");
                AssetStoreClient.sLoginErrorMessage = (resp.HttpStatusCode != 500 ? AssetStoreClient.sLoginErrorMessage : "Server error. Possibly due to excess incorrect login attempts. Try again later.");
                DebugUtils.LogError(resp.HttpErrorMessage ?? "Unknown http error");
            }
            else if (!resp.data.StartsWith("<!DOCTYPE"))
            {
                AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGGED_IN;
                JSONValue jSONValue = JSONParser.SimpleParse(resp.data);
                AssetStoreClient.ActiveSessionID = jSONValue["xunitysession"].AsString(false);
                AssetStoreClient.UserIconUrl = jSONValue.Get("keyimage.icon").AsString(false);
                if (AssetStoreClient.RememberSession)
                {
                    AssetStoreClient.SavedSessionID = AssetStoreClient.ActiveSessionID;
                }
            }
            else
            {
                AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGIN_ERROR;
                AssetStoreClient.sLoginErrorMessage = (num < 0 ? "Failed to login" : resp.HttpHeaders.Get(num));
                DebugUtils.LogError(resp.data ?? "no data");
            }
            callback(AssetStoreClient.sLoginErrorMessage);
        };
    }

    internal delegate void DoneCallback(AssetStoreResponse job);

    public delegate void DoneLoginCallback(string errorMessage);

    private class LargeFilePending
    {
        public string Id;

        public string FilePath;

        public string URI;

        public FileStream RequestFileStream;

        public HttpWebRequest Request;

        public Stream RequestStream;

        public long BytesToSend;

        public long BytesSent;

        public AssetStoreClient.DoneCallback RequestDoneCallback;

        public AssetStoreClient.ProgressCallback RequestProgressCallback;

        public byte[] Buffer;

        private Dictionary<string, string> m_extraParams;

        public LargeFilePending(string url, string filepath, Dictionary<string, string> extraParams, AssetStoreClient.DoneCallback doneCallback, AssetStoreClient.ProgressCallback progressCallback)
        {
            this.Id = filepath;
            this.URI = url;
            this.FilePath = filepath;
            this.RequestDoneCallback = doneCallback;
            this.RequestProgressCallback = progressCallback;
            this.m_extraParams = extraParams;
        }

        public void Close()
        {
            if (this.RequestFileStream != null)
            {
                this.RequestFileStream.Close();
                this.RequestFileStream = null;
            }
            if (this.RequestStream != null)
            {
                this.RequestStream.Close();
                this.RequestStream = null;
            }
            this.Request = null;
            this.Buffer = null;
        }

        public void Open()
        {
            try
            {
                this.RequestFileStream = new FileStream(this.FilePath, FileMode.Open, FileAccess.Read);
                this.Request = (HttpWebRequest)WebRequest.Create(AssetStoreClient.APIUri(this.URI, this.m_extraParams));
                this.Request.AllowWriteStreamBuffering = false;
                this.Request.Timeout = 36000000;
                this.Request.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                this.Request.KeepAlive = false;
                this.Request.ContentLength = this.RequestFileStream.Length;
                this.Request.Method = "PUT";
                this.BytesToSend = this.RequestFileStream.Length;
                this.BytesSent = (long)0;
                this.RequestStream = this.Request.GetRequestStream();
                if (this.Buffer == null)
                {
                    this.Buffer = new byte[32768];
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                AssetStoreResponse assetStoreResponse = AssetStoreClient.parseAssetStoreResponse(null, null, exception, null);
                this.RequestDoneCallback(assetStoreResponse);
                this.Close();
                throw exception;
            }
        }
    }

    private enum LoginState
    {
        LOGGED_OUT,
        IN_PROGRESS,
        LOGGED_IN,
        LOGIN_ERROR
    }

    public class Pending
    {
        internal AssetStoreClient.PendingQueueDelegate queueDelegate;

        public AssetStoreWebClient conn;

        public Exception ex;

        public string data;

        public byte[] binData;

        public volatile uint bytesReceived;

        public volatile uint totalBytesToReceive;

        public volatile uint bytesSend;

        public volatile uint totalBytesToSend;

        public volatile bool statsUpdated;

        public string id;

        public AssetStoreClient.DoneCallback callback;

        public AssetStoreClient.ProgressCallback progressCallback;

        public Pending()
        {
        }
    }

    internal delegate bool PendingQueueDelegate();

    public delegate void ProgressCallback(double pctUp, double pctDown);
}