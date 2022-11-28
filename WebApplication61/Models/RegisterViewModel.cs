﻿using System.ComponentModel.DataAnnotations;

namespace WebApplication61.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(30, ErrorMessage = "Username can be max 30 charcters.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password can be min 6 charcters.")]
        [MaxLength(16, ErrorMessage = "Password can be max 16 charcters.")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Re-Password is required.")]
        [MinLength(6, ErrorMessage = "Re-Password can be min 6 charcters.")]
        [MaxLength(16, ErrorMessage = "Re-Password can be max 16 charcters.")]
        [Compare(nameof(Password))]
        public string RePassword { get; set; }
    }
}
