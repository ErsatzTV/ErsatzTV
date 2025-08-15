using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Graphics;

public static class Mapper
{
    public static GraphicsElementViewModel ProjectToViewModel(GraphicsElement graphicsElement)
    {
        var fileName = Path.GetFileName(graphicsElement.Path);
        return graphicsElement.Kind switch
        {
            GraphicsElementKind.Text => new GraphicsElementViewModel(graphicsElement.Id, $"text/{fileName}"),
            GraphicsElementKind.Image => new GraphicsElementViewModel(graphicsElement.Id, $"image/{fileName}"),
            GraphicsElementKind.Subtitle => new  GraphicsElementViewModel(graphicsElement.Id, $"subtitle/{fileName}"),
            _ => new GraphicsElementViewModel(graphicsElement.Id, graphicsElement.Path)
        };
    }
}