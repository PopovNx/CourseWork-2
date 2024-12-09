using System.Reflection;

namespace ImageCP;

internal static unsafe class Program
{
    private const double LowOut = 45;
    private const double HighOut = 220;
    private const double Gamma = 1.5;
    private const string ImageName = "input.png";

    private static readonly Form Form = new()
    {
        Text = "ImageCP",
        ShowIcon = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        StartPosition = FormStartPosition.CenterScreen,
    };

    private static PerformResultedTransform<TransformResultResult> Task2TransformGrayscale()
    {
        var result = new TransformResultResult();
        return () => ((
                ImageTransformer.EmptyTransform, @ref =>
                {
                    var (r, g, b) = @ref;
                    *r = *g = *b = (byte)(0.299 * *r + 0.587 * *g + 0.114 * *b);
                }, @ref =>
                {
                    var (r, _, _) = @ref;
                    result.LMax = Math.Max(*r, result.LMax);
                    result.LMin = Math.Min(*r, result.LMin);
                }),
            result);
    }

    private static PerformResultedTransform<TransformResultResult> Task3TransformToneGrayscale(
        TransformResultResult previousResult)
    {
        var result = new TransformResultResult();

        return () => (
            (ImageTransformer.EmptyTransform,
                @ref =>
                {
                    var (r, g, b) = @ref;
                    var i = *r;
                    var sigma = Math.Pow((i - previousResult.LMin) / (previousResult.LMax - previousResult.LMin),
                        Gamma);
                    if (double.IsNaN(sigma)) sigma = 0;
                    var creeper = LowOut + (HighOut - LowOut) * sigma;
                    *r = *g = *b = (byte)creeper;
                  
                },
                @ref =>
                {
                    var (r, _, _) = @ref;
                    result.LMax = Math.Max(*r, result.LMax);
                    result.LMin = Math.Min(*r, result.LMin);
                }), result);
    }

    private static PerformResultedTransform<TransformResultResult> TransformNiggaTive(
        TransformResultResult previousResult)
    {
        var result = new TransformResultResult();
        return () => (
            (
                ImageTransformer.EmptyTransform,
                @ref =>
                {
                    var (r, g, b) = @ref;
                    *r = *g = *b = (byte)(previousResult.LMax - *r);
                },
                @ref =>
                {
                    var (r, _, _) = @ref;
                    result.LMax = Math.Max(*r, result.LMax);
                    result.LMin = Math.Min(*r, result.LMin);
                }
            ),
            result);
    }

    private static void SpawnImage(Bitmap image, string title, string explain, int cellX, int cellY)
    {
        const int screenPad = 10;
        const int gap = 15;
        const int boxWidth = 600;
        const int boxHeight = 600;

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
            panel.Controls.Add(new PictureBox
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
        SpawnImage(original,
            "Original Image",
            "",
            0, 0);


        var (grayscaleImg, grayscaleResult) =
            ImageTransformer.ApplyBitmapTransform(original, Task2TransformGrayscale());
        SpawnImage(grayscaleImg,
            $"Grayscale Image",
            $"K={grayscaleResult.K:F2},Lmin={grayscaleResult.LMin:F2},Lmax={grayscaleResult.LMax:F2}",
            1, 0);

        var (toneGrayscaleImage, toneGrayscaleResult) =
            ImageTransformer.ApplyBitmapTransform(original, Task3TransformToneGrayscale(grayscaleResult));


        SpawnImage(toneGrayscaleImage, $" LO:{LowOut} LH:{HighOut} GM:{Gamma} ",
            $"K={toneGrayscaleResult.K:F2},Lmin={toneGrayscaleResult.LMin:F2},Lmax={toneGrayscaleResult.LMax:F2}"
            , 0,
            1);

        var (niggaImage, niggaResult) =
            ImageTransformer.ApplyBitmapTransform(toneGrayscaleImage, TransformNiggaTive(toneGrayscaleResult));
        SpawnImage(niggaImage, $"Inverted Image",
            $"K={niggaResult.K:F2},Lmin={niggaResult.LMin:F2},Lmax={niggaResult.LMax:F2}",
            1,
            1);
    }

    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        InitApp();
        Application.Run(Form);
    }
}