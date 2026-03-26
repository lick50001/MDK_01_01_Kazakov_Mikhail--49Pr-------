using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestApiKazakov.Models
{
    [Table("OrderItems")]
    public class OrderItems
    {
        [Key]
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
