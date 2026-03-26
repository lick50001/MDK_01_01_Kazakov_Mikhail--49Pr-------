using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestApiKazakov.Models
{
    [Table("Orders")]
    public class Orders
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public decimal TotalAmount { get; set; }

        [ForeignKey("UserId")]
        public virtual Users User { get; set; }

        public virtual ICollection<OrderItems> OrderItems { get; set; }
    }
}
