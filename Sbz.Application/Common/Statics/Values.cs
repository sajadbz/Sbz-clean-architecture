using Bz.ClassFinder.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sbz.Application.Statics
{
    public class Values
    {
        public static List<BzClassInfo> Permissions
        {
            get
            {
                var permission = Bz.ClassFinder.Helper
                    .GetClassAndMethods(Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Sbz.WebUI.dll"))
                    .ToList();
                //permission.Add(_otherBzClassInfo);

                return permission.Where(c => c.Methods.Any()).OrderBy(c => c.FullName).ToList();
            }
        }
    }
}
