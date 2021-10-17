using System.Collections.Generic;
using System.Linq;

namespace Sbz.Application.Common.Extensions
{
    public static class NumberExtensions
    {
        public static string ConvertIntToStringWithOrders(this IEnumerable<int> numbers)
        {
            return string.Join(",", numbers.OrderBy(x => x));
        }
        public static string ConvertDoubleToStringWithOrders(this IEnumerable<double> numbers)
        {
            return string.Join(",", numbers.OrderBy(x => x));
        }
        public static string AddCommas(this double price)
        {
            return price.ToString("#,#.00");
        }
       
    }
}
