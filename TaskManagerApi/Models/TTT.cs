using System.Runtime.Intrinsics.X86;

namespace TaskManagerApi.Models
{
    public class TTT
    {
   public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool? IsCompleted { get; set; }
     
    }
}