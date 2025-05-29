using System.ComponentModel.DataAnnotations;

    public class Account
    {
        public int Id { get; set; }
        public required string FullName { get; set; } 
        public required string Email { get; set; }
        public decimal Balance { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
}