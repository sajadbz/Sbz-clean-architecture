
using System.Threading.Tasks;

namespace Sbz.Application.Common.Interfaces
{
    public interface IEmailService
    {
        bool SendEmail(string to, string subject, string body);
    }
}
