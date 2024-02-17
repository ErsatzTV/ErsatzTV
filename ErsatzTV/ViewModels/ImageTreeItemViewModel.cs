using ErsatzTV.Application.Images;
using MudBlazor;
using S = System.Collections.Generic;

namespace ErsatzTV.ViewModels;

public class ImageTreeItemViewModel
{
    private readonly string _imageCount;
    
    public ImageTreeItemViewModel(ImageFolderViewModel imageFolder)
    {
        LibraryFolderId = imageFolder.LibraryFolderId;
        Text = imageFolder.Name;
        FullPath = imageFolder.FullPath;
        TreeItems = [];
        CanExpand = imageFolder.SubfolderCount > 0;

        _imageCount = imageFolder.ImageCount switch
        {
            > 1 => $"{imageFolder.ImageCount} images",
            1 => "1 image",
            _ => string.Empty
        };

        foreach (double durationSeconds in imageFolder.DurationSeconds)
        {
            ImageFolderDuration = durationSeconds;
        }

        UpdateDuration(ImageFolderDuration);

        Icon = Icons.Material.Filled.Folder;
    }

    public string Text { get; }
    
    public string EndText { get; private set; }

    public string FullPath { get; }
    
    public string Icon { get; }
    
    public int LibraryFolderId { get; }
    public double? ImageFolderDuration { get; private set; }
    
    public bool CanExpand { get; }
    
    public S.HashSet<ImageTreeItemViewModel> TreeItems { get; }

    public void UpdateDuration(double? imageFolderDuration)
    {
        ImageFolderDuration = imageFolderDuration;

        string duration = string.Empty;
        
        foreach (double durationSeconds in Optional(imageFolderDuration))
        {
            duration = durationSeconds switch
            {
                1 => "1 second",
                > 0 => $"{durationSeconds:0.##} seconds",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(_imageCount))
            {
                duration += " - ";
            }
        }

        EndText = $"{duration}{_imageCount}";
    }
}
