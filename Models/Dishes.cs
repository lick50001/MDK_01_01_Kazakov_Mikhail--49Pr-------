using System.ComponentModel.DataAnnotations.Schema;

namespace RestApiKazakov.Models
{
    public class Dishes
    {
        public int Id { get; set; }
        [ForeignKey("Menu")]  // 👈 Явно указываем, что это FK для свойства Menu
        public int MenuId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }

        [ForeignKey("MenuId")]
        public virtual Menus Menu { get; set; }
    }
}
