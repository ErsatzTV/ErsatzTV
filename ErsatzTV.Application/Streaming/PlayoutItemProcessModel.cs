using System;
using System.Diagnostics;

namespace ErsatzTV.Application.Streaming;

public record PlayoutItemProcessModel(Process Process, DateTimeOffset Until);