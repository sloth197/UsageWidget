using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

var output = args.Length > 0 ? args[0] : "app.ico";
int[] sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];
var images = sizes.Select(RenderPercent).ToArray();

using var file = File.Create(output);
using var writer = new BinaryWriter(file);
writer.Write((ushort)0);
writer.Write((ushort)1);
writer.Write((ushort)images.Length);

var offset = 6 + images.Length * 16;
for (var i = 0; i < images.Length; i++)
{
    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
    writer.Write((byte)0);
    writer.Write((byte)0);
    writer.Write((ushort)1);
    writer.Write((ushort)32);
    writer.Write(images[i].Length);
    writer.Write(offset);
    offset += images[i].Length;
}
foreach (var image in images) writer.Write(image);

static byte[] RenderPercent(int size)
{
    using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bitmap);
    g.Clear(Color.Transparent);
    g.SmoothingMode = size <= 24 ? SmoothingMode.HighQuality : SmoothingMode.AntiAlias;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

    var stroke = Math.Max(1.8f, size * 0.105f);
    var radius = size * 0.105f;
    using var pen = new Pen(Color.White, stroke)
    {
        StartCap = LineCap.Round,
        EndCap = LineCap.Round,
        LineJoin = LineJoin.Round
    };

    g.DrawLine(pen, size * 0.70f, size * 0.17f, size * 0.30f, size * 0.83f);
    g.DrawEllipse(pen, size * 0.29f - radius, size * 0.29f - radius, radius * 2, radius * 2);
    g.DrawEllipse(pen, size * 0.71f - radius, size * 0.71f - radius, radius * 2, radius * 2);

    using var stream = new MemoryStream();
    bitmap.Save(stream, ImageFormat.Png);
    return stream.ToArray();
}
