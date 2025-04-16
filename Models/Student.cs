using System.Text.Json.Serialization;

namespace FututreTechApp.Models
{
    public class Student
    {
        [JsonPropertyName("id")]
        public string? id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("mobileNumber")]
        public string MobileNumber { get; set; }

        [JsonPropertyName("enrolmentStatus")]
        public string EnrolmentStatus { get; set; }

        [JsonPropertyName("profileImageUrl")]
        public string ProfileImageUrl { get; set; }
    }

}


