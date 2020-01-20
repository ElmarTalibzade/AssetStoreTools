using System;
using System.Net;

internal class AssetStoreWebClient : WebClient
{
    public AssetStoreWebClient()
    {
    }

    protected override WebRequest GetWebRequest(Uri address)
    {
        return (HttpWebRequest)base.GetWebRequest(address);
    }
}