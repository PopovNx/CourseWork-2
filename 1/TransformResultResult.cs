namespace ImageCP;

public class TransformResultResult
{
    public double LMin { get; set; }
    public double LMax { get; set; }

    public double K => (LMax - LMin) / LMax;
}