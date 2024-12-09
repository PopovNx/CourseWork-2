namespace ImageCP;

public delegate ((PixelTransform Prepare, PixelTransform Modify, PixelTransform Evaluate) Transform, T Result)
    PerformResultedTransform<T>();