using System.Drawing.Imaging;
using System.Reflection;
using FontStyle = System.Drawing.FontStyle;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using Label = System.Windows.Forms.Label;

namespace ImageCP;

internal static unsafe class Program
{
    private const string ImageName = "image.jpg";

    private static readonly Form Form = new()
    {
        Text = "ImageCP",
        ShowIcon = false,
        StartPosition = FormStartPosition.CenterScreen,
        Size = new Size(800, 600),
        AutoScroll = true
    };

    private static double Random01 => Random.Shared.NextDouble();

    private static double NextGaussian(double mean = 0.0, double sigma = 8.0 / 100.0)
    {
        var u1 = 1.0 - Random01;
        var u2 = 1.0 - Random01;
        var val = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + sigma * val;
    }

    private static double NextMultiplicative(double mean = 1.0, double sigma = 8.0 / 100.0) =>
        mean + sigma * (Random01 * 2 - 1);

    private static double NextSaltPepper(double rnd, double rnd2, double input, double d = 0.1 + 8.0 / 100.0) =>
        rnd < d ? rnd2 < 0.5 ? 0 : 255 : input;


    private static byte Clamp(double value) => (byte)Math.Max(0, Math.Min(255, value));

    private static PerformResultedTransform<bool> AddGaussianNoise()
    {
        return () => ([
                @ref =>
                {
                    var (r, g, b) = @ref;
                    *r = Clamp(*r + NextGaussian() * 255);
                    *g = Clamp(*g + NextGaussian() * 255);
                    *b = Clamp(*b + NextGaussian() * 255);
                }
            ],
            true);
    }

    private static PerformResultedTransform<bool> AddMultiplicativeNoise()
    {
        return () => ([
                @ref =>
                {
                    var (r, g, b) = @ref;
                    *r = Clamp(*r * NextMultiplicative());
                    *g = Clamp(*g * NextMultiplicative());
                    *b = Clamp(*b * NextMultiplicative());
                }
            ],
            true);
    }

    private static PerformResultedTransform<bool> AddSaltPepperNoise()
    {
        return () => ([
                @ref =>
                {
                    var (r, g, b) = @ref;
                    var rnd = Random01;
                    var rnd2 = Random01;
                    *r = Clamp(NextSaltPepper(rnd, rnd2, *r));
                    *g = Clamp(NextSaltPepper(rnd, rnd2, *g));
                    *b = Clamp(NextSaltPepper(rnd, rnd2, *b));
                }
            ],
            true);
    }

    private static Bitmap MeanFilter(Bitmap image, int kernelSize)
    {
        const PixelFormat format = PixelFormat.Format32bppArgb;
        var rect = new Rectangle(Point.Empty, image.Size);
        var newImage = image.Clone(rect, format);
        var data = newImage.LockBits(rect, ImageLockMode.ReadWrite, format);
        var pointer = (byte*)data.Scan0;

        for (var y = 0; y < data.Height; y++)
        for (var x = 0; x < data.Width; x++)
        for (var channelIndex = 0; channelIndex < 3; channelIndex++)
            ProcessChannelFn(channelIndex, x, y);

        newImage.UnlockBits(data);
        return newImage;

        void ProcessChannelFn(int channelIndex, int x, int y)
        {
            var sum = 0;
            var count = 0;

            for (var ky = 0; ky < kernelSize; ky++)
            for (var kx = 0; kx < kernelSize; kx++)
            {
                var px = x + kx - kernelSize / 2;
                var py = y + ky - kernelSize / 2;

                if (px < 0 || py < 0 || px >= data.Width || py >= data.Height)
                    continue;

                var offset = py * data.Stride + px * 4;
                sum += pointer[offset + channelIndex];
                count++;
            }

            var offset2 = y * data.Stride + x * 4;
            if (count == 0)
                return;
            pointer[offset2 + channelIndex] = (byte)(sum / count);
        }
    }


    private static Bitmap MedianFilter(Bitmap image, int kernelSize)
    {
        const PixelFormat format = PixelFormat.Format32bppArgb;
        var rect = new Rectangle(Point.Empty, image.Size);
        var newImage = image.Clone(rect, format);
        var newData = newImage.LockBits(rect, ImageLockMode.ReadWrite, format);
        var oldData = image.LockBits(rect, ImageLockMode.ReadOnly, format);
        var newPointer = (byte*)newData.Scan0;
        var oldPointer = (byte*)oldData.Scan0;

        for (var y = 0; y < newData.Height; y++)
        for (var x = 0; x < newData.Width; x++)
        for (var channelIndex = 0; channelIndex < 3; channelIndex++)
            ProcessChannelFn(channelIndex, x, y);

        newImage.UnlockBits(newData);
        image.UnlockBits(oldData);
        return newImage;

        void ProcessChannelFn(int channelIndex, int x, int y)
        {
            var putHere = &newPointer[y * newData.Stride + x * 4 + channelIndex];
            Span<int> values = stackalloc int[kernelSize * kernelSize];
            var index = 0;

            for (var ky = 0; ky < kernelSize; ky++)
            for (var kx = 0; kx < kernelSize; kx++)
            {
                var px = x + kx - kernelSize / 2;
                var py = y + ky - kernelSize / 2;

                if (px < 0 || py < 0 || px >= newData.Width || py >= newData.Height)
                    continue;

                var offset = py * newData.Stride + px * 4;
                values[index] = oldPointer[offset + channelIndex];
                index++;
            }

            values = values[..index];
            values.Sort();
            if (values.Length % 2 == 0)
                *putHere = (byte)((values[values.Length / 2] + values[values.Length / 2 + 1]) / 2);
            else
                *putHere = (byte)values[values.Length / 2];
        }
    }


    private static void SpawnImage(Bitmap image, string title, string explain, int cellX, int cellY)
    {
        const int screenPad = 10;
        const int gap = 15;
        const int boxWidth = 400;
        const int boxHeight = 400;

        const int imagePad = 35;

        var panel = SpawnPanel();
        SpawnPictureBox(image);
        SpawnLabel(title, 0, 0);
        SpawnLabel(explain, 0, boxHeight - imagePad);

        Form.Controls.Add(panel);
        return;

        Panel SpawnPanel() => new()
        {
            Size = new Size(boxWidth, boxHeight),
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(screenPad + cellX * (boxWidth + gap), screenPad + cellY * (boxHeight + gap))
        };

        void SpawnLabel(string text, int x, int y) =>
            panel.Controls.Add(new Label
            {
                Location = new Point(x, y),
                Font = new Font("JetBrains Mono", 14, FontStyle.Bold),
                Size = new Size(boxWidth, imagePad),
                TextAlign = ContentAlignment.TopCenter,
                AutoEllipsis = true,
                Text = text
            });

        void SpawnPictureBox(Bitmap img) =>
            panel.Controls.Add(new PixelBox
            {
                Image = img,
                Size = new Size(boxWidth - imagePad * 2, boxHeight - imagePad * 2),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(imagePad, imagePad)
            });
    }


    private static void InitApp()
    {
        var imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(ImageCP)}.{ImageName}") ??
                          throw new Exception("WHERE IS MY IMAGE");

        var original = new Bitmap(imageStream);
        original = new Bitmap(original, new Size(256, 256));


        var (additiveNoise, _) =
            ImageTransformer.ApplyBitmapTransform(original, AddGaussianNoise());

        var (multiplicativeNoise, _) =
            ImageTransformer.ApplyBitmapTransform(original, AddMultiplicativeNoise());


        var (saltPepperNoise, _) =
            ImageTransformer.ApplyBitmapTransform(original, AddSaltPepperNoise());


        SpawnImage(original, "Task 1: Input image", "256x256", 0, 0);
        SpawnImage(additiveNoise, "Task 2", "Additive noise", 0, 1);
        SpawnImage(multiplicativeNoise, "Task 3", "Multiplicative noise", 1, 0);
        SpawnImage(saltPepperNoise, "Task 4", "Salt and pepper noise", 1, 1);
        var desctop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        saltPepperNoise.Save(Path.Combine(desctop, "saltPepperNoise.jpg"), ImageFormat.Jpeg);

        int[] filterSizes = [3, 5, 7, 11];
        for (var index = 0; index < filterSizes.Length; index++)
        {
            var filterSize = filterSizes[index];
            var additiveNoiseFiltered = MeanFilter(additiveNoise, filterSize);
            var multiplicativeNoiseFiltered = MeanFilter(multiplicativeNoise, filterSize);
            var saltPepperNoiseFiltered = MeanFilter(saltPepperNoise, filterSize);

            SpawnImage(additiveNoiseFiltered, $"Task 5: Additive mean",
                $"Filter size: {filterSize}x{filterSize}", index, 2);
            SpawnImage(multiplicativeNoiseFiltered, $"Task 5: Multiplicative mean",
                $"Filter size: {filterSize}x{filterSize}", index, 3);
            SpawnImage(saltPepperNoiseFiltered, $"Task 5: Salt and pepper mean",
                $"Filter size: {filterSize}x{filterSize}", index, 4);
        }

        for (var index = 0; index < filterSizes.Length; index++)
        {
            var filterSize = filterSizes[index];
            var additiveNoiseFiltered = MedianFilter(additiveNoise, filterSize);
            var multiplicativeNoiseFiltered = MedianFilter(multiplicativeNoise, filterSize);
            var saltPepperNoiseFiltered = MedianFilter(saltPepperNoise, filterSize);

            SpawnImage(additiveNoiseFiltered, $"Task 6: Additive median",
                $"Filter size: {filterSize}x{filterSize}", index, 5);
            SpawnImage(multiplicativeNoiseFiltered, $"Task 6: Multiplicative median",
                $"Filter size: {filterSize}x{filterSize}", index, 6);
            SpawnImage(saltPepperNoiseFiltered, $"Task 6: SP median",
                $"Filter size: {filterSize}x{filterSize}", index, 7);
        }
    }


    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        InitApp();
        Application.Run(Form);
    }
}