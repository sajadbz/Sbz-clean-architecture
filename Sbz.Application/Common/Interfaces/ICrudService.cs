using System.Linq;
using System.Threading.Tasks;

namespace Sbz.Application.Common.Interfaces
{
    public interface ICrudService<TKey, TIndexVm, TCreateVm> :
        ICrudCreateService<TKey,TCreateVm>,
        ICrudReadService<TKey, TIndexVm, TCreateVm>, 
        ICrudUpdateService<TCreateVm>,         
        ICrudDeleteService<TKey> where TKey : struct where TIndexVm : class where TCreateVm : class
    {
        
    }    
    public interface ICrudCreateService<TKey,TCreateVm> where TKey : struct where TCreateVm : class
    {
        Task<TKey> AddAsync(TCreateVm vm);
    }
    public interface ICrudReadService<TKey, TIndexVm, TCreateVm> where TKey : struct where TIndexVm : class where TCreateVm : class
    {
        IQueryable<TIndexVm> GetAllAsQuery();
        Task<TCreateVm> FindAsync(TKey id);
        Task<bool> IsExistAsync(TKey id);
    }
    public interface ICrudUpdateService<TCreateVm> where TCreateVm : class
    {
        Task<bool> UpdateAsync(TCreateVm vm);
    }    
    public interface ICrudDeleteService<TKey> where TKey : struct
    {
        Task<bool> SoftDeleteAsync(TKey id);
    }
}
