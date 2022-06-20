public class TokenOptions
{
    public List<string> Audience { get; set; }
    public string Issuer { get; set; }
    public int AccessTokenDuration { get; set; }
    public int RefreshTokenDuration { get; set; }
    public string SymmetricSecurityKey { get; set; }
}