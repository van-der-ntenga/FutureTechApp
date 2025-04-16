using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FututreTechApp.Models
{
    public class StudentViewModel
    {
        [JsonPropertyName("id")]
        public string? id { get; set; }
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(10)]
        public string MobileNumber { get; set; }

        [Required]
        public string EnrolmentStatus { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? Photo { get; set; } 
        
    }
}