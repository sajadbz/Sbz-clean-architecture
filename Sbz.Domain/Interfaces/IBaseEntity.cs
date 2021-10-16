namespace Sbz.Domain.Interfaces
{
    public interface IBaseEntity<TKey>
    {
        TKey Id { get; set; }
    }
}
