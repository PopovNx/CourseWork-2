namespace ImageCP;

public delegate (IEnumerable<PixelTransform> Transform, T Result)
    PerformResultedTransform<T>();