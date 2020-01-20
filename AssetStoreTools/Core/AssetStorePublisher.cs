using System;

internal class AssetStorePublisher
{
    public AssetStorePublisher.Status mStatus;

    public int publisherId;

    public string publisherName;

    public int PublisherID
    {
        get { return this.publisherId; }
    }

    public AssetStorePublisher()
    {
    }

    public void Reset()
    {
        this.publisherId = -1;
        this.publisherName = string.Empty;
    }

    public enum Status
    {
        NotLoaded,
        Loading,
        New,
        Existing,
        Saving
    }
}