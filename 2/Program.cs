using System.Reflection;
using ScottPlot;
using ScottPlot.Statistics;
using ScottPlot.WinForms;
using FontStyle = System.Drawing.FontStyle;
using Label = System.Windows.Forms.Label;

namespace ImageCP;

internal static unsafe class Program
{
    private const string ImageName = "input.png";

    private static readonly Form Form = new()
    {
        Text = "ImageCP",
        ShowIcon = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        StartPosition = FormStartPosition.CenterScreen,
    };

    private static PerformResultedTransform<IntensityTransformResult> Task2GetIntensity()
    {
        var result = new IntensityTransformResult();
        return () => ([
                @ref =>
                {
                    var (r, g, b) = @ref;
                    *r = *g = *b = (byte)(0.299 * *r + 0.587 * *g + 0.114 * *b);
                },
                @ref =>
                {
                    var (r, _, _) = @ref;
                    result.IntensityValues.Add(*r);
                    result.LMin = Math.Min(result.LMin, *r);
                    result.LMax = Math.Max(result.LMax, *r);
                }
            ],
            result);
    }

    private static PerformResultedTransform<IntensityTransformResult> Task4Distension(double lmin, double lmax)
    {
        var result = new IntensityTransformResult();
        return () => ([
                @ref =>
                {
                    var (r, g, b) = @ref;
                    var k = lmax - lmin;
                    var distension = 255.0 / k * (*r - lmin);
                    if (distension < lmin)
                        *r = 0;
                    else if (distension > lmax)
                        *r = 255;
                    else
                        *r = (byte)distension;

                    *g = *b = *r;
                },
                @ref =>
                {
                    var (r, _, _) = @ref;
                    result.IntensityValues.Add(*r);
                    result.LMin = Math.Min(result.LMin, *r);
                    result.LMax = Math.Max(result.LMax, *r);
                }
            ],
            result);
    }

    private static PerformResultedTransform<IntensityTransformResult> Task5EqualHista(int totalPixels)
    {
        var result = new IntensityTransformResult();
        return () => ([
                @ref =>
                {
                    var (r, _, _) = @ref;
                    result.Histogram[*r]++;
                },
                _ =>
                {
                    if (result.DfgDone) return;
                    var cumulative = 0;
                    for (var i = 0; i < 256; i++)
                    {
                        cumulative += result.Histogram[i];
                        result.Cdfa[i] = cumulative / (double)totalPixels;
                    }

                    result.DfgDone = true;
                },
                @ref =>
                {
                    var (r, g, b) = @ref;
                    *r = (byte)(result.Csm(*r) * 255);
                    *g = *b = *r;
                },
                @ref =>
                {
                    var (r, _, _) = @ref;
                    result.IntensityValues.Add(*r);
                    result.LMin = Math.Min(result.LMin, *r);
                    result.LMax = Math.Max(result.LMax, *r);
                }
            ],
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


    private static void SpawnChart(Action<Plot> plotAction)
    {
        var newForm = new Form();
        newForm.Text = "ImageCP Chart";
        newForm.ShowIcon = false;
        FormsPlot plot = new();
        plot.Size = new Size(1000, 600);
        plotAction(plot.Plot);
        plot.Plot.Axes.AutoScale();
        plot.Refresh();
        newForm.Controls.Add(plot);
        newForm.AutoSize = true;
        newForm.Show();
    }

    private static void InitApp()
    {
        var imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(ImageCP)}.{ImageName}") ??
                          throw new Exception("WHERE IS MY IMAGE");

        var original = new Bitmap(imageStream);

        var (grayscale, itr) =
            ImageTransformer.ApplyBitmapTransform(original, Task2GetIntensity());

        SpawnImage(grayscale,
            "Original Image",
            $"LMax: {itr.LMax}, LMin: {itr.LMin}, K: {itr.K:F4}",
            0, 0);

        HistDraw(itr, "Original Image Intensity Histogram");
        CumDraw(itr, "Original Image Intensity Cum");

        var (distension, itr2) =
            ImageTransformer.ApplyBitmapTransform(grayscale, Task4Distension(itr.LMin, itr.LMax));

        SpawnImage(distension,
            "Distension Image",
            $"LMax: {itr2.LMax}, LMin: {itr2.LMin}, K: {itr2.K:F4}",
            1, 0);
        HistDraw(itr2, "Distension Image Intensity Histogram");
        CumDraw(itr2, "Distension Image Intensity Cum");


        var (equalized, itr4) =
            ImageTransformer.ApplyBitmapTransform(grayscale, Task5EqualHista(original.Width * original.Height));
        SpawnImage(equalized,
            "Equalized Image",
            $"LMax: {itr4.LMax}, LMin: {itr4.LMin}, K: {itr4.K:F4}",
            0, 1);

        var (_, itr3) =
            ImageTransformer.ApplyBitmapTransform(equalized, Task2GetIntensity());
        HistDraw(itr3, "Equalized Image Intensity Histogram");
        CumDraw(itr3, "Equalized Image Intensity Cum");
    }

    private static void CumDraw(IntensityTransformResult itr, string title) =>
        SpawnChart(plot =>
        {
            var intensityValues = itr.IntensityValues;
            var intensityHist = new Histogram(intensityValues.Min(), intensityValues.Max(), 100);
            intensityHist.AddRange(intensityValues);
            plot.Title(title);
            var cumsum = intensityHist.GetCumulative();
            var max = cumsum.Max();
            cumsum = cumsum.Select(x => x / max).ToArray();
            var scatter = plot.Add.Scatter(
                Enumerable.Range(0, intensityHist.Bins.Length).Select(i => intensityHist.Bins[i]).ToArray(),
                cumsum);
            scatter.MarkerSize = 0;
            scatter.LineWidth = 4;
            scatter.Color = Colors.Crimson;
        });

    private static void HistDraw(IntensityTransformResult itr, string title) =>
        SpawnChart(plot =>
        {
            var intensityValues = itr.IntensityValues;
            var intensityHist = new Histogram(intensityValues.Min(), intensityValues.Max(), 10);
            intensityHist.AddRange(intensityValues);

            plot.Title(title);
            plot.Add.Bars(intensityHist.Bins.Zip(intensityHist.Counts).Select((barData, _) => new Bar
            {
                Position = barData.First,
                Value = barData.Second,
                FillColor = Colors.AliceBlue,
                Size = intensityHist.BinSize
            }));
        });

    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        InitApp();
        Application.Run(Form);
    }
}