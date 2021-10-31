using System.Threading.Tasks;
using Sbz.Application.Common.Models;

namespace Sbz.Application.Common.Interfaces
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model);
        Task<string> RenderDefaultEmailBodyAsync(EmailDefaultModel model);
        string GenerateCurrentCultureViewName(string viewName);

    }
}