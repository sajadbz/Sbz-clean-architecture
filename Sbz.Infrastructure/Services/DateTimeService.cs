using System;
using Sbz.Application.Common.Interfaces;

namespace Sbz.Infrastructure.Services
{
    public class DateTimeService : IDateTime
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
        public string NowName => Now.ToString("yyyyMMddHHmmss");
    }
}
