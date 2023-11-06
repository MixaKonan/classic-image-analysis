namespace Enodatio.Logic;

public static class Utils
{
    public static int Map(int source, int sourceFrom, int sourceTo, int targetFrom, int targetTo)
    {
        return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
    }

    public static double Map(double source, double sourceFrom, double sourceTo, double targetFrom, double targetTo)
    {
        return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
    }
}