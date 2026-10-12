using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Finance_Tracker.ViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Field cannot be empty")]
        [EmailAddress]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "Cannot be empty")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [DisplayName("Remember Me ")]
        public bool Rememberme { get; set; }
    }
}
