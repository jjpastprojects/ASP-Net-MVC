using System.ComponentModel.DataAnnotations;
using System.Web.UI.WebControls;

namespace WebScannerAnalyzer.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter user name.")]
        [Display(Name = "User")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Please enter password.")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}