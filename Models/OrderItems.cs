using System.ComponentModel.DataAnnotations.Schema;

namespace RestApiKazakov.Models
{
    public class OrderItems
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int DishId { get; set; }
        public int Quantity { get; set; }

        [ForeignKey("OrderId")]
        public virtual Orders Order { get; set; }

        [ForeignKey("DishId")]
        public virtual Dishes Dish { get; set; }
    }
}
