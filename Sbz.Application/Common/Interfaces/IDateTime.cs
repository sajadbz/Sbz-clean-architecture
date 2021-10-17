using System;

namespace Sbz.Application.Common.Interfaces
{
    public interface IDateTime
    {
        DateTimeOffset Now { get; }        
        string NowName { get; }        
    }
}
