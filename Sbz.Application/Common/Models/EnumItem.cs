namespace Sbz.Application.Common.Models
{
    public class EnumItem
    {
        public EnumItem(int id, string name, string title)
        {
            Id = id;
            Name = name;
            Title = title;
        }
        public int Id { get; set; }
        public string Name { get; private set; }
        public string Title { get; private set; }
    }
}
