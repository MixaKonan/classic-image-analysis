using System.Text.RegularExpressions;

namespace Enodatio.UI;

public static class Common
{
    public static class Regexes
    {
        public static readonly Regex NumbersOnly = new Regex("[^0-9.-]+");
    }
}