using System.Collections.Concurrent;
using System.Numerics;
using WoWAddonLab.Automation;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace WoWAddonLab.Diagnostics;

internal readonly record struct ViewportReadRectangle(
    int X,
    int OpenGlY,
    int Width,
    int Height);
