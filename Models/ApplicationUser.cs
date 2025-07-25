﻿using Microsoft.AspNetCore.Identity;

namespace MyWebApp.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}