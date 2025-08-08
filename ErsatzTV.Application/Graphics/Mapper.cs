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
            _ => new GraphicsElementViewModel(graphicsElement.Id, graphicsElement.Path)
        };
    }
}