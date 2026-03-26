using System.ComponentModel.DataAnnotations.Schema;

namespace RestApiKazakov.Models
{
    public class Orders
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public double TotalAmount { get; set; }

        [ForeignKey("UserId")]
        public virtual Users User { get; set; }

        // Навигационное свойство (Связь с позициями заказа)
        public virtual ICollection<OrderItems> OrderItems { get; set; }
    }
}
