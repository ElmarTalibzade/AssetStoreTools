using System;

public static class AssetBundlerFactory
{
    public static IAssetBundler GetBundler()
    {
        return new AssetBundler4();
    }
}