using System;

namespace Sbz.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        int UserId { get; }        
        string AvatarPath { get; }
        string UserIp { get; }
        string UrlReferer { get; set; }
        string CurrentUrl { get; set; }
        string FullName { get; set; }
    }
}
