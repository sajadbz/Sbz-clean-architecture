using System;

namespace Sbz.Application.Common.Generator
{
    public static class CodeGenerator
    {
        public static int NewMobileActiveCode()
        {
            return new Random().Next(1000, 100000);
        }

        public static string NewGuidCode()
        {
            return Guid.NewGuid().ToString("N");
        }
               
    }
}
