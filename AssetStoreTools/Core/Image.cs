using System;
using System.Runtime.CompilerServices;
using UnityEngine;

internal class Image
{
    public string mUrl;

    public ImageCache mImgCache;

    public Texture2D Icon
    {
        get { return this.Texture; }
    }

    public string Name { get; set; }

    public Texture2D Texture
    {
        get
        {
            if (this.mImgCache == null)
            {
                return null;
            }

            if (this.mImgCache.Texture == null)
            {
                return GUIUtil.StatusWheel.image as Texture2D;
            }

            return this.mImgCache.Texture;
        }
    }

    public Image()
    {
    }
}