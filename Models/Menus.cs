namespace RestApiKazakov.Models
{
    public class Menus
    { 
        public int Id {  get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Dishes> Dishes { get; set; }
    }
}
