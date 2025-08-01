﻿using ErsatzTV.Application.Artworks;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.ViewModels;

public class WatermarkEditViewModel
{
    public WatermarkEditViewModel()
    {
    }

    public WatermarkEditViewModel(WatermarkViewModel vm)
    {
        Id = vm.Id;
        Name = vm.Name;
        Image = vm.Image;
        Mode = vm.Mode;
        ImageSource = vm.ImageSource;
        Location = vm.Location;
        Size = vm.Size;
        Width = vm.Width;
        HorizontalMargin = vm.HorizontalMargin;
        VerticalMargin = vm.VerticalMargin;
        FrequencyMinutes = vm.FrequencyMinutes;
        DurationSeconds = vm.DurationSeconds;
        Opacity = vm.Opacity;
        PlaceWithinSourceContent = vm.PlaceWithinSourceContent;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public ArtworkContentTypeModel Image { get; set; }
    public ChannelWatermarkMode Mode { get; set; }
    public ChannelWatermarkImageSource ImageSource { get; set; }
    public WatermarkLocation Location { get; set; }
    public WatermarkSize Size { get; set; }
    public double Width { get; set; }
    public double HorizontalMargin { get; set; }
    public double VerticalMargin { get; set; }
    public int FrequencyMinutes { get; set; }
    public int DurationSeconds { get; set; }
    public int Opacity { get; set; }
    public bool PlaceWithinSourceContent { get; set; }

    public CreateWatermark ToCreate() =>
        new(
            Name,
            Image,
            Mode,
            ImageSource,
            Location,
            Size,
            Width,
            HorizontalMargin,
            VerticalMargin,
            FrequencyMinutes,
            DurationSeconds,
            Opacity,
            PlaceWithinSourceContent);

    public UpdateWatermark ToUpdate() =>
        new(
            Id,
            Name,
            Image,
            Mode,
            ImageSource,
            Location,
            Size,
            Width,
            HorizontalMargin,
            VerticalMargin,
            FrequencyMinutes,
            DurationSeconds,
            Opacity,
            PlaceWithinSourceContent);
}
