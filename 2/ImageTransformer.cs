using System.Drawing.Imaging;

namespace ImageCP;

public static unsafe class ImageTransformer
{
    public static (Bitmap NewImage, T Result) ApplyBitmapTransform<T>(Bitmap image, PerformResultedTransform<T> modify)
    {
        const PixelFormat format = PixelFormat.Format32bppArgb;
        var rect = new Rectangle(Point.Empty, image.Size);

        var newImage = image.Clone(rect, format);
        var data = newImage.LockBits(rect, ImageLockMode.ReadWrite, format);
        var (contexts, result) = modify();
        foreach (var context in contexts)
        {
            var pointer = (byte*)data.Scan0;
            for (var end = pointer + data.Stride * data.Height; pointer < end; pointer += 4)
                context(new PixelRef(&pointer[2], &pointer[1], &pointer[0]));
        }
        
        newImage.UnlockBits(data);
        return (newImage, Result: result);
    }
}