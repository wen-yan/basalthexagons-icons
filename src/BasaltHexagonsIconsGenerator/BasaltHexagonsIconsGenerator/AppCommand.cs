using System.CommandLine;
using System.Drawing;
using System.Drawing.Imaging;
using BasaltHexagons.CommandLine;
using BasaltHexagons.CommandLine.Annotations;
using Svg;

namespace BasaltHexagonsIconsGenerator;

partial class AppCommandOptions
{
    [CliCommandSymbol] public DirectoryInfo OutputDirectory { get; init; }
}

[CliCommandBuilder(CliCommandBuilderAttribute.DefaultRootCommandName, null)]
partial class AppCliCommandBuilder : RootCliCommandBuilder<AppCommand, AppCommandOptions>
{
    public AppCliCommandBuilder(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.Description = "Generate BasalHexagons icons";
        this.OutputDirectoryOption = new Option<DirectoryInfo>(["--output-directory", "-o"], "Directory to generate BasalHexagons icons")
        {
            IsRequired = true,
        };
    }
}

class AppCommand : Command<AppCommandOptions>
{
    public AppCommand(CommandContext commandContext) : base(commandContext)
    {
    }

    public override ValueTask ExecuteAsync()
    {
        foreach (Theme theme in Constants.Themes)
        {
            GenerateImages(new RenderContext(theme), this.Options.OutputDirectory.ToString(), $"basalthexagons-{theme.Name}");
        }

        return ValueTask.CompletedTask;
    }

    private static void GenerateImages(RenderContext context, string directory, string baseFilename)
    {
        SvgDocument GenerateSvg()
        {
            SvgDocument doc = new()
            {
                Width = Constants.SvgWidth,
                Height = Constants.SvgHeight,
                ViewBox = new SvgViewBox(context.CanvasRect.Left, context.CanvasRect.Top, context.CanvasRect.Width, context.CanvasRect.Height),
            };

            SvgPointCollection basePolygonPoints = new();
            basePolygonPoints.AddRange(GetBaseHexagonPoints().SelectMany<PointF, SvgUnit>(x => [new SvgUnit(x.X), new SvgUnit(x.Y)]));

            SvgDefinitionList defs = new();
            defs.Children.Add(new SvgPolygon()
            {
                ID = "base",
                Points = basePolygonPoints
            });
            doc.Children.Add(defs);

            // border
            doc.Children.Add(new SvgRectangle()
            {
                X = context.CanvasRect.Left + context.BorderStrokeWidth / 2,
                Y = context.CanvasRect.Top + context.BorderStrokeWidth / 2,
                Width = context.CanvasRect.Width - context.BorderStrokeWidth,
                Height = context.CanvasRect.Height - context.BorderStrokeWidth,
                CornerRadiusX = context.CanvasRect.Width * Constants.BorderCornerRadiusRation,
                Stroke = new SvgColourServer(context.Theme.BorderColor),
                StrokeWidth = context.BorderStrokeWidth,
                Fill = new SvgColourServer(context.Theme.BorderFill),
            });

            // hexagons
            foreach (Hexagon hexagon in context.Hexagons)
            {
                doc.Children.Add(new SvgUse()
                {
                    ReferencedElement = new Uri("#base", UriKind.Relative),
                    X = hexagon.Center.X,
                    Y = hexagon.Center.Y,
                    Fill = new SvgColourServer(hexagon.Color)
                });
            }

            return doc;
        }

        void SavePng(SvgDocument svgDoc, int size)
        {
            svgDoc.Width = size;
            svgDoc.Height = size;
            using Bitmap bitmap = new(size, size);
            svgDoc.Draw(bitmap);

            // Save the Bitmap as a PNG file
            string pngPath = $"{directory}/{baseFilename}-{size}x{size}.png";
            bitmap.Save(pngPath, ImageFormat.Png);
            Console.WriteLine($"Saved png to {pngPath}");
        }

        SvgDocument svgDoc = GenerateSvg();
        string svgPath = $"{directory}/{baseFilename}.svg";
        svgDoc.Write(svgPath);
        Console.WriteLine($"Saved svg to {svgPath}");

        foreach (int pngSize in Constants.PngSizes)
            SavePng(svgDoc, pngSize);
    }

    private static IEnumerable<PointF> GetBaseHexagonPoints() => Enumerable.Range(0, 6)
        .Select(i =>
        {
            double angle = Math.PI / 3 * (i + 0.5);
            double x = Constants.HexagonSize * Math.Cos(angle);
            double y = Constants.HexagonSize * Math.Sin(angle);
            return new PointF((float)x, (float)y);
        });
}

static class Constants
{
    public const float HexagonSize = 10;
    public const float HexagonMargin = -0.8f;
    public const int SvgWidth = 128;
    public const int SvgHeight = 128;
    public const float CanvasMarginRation = 0.12f;
    public const float BorderStrokeWidthRatio = 0.015f;
    public const float BorderCornerRadiusRation = 0.05f;

    public static readonly HexagonInfo[] HexagonInfos =
    [
        new(new(1, 1), 0),
        new(new(3, 1), 1),
        new(new(5, 1), 2),
        new(new(2, 3), 3),
        new(new(4, 3), 4),
        new(new(1, 5), 5),
    ];

    private static readonly Color[] ColorfulHexagonColors =
    [
        Color.FromArgb(0x46, 0x69, 0x46),
        Color.FromArgb(0x69, 0x3c, 0x2d),
        Color.FromArgb(0x46, 0x46, 0x2d),
        Color.FromArgb(0x3c, 0x69, 0x69),
        Color.FromArgb(0x46, 0x46, 0x69),
        Color.FromArgb(0x55, 0x46, 0x46),
    ];

    private static readonly Color[] GrayHexagonColors =
    [
        Color.FromArgb(0x46, 0x46, 0x46),
        Color.FromArgb(0x3c, 0x3c, 0x3c),
        Color.FromArgb(0x46, 0x46, 0x46),
        Color.FromArgb(0x69, 0x69, 0x69),
        Color.FromArgb(0x69, 0x69, 0x69),
        Color.FromArgb(0x55, 0x55, 0x55),
    ];

    public static readonly Theme[] Themes =
    [
        new("dark", Color.FromArgb(0x96, 0x96, 0x96), Color.FromArgb(0x2c, 0x2c, 0x2c), ColorfulHexagonColors),
        new("light", Color.FromArgb(0x2c, 0x2c, 0x2c), Color.FromArgb(0xc2, 0xc2, 0xc2), ColorfulHexagonColors),
        new("gray", Color.FromArgb(0x2c, 0x2c, 0x2c), Color.FromArgb(0xc2, 0xc2, 0xc2), GrayHexagonColors),
    ];

    public static readonly int[] PngSizes = [32, 64, 96, 128, 256];
}

record Theme(string Name, Color BorderColor, Color BorderFill, Color[] HexagonColors);

record HexagonInfo(Point Center, int ColorIndex);

record Hexagon(PointF Center, Color Color)
{
    public float Left => (float)(this.Center.X - Constants.HexagonSize * Math.Cos(Math.PI / 6));
    public float Right => (float)(this.Center.X + Constants.HexagonSize * Math.Cos(Math.PI / 6));
    public float Top => this.Center.Y - Constants.HexagonSize;
    public float Bottom => this.Center.Y + Constants.HexagonSize;

    public RectangleF RangeBox => new(this.Left, this.Top, this.Right - this.Left, this.Bottom - this.Top);
}

class RenderContext
{
    public RenderContext(Theme theme)
    {
        float dx = Constants.HexagonSize + Constants.HexagonMargin;
        float dy = (float)(dx / 2.0 * Math.Tan(Math.PI / 3.0));

        this.Theme = theme;
        this.Hexagons = Constants.HexagonInfos
            .Select(h =>
            {
                float x = dx * h.Center.X;
                float y = dy * h.Center.Y;
                return new Hexagon(new(x, y), this.Theme.HexagonColors[h.ColorIndex]);
            })
            .ToArray();

        RectangleF hexagonRangeBox = this.Hexagons
            .Select(x => x.RangeBox)
            .Aggregate(RectangleF.Union);

        PointF canvasCenter = hexagonRangeBox.GetCenter();
        float halfCanvasSize = Math.Max(hexagonRangeBox.Width, hexagonRangeBox.Height) * (1.0f + Constants.CanvasMarginRation) / 2.0f;

        this.CanvasRect = new(
            canvasCenter.X - halfCanvasSize,
            canvasCenter.Y - halfCanvasSize,
            halfCanvasSize * 2,
            halfCanvasSize * 2);

        this.BorderStrokeWidth = this.CanvasRect.Width * Constants.BorderStrokeWidthRatio;
    }

    public Theme Theme { get; }
    public Hexagon[] Hexagons { get; }
    public RectangleF CanvasRect { get; }
    public float BorderStrokeWidth { get; }
}

static class GeometryExtensions
{
    public static PointF GetCenter(this RectangleF rect) => new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
}