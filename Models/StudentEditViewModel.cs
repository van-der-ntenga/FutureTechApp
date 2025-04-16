using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace FututreTechApp.Models
{
    public class StudentEditViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
        public string EnrollmentStatus { get; set; }
        public string PhotoUrl { get; set; }

        public List<SelectListItem> EnrollmentStatusList { get; set; }
    }
}
