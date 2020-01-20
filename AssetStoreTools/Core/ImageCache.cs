using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

internal class ImageCache
{
    private const float k_FadeTime = 0.7f;

    private const int kImageCacheSize = 103;

    private double m_DownloadedAt;

    private static Dictionary<string, ImageCache> s_EntriesByUrl;

    public static Dictionary<string, ImageCache> EntriesByUrl
    {
        get
        {
            if (ImageCache.s_EntriesByUrl == null)
            {
                ImageCache.s_EntriesByUrl = new Dictionary<string, ImageCache>();
            }

            return ImageCache.s_EntriesByUrl;
        }
    }

    public float FadeAlpha
    {
        get { return (this.m_DownloadedAt >= 0 ? Mathf.Min(1f, (float) (EditorApplication.timeSinceStartup - this.m_DownloadedAt) / 0.7f) : 0f); }
    }

    public bool Failed { get; private set; }

    public double LastUsed { get; set; }

    public static int MaxCacheSize
    {
        get { return 103; }
    }

    public float Progress { get; set; }

    public Texture2D Texture { get; set; }

    static ImageCache()
    {
    }

    public ImageCache()
    {
    }

    public static ImageCache DownloadImage(Image img, ImageCache.DownloadCallback callback)
    {
        ImageCache imageCache;
        if (img == null || img.mUrl == null)
        {
            return null;
        }

        if (!ImageCache.EntriesByUrl.TryGetValue(img.mUrl, out imageCache))
        {
            ImageCache.EnsureCacheSpace();
            imageCache = new ImageCache()
            {
                Failed = false,
                Texture = null,
                m_DownloadedAt = -1,
                LastUsed = EditorApplication.timeSinceStartup
            };
            ImageCache.EntriesByUrl[img.mUrl] = imageCache;
        }
        else if (imageCache.Texture != null || imageCache.Progress >= 0f || imageCache.Failed)
        {
            img.mImgCache = imageCache;
            return imageCache;
        }

        img.mImgCache = imageCache;
        imageCache.Progress = 0f;
        AssetStoreClient.ProgressCallback progress = (double pctUp, double pctDown) => imageCache.Progress = (float) pctDown;
        AssetStoreClient.DoneCallback doneCallback = (AssetStoreResponse resp) =>
        {
            imageCache.Progress = -1f;
            imageCache.LastUsed = EditorApplication.timeSinceStartup;
            imageCache.m_DownloadedAt = imageCache.LastUsed;
            imageCache.Failed = resp.failed;
            if (resp.ok && resp.binData != null && (int) resp.binData.Length > 0)
            {
                Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2D.LoadImage(resp.binData);
                imageCache.Texture = texture2D;
            }

            if (callback != null)
            {
                string str = string.Format("Error fetching {0}", img.Name);
                if (resp.failed)
                {
                    DebugUtils.LogWarning(string.Concat(str, string.Format(" : ({0}) {1} from {2}", resp.HttpStatusCode, resp.HttpErrorMessage ?? "n/a", img.mUrl)));
                }

                callback(img, imageCache, (!resp.ok ? str : null));
            }
        };
        AssetStoreClient.LoadFromUrl(img.mUrl, doneCallback, progress);
        return imageCache;
    }

    private static void EnsureCacheSpace()
    {
        while (ImageCache.EntriesByUrl.Count > 103)
        {
            string key = null;
            double lastUsed = EditorApplication.timeSinceStartup;
            foreach (KeyValuePair<string, ImageCache> entriesByUrl in ImageCache.EntriesByUrl)
            {
                if (entriesByUrl.Value.LastUsed >= lastUsed)
                {
                    continue;
                }

                key = entriesByUrl.Key;
                lastUsed = entriesByUrl.Value.LastUsed;
            }

            ImageCache.EntriesByUrl.Remove(key);
        }
    }

    public static ImageCache PushImage(string url, Texture2D tex)
    {
        ImageCache.EnsureCacheSpace();
        ImageCache imageCache = new ImageCache()
        {
            Failed = false,
            Texture = tex,
            LastUsed = EditorApplication.timeSinceStartup,
            Progress = -1f
        };
        ImageCache.EntriesByUrl[url] = imageCache;
        return imageCache;
    }

    public static ImageCache PushImage(Image img, Texture2D tex)
    {
        img.mImgCache = ImageCache.PushImage(img.mUrl, tex);
        return img.mImgCache;
    }

    public delegate void DownloadCallback(Image img, ImageCache imgcache, string errorMessage);
}