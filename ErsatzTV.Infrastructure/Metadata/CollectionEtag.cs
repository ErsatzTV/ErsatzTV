using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.IO;

namespace ErsatzTV.Infrastructure.Metadata;

[SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms")]
public class CollectionEtag : ICollectionEtag
{
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public CollectionEtag(RecyclableMemoryStreamManager recyclableMemoryStreamManager) =>
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;

    public string ForCollectionItems(List<MediaItem> items)
    {
        using MemoryStream ms = _recyclableMemoryStreamManager.GetStream();
        using var bw = new BinaryWriter(ms);

        foreach (MediaItem item in items.OrderBy(i => i.Id))
        {
            bw.Write(item.Id);
        }

        ms.Position = 0;
        byte[] hash = SHA1.Create().ComputeHash(ms);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}
