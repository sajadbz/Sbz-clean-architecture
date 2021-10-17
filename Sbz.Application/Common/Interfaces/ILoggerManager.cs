using Sbz.Application.Common.Models;
using System;
using System.Collections.Generic;

namespace Sbz.Application.Common.Interfaces
{
    public interface ILoggerManager<TController>
    {
        void LogInfo(string message);
        void LogWarn(string message);
        void LogDebug(string message);
        void LogError(string message);               
    }
}
