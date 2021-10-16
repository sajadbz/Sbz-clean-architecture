using Sbz.Domain.Interfaces;

namespace Sbz.Domain.Common
{
    public abstract class BaseEntity<TKey> : IBaseEntity<TKey>
    {
        public TKey Id { get; set; }
    }
}
