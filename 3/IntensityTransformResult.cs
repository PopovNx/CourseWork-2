namespace ImageCP;

public class IntensityTransformResult
{
    public double LMin { get; set; } = double.MaxValue;
    public double LMax { get; set; } = double.MinValue;

    public double K => (LMax - LMin) / LMax;
    public List<double> IntensityValues { get; set; } = [];
    public int[] Histogram { get; set; } = new int[256];
    public readonly double[] Cdfa = new double[256];
    public double Csm(int i) => Cdfa[i];
    public bool DfgDone = false;
}