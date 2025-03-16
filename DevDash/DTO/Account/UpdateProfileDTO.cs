﻿namespace DevDash.DTO.Account
{
    public class UpdateProfileDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string? ImageUrl { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly Birthday { get; set; }
    }
}
