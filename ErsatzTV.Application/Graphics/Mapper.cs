using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Graphics;

public static class Mapper
{
    public static GraphicsElementViewModel ProjectToViewModel(GraphicsElement graphicsElement)
    {
        string fileName = Path.GetFileName(graphicsElement.Path);
        fileName = graphicsElement.Kind switch
        {
            GraphicsElementKind.Text => $"text/{fileName}",
            GraphicsElementKind.Image => $"image/{fileName}",
            GraphicsElementKind.Subtitle => $"subtitle/{fileName}",
            GraphicsElementKind.Motion => $"motion/{fileName}",
            _ => graphicsElement.Path
        };

        string name = fileName;
        if (!string.IsNullOrWhiteSpace(graphicsElement.Name))
        {
            name = $"{graphicsElement.Name} ({fileName})";
        }

        return new GraphicsElementViewModel(graphicsElement.Id, name, fileName);
    }
}
