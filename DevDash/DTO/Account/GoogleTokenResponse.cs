﻿namespace DevDash.DTO.Account
{
    public class GoogleTokenResponse
    {
        public string AccessToken { get; set; }
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }
}
