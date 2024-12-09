using System.Drawing.Imaging;

namespace ImageCP;

public static unsafe class ImageTransformer
{
    public static PixelTransform EmptyTransform = _ => { };

    public static (Bitmap NewImage, T Result) ApplyBitmapTransform<T>(Bitmap image, PerformResultedTransform<T> modify)
    {
        const PixelFormat format = PixelFormat.Format32bppArgb;
        var rect = new Rectangle(Point.Empty, image.Size);

        var newImage = image.Clone(rect, format);
        var data = newImage.LockBits(rect, ImageLockMode.ReadWrite, format);
        var (context, result) = modify();
        var pointer = (byte*)data.Scan0;
        for (var end = pointer + data.Stride * data.Height; pointer < end; pointer += 4)
            context.Prepare(new PixelRef(&pointer[2], &pointer[1], &pointer[0]));

        pointer = (byte*)data.Scan0;
        for (var end = pointer + data.Stride * data.Height; pointer < end; pointer += 4)
            context.Modify(new PixelRef(&pointer[2], &pointer[1], &pointer[0]));

        pointer = (byte*)data.Scan0;
        for (var end = pointer + data.Stride * data.Height; pointer < end; pointer += 4)
            context.Evaluate(new PixelRef(&pointer[2], &pointer[1], &pointer[0]));

        newImage.UnlockBits(data);
        return (newImage, Result: result);
    }
}