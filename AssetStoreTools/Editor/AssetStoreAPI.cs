using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal static class AssetStoreAPI
{
    private const string kBundlePath = "/package/{0}/assetbundle/{1}";

    private const string kUnityPackagePath = "/package/{0}/unitypackage";

    public static void GetMetaData(AssetStorePublisher account, PackageDataSource packageDataSource, AssetStoreAPI.DoneCallback callback)
    {
        AssetStoreClient.CreatePendingGet("metadata", "/metadata/0", (AssetStoreResponse res) =>
        {
            string str;
            JSONValue jSONValue;
            DebugUtils.Log(res.data);
            if (AssetStoreAPI.Parse(res, out str, out jSONValue) && !jSONValue.ContainsKey("error_fields"))
            {
                callback(str);
                return;
            }

            string str1 = "none";
            try
            {
                str1 = "account";
                string str2 = AssetStoreAPI.OnAssetStorePublisher(jSONValue, account, packageDataSource);
                if (str2 != null)
                {
                    callback(str2);
                    return;
                }
            }
            catch (JSONTypeException jSONTypeException)
            {
                callback(string.Concat("Malformed metadata response from server: ", str1, " - ", jSONTypeException.Message));
            }
            catch (KeyNotFoundException keyNotFoundException)
            {
                callback(string.Concat("Malformed metadata response from server. ", str1, " - ", keyNotFoundException.Message));
            }

            callback(null);
        });
    }

    public static string GetUploadBundlePath(Package package, string relativeAssetPath)
    {
        return string.Format("/package/{0}/assetbundle/{1}", package.versionId, Uri.EscapeDataString(relativeAssetPath));
    }

    private static string OnAssetStorePublisher(JSONValue jval, AssetStorePublisher account, PackageDataSource packageDataSource)
    {
        string str;
        string str1 = "unknown field";
        try
        {
            str1 = "publisher";
            Dictionary<string, JSONValue> strs = jval["publisher"].AsDict(false);
            account.mStatus = AssetStorePublisher.Status.New;
            if (strs.ContainsKey("name"))
            {
                account.mStatus = AssetStorePublisher.Status.Existing;
                str1 = "publisher -> id";
                JSONValue item = strs["id"];
                account.publisherId = int.Parse(item.AsString(false));
                str1 = "publisher -> name";
                account.publisherName = strs["name"].AsString(false);
            }

            str1 = "publisher";
            if (AssetStoreManager.sDbg)
            {
                JSONValue jSONValue = jval["publisher"];
                DebugUtils.Log(string.Concat("publisher ", jSONValue.ToString(string.Empty, "    ")));
                JSONValue item1 = jval["packages"];
                DebugUtils.Log(string.Concat("packs ", item1.ToString(string.Empty, "    ")));
            }

            str1 = "packages";
            if (!jval.Get("packages").IsNull())
            {
                AssetStoreAPI.OnPackages(jval["packages"], packageDataSource);
            }

            return null;
        }
        catch (JSONTypeException jSONTypeException1)
        {
            JSONTypeException jSONTypeException = jSONTypeException1;
            str = string.Concat("Malformed response from server: ", str1, " - ", jSONTypeException.Message);
        }
        catch (KeyNotFoundException keyNotFoundException1)
        {
            KeyNotFoundException keyNotFoundException = keyNotFoundException1;
            str = string.Concat("Malformed response from server. ", str1, " - ", keyNotFoundException.Message);
        }

        return str;
    }

    private static string OnPackageReceived(JSONValue jval, Package package)
    {
        string str;
        string str1 = "unknown";
        try
        {
            if (jval.ContainsKey("id"))
            {
                string empty = string.Empty;
                string empty1 = string.Empty;
                string empty2 = string.Empty;
                string str2 = string.Empty;
                string empty3 = string.Empty;
                bool flag = false;
                string str3 = string.Empty;
                string empty4 = string.Empty;
                string str4 = string.Empty;
                str1 = "id";
                if (!jval[str1].IsNull())
                {
                    JSONValue item = jval[str1];
                    package.versionId = int.Parse(item.AsString(false));
                }

                str1 = "name";
                jval.Copy(str1, ref empty, false);
                str1 = "version_name";
                jval.Copy(str1, ref empty1, false);
                str1 = "root_guid";
                jval.Copy(str1, ref empty2, false);
                str1 = "root_path";
                jval.Copy(str1, ref str2, false);
                str1 = "project_path";
                jval.Copy(str1, ref empty3, false);
                str1 = "is_complete_project";
                jval.Copy(str1, ref flag);
                str1 = "preview_url";
                jval.Copy(str1, ref str3);
                str1 = "icon_url";
                jval.Copy(str1, ref empty4);
                str1 = "status";
                jval.Copy(str1, ref str4);
                package.Name = empty;
                package.VersionName = empty1;
                package.RootGUID = empty2;
                package.RootPath = str2;
                package.ProjectPath = empty3;
                package.IsCompleteProjects = flag;
                package.PreviewURL = str3;
                package.SetStatus(str4);
                if (!string.IsNullOrEmpty(empty4))
                {
                    package.SetIconURL(empty4);
                }

                return null;
            }
            else
            {
                str = null;
            }
        }
        catch (JSONTypeException jSONTypeException1)
        {
            JSONTypeException jSONTypeException = jSONTypeException1;
            str = string.Concat(new string[] {"Malformed metadata response for package '", package.Name, "' field '", str1, "': ", jSONTypeException.Message});
        }
        catch (KeyNotFoundException keyNotFoundException1)
        {
            KeyNotFoundException keyNotFoundException = keyNotFoundException1;
            str = string.Concat(new string[] {"Malformed metadata response for package. '", package.Name, "' field '", str1, "': ", keyNotFoundException.Message});
        }

        return str;
    }

    private static void OnPackages(JSONValue jv, PackageDataSource packageDataSource)
    {
        IList<Package> allPackages = packageDataSource.GetAllPackages();
        Dictionary<string, JSONValue> strs = jv.AsDict(false);
        string empty = string.Empty;
        allPackages.Clear();
        foreach (KeyValuePair<string, JSONValue> keyValuePair in strs)
        {
            int num = int.Parse(keyValuePair.Key);
            JSONValue value = keyValuePair.Value;
            Package package = packageDataSource.FindByID(num) ?? new Package(num);
            empty = string.Concat(empty, AssetStoreAPI.OnPackageReceived(value, package));
            empty = string.Concat(empty, AssetStoreAPI.RefreshMainAssets(value, package));
            allPackages.Add(package);
        }

        packageDataSource.OnDataReceived(empty);
    }

    private static bool Parse(AssetStoreResponse response, out string error, out JSONValue jval)
    {
        bool flag;
        jval = new JSONValue();
        error = null;
        if (response.failed)
        {
            error = string.Format("Error receiving response from server ({0}): {1}", response.HttpStatusCode, response.HttpErrorMessage ?? "n/a");
            return true;
        }

        try
        {
            jval = (new JSONParser(response.data)).Parse();
            if (jval.ContainsKey("error"))
            {
                JSONValue item = jval["error"];
                error = item.AsString(true);
            }
            else if (jval.ContainsKey("status") && jval["status"].AsString(true) != "ok")
            {
                JSONValue jSONValue = jval["message"];
                error = jSONValue.AsString(true);
            }

            return error != null;
        }
        catch (JSONParseException jSONParseException1)
        {
            JSONParseException jSONParseException = jSONParseException1;
            error = "Error parsing reply from AssetStore";
            DebugUtils.LogError(string.Concat("Error parsing server reply: ", response.data));
            DebugUtils.LogError(jSONParseException.Message);
            flag = true;
        }

        return flag;
    }

    private static string RefreshMainAssets(JSONValue jval, Package package)
    {
        string str;
        string str1 = "unknown";
        try
        {
            str1 = "assetbundles";
            JSONValue jSONValue = jval.Get(str1);
            if (!jSONValue.IsNull())
            {
                List<string> strs = new List<string>();
                foreach (JSONValue jSONValue1 in jSONValue.AsList(false))
                {
                    strs.Add(jSONValue1.AsString(false));
                }

                package.MainAssets = strs;
            }

            return null;
        }
        catch (JSONTypeException jSONTypeException1)
        {
            JSONTypeException jSONTypeException = jSONTypeException1;
            str = string.Concat(new string[] {"Malformed metadata response for mainAssets '", package.Name, "' field '", str1, "': ", jSONTypeException.Message});
        }
        catch (KeyNotFoundException keyNotFoundException1)
        {
            KeyNotFoundException keyNotFoundException = keyNotFoundException1;
            str = string.Concat(new string[] {"Malformed metadata response for package. '", package.Name, "' field '", str1, "': ", keyNotFoundException.Message});
        }

        return str;
    }

    private static void Upload(string path, string filepath, AssetStoreAPI.DoneCallback callback, AssetStoreClient.ProgressCallback progress = null)
    {
        AssetStoreClient.Pending pending = AssetStoreClient.CreatePendingUpload(path, path, filepath, (AssetStoreResponse resp) =>
        {
            JSONValue jSONValue;
            string str = null;
            AssetStoreAPI.Parse(resp, out str, out jSONValue);
            callback(str);
        });
        pending.progressCallback = progress;
    }

    public static void UploadAssets(Package package, string newGUID, string newRootPath, string newProjectpath, string filepath, AssetStoreAPI.DoneCallback callback, AssetStoreClient.ProgressCallback progress)
    {
        string str1 = string.Format("/package/{0}/unitypackage", package.versionId);
        Dictionary<string, string> strs = new Dictionary<string, string>()
        {
            {"root_guid", newGUID},
            {"root_path", newRootPath},
            {"project_path", newProjectpath}
        };
        AssetStoreClient.UploadLargeFile(str1, filepath, strs, (AssetStoreResponse resp) =>
        {
            JSONValue jSONValue;
            if (resp.HttpStatusCode == -2)
            {
                callback("aborted");
                return;
            }

            string str = null;
            AssetStoreAPI.Parse(resp, out str, out jSONValue);
            callback(str);
        }, progress);
    }

    public static void UploadBundle(string path, string filepath, AssetStoreAPI.UploadBundleCallback callback, AssetStoreClient.ProgressCallback progress)
    {
        AssetStoreClient.UploadLargeFile(path, filepath, null, (AssetStoreResponse resp) =>
        {
            JSONValue jSONValue;
            string str = null;
            AssetStoreAPI.Parse(resp, out str, out jSONValue);
            callback(filepath, str);
        }, progress);
    }

    public delegate void DoneCallback(string errorMessage);

    public delegate void UploadBundleCallback(string filepath, string errorMessage);
}