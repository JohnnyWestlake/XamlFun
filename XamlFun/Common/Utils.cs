using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace XamlFun.Common
{
    public class Utils
    {
        public static Random Random { get; } = new Random();

        public static Color GetRandomColor()
        {
            return Color.FromArgb(255, (byte)Random.Next(0, 255), (byte)Random.Next(0, 255), (byte)Random.Next(0, 255));
        }
    }
}
