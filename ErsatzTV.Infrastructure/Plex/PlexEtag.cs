using System.Security.Cryptography;
using ErsatzTV.Infrastructure.Plex.Models;

namespace ErsatzTV.Infrastructure.Plex;

public static class PlexEtag
{
    public static string ForMovie(PlexMetadataResponse response)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // video key
        bw.Write(response.Key);

        // video added at
        bw.Write(response.AddedAt);

        // video updated at
        bw.Write(response.UpdatedAt);

        foreach (PlexMediaResponse<PlexPartResponse> media in response.Media)
        {
            // media id
            bw.Write((byte)FieldKey.MediaId);
            bw.Write(media.Id);

            // media part id
            foreach (PlexPartResponse part in media.Part)
            {
                bw.Write((byte)FieldKey.PartId);
                bw.Write(part.Id);
            }
        }

        // genre ids
        foreach (PlexGenreResponse genre in Optional(response.Genre).Flatten())
        {
            bw.Write((byte)FieldKey.GenreTag);
            bw.Write(genre.Tag);
        }

        // label ids
        foreach (PlexLabelResponse label in Optional(response.Label).Flatten())
        {
            bw.Write((byte)FieldKey.LabelTag);
            bw.Write(label.Tag);
        }

        // director ids
        foreach (PlexDirectorResponse director in Optional(response.Director).Flatten())
        {
            bw.Write((byte)FieldKey.DirectorTag);
            bw.Write(director.Tag);
        }

        // writer ids
        foreach (PlexWriterResponse writer in Optional(response.Writer).Flatten())
        {
            bw.Write((byte)FieldKey.WriterTag);
            bw.Write(writer.Tag);
        }

        // collection ids
        foreach (PlexCollectionResponse collection in Optional(response.Collection).Flatten())
        {
            bw.Write((byte)FieldKey.CollectionTag);
            bw.Write(collection.Tag);
        }

        // role ids
        foreach (PlexRoleResponse role in Optional(response.Role).Flatten())
        {
            bw.Write((byte)FieldKey.RoleTag);
            bw.Write(role.Tag);
        }

        ms.Position = 0;
        byte[] hash = SHA1.Create().ComputeHash(ms);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    public static string ForShow(PlexMetadataResponse response)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // video key
        bw.Write(response.Key);

        // video added at
        bw.Write(response.AddedAt);

        // video updated at
        bw.Write(response.UpdatedAt);

        // genre ids
        foreach (PlexGenreResponse genre in Optional(response.Genre).Flatten())
        {
            bw.Write((byte)FieldKey.GenreTag);
            bw.Write(genre.Tag);
        }

        // collection ids
        foreach (PlexCollectionResponse collection in Optional(response.Collection).Flatten())
        {
            bw.Write((byte)FieldKey.CollectionTag);
            bw.Write(collection.Tag);
        }

        // role ids
        foreach (PlexRoleResponse role in Optional(response.Role).Flatten())
        {
            bw.Write((byte)FieldKey.RoleTag);
            bw.Write(role.Tag);
        }

        // label ids
        foreach (PlexLabelResponse label in Optional(response.Label).Flatten())
        {
            bw.Write((byte)FieldKey.LabelTag);
            bw.Write(label.Tag);
        }

        ms.Position = 0;
        byte[] hash = SHA1.Create().ComputeHash(ms);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    public static string ForSeason(PlexXmlMetadataResponse response)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // video key
        bw.Write(response.Key);

        // video added at
        bw.Write(response.AddedAt);

        // video updated at
        bw.Write(response.UpdatedAt);

        // collection ids
        foreach (PlexCollectionResponse collection in Optional(response.Collection).Flatten())
        {
            bw.Write((byte)FieldKey.CollectionTag);
            bw.Write(collection.Tag);
        }

        // thumb
        if (!string.IsNullOrWhiteSpace(response.Thumb))
        {
            bw.Write(response.Thumb);
        }

        // art
        if (!string.IsNullOrWhiteSpace(response.Art))
        {
            bw.Write(response.Art);
        }

        ms.Position = 0;
        byte[] hash = SHA1.Create().ComputeHash(ms);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    public static string ForEpisode(PlexXmlMetadataResponse response)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // video key
        bw.Write(response.Key);

        // video added at
        bw.Write(response.AddedAt);

        // video updated at
        bw.Write(response.UpdatedAt);

        foreach (PlexMediaResponse<PlexXmlPartResponse> media in response.Media)
        {
            // media id
            bw.Write((byte)FieldKey.MediaId);
            bw.Write(media.Id);

            // media part id
            foreach (PlexXmlPartResponse part in media.Part)
            {
                bw.Write((byte)FieldKey.PartId);
                bw.Write(part.Id);

                // media part id
                foreach (PlexStreamResponse stream in part.Stream)
                {
                    bw.Write((byte)FieldKey.StreamId);
                    bw.Write(stream.Id);
                }
            }
        }

        // genre ids
        foreach (PlexGenreResponse genre in Optional(response.Genre).Flatten())
        {
            bw.Write((byte)FieldKey.GenreTag);
            bw.Write(genre.Tag);
        }

        // director ids
        foreach (PlexDirectorResponse director in Optional(response.Director).Flatten())
        {
            bw.Write((byte)FieldKey.DirectorTag);
            bw.Write(director.Tag);
        }

        // writer ids
        foreach (PlexWriterResponse writer in Optional(response.Writer).Flatten())
        {
            bw.Write((byte)FieldKey.WriterTag);
            bw.Write(writer.Tag);
        }

        // collection ids
        foreach (PlexCollectionResponse collection in Optional(response.Collection).Flatten())
        {
            bw.Write((byte)FieldKey.CollectionTag);
            bw.Write(collection.Tag);
        }

        // role ids
        foreach (PlexRoleResponse role in Optional(response.Role).Flatten())
        {
            bw.Write((byte)FieldKey.RoleTag);
            bw.Write(role.Tag);
        }

        ms.Position = 0;
        byte[] hash = SHA1.Create().ComputeHash(ms);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    private enum FieldKey : byte
    {
        MediaId = 0,
        PartId = 1,
        StreamId = 2,

        GenreTag = 10,
        DirectorTag = 11,
        WriterTag = 12,
        CollectionTag = 13,
        RoleTag = 14,
        LabelTag = 15,

        Thumb = 20,
        Art = 21
    }
}
