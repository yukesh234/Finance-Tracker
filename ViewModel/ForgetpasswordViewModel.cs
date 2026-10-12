using System.ComponentModel.DataAnnotations;

namespace Finance_Tracker.ViewModel
{
    public class ForgetpasswordViewModel
    {
        [Required(ErrorMessage = "Field cannot be empty")]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
