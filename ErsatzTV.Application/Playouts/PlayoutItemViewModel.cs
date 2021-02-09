using System;

namespace ErsatzTV.Application.Playouts
{
    public record PlayoutItemViewModel(string Title, DateTimeOffset Start, string Duration);
}
