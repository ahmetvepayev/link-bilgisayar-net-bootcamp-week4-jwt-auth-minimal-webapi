using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class TokenAuthConfig
{
    public static void AddTokenAuth(this IServiceCollection service, TokenOptions tokenOptions)
    {
        service.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts => {
            opts.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidIssuer = tokenOptions.Issuer,
                ValidAudience = tokenOptions.Audience[0],
                IssuerSigningKey = GetSymmetricSecurityKey(tokenOptions.SymmetricSecurityKey),

                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
    }

    public static TokenOptions GetTokenOptions(this ConfigurationManager configuration)
    {
        TokenOptions tokenOptions = new TokenOptions();
        configuration.GetSection("TokenSettings").Bind(tokenOptions);
        return tokenOptions;
    }

    public static TokenOptions GetTokenOptions(this IConfiguration configuration)
    {
        TokenOptions tokenOptions = new TokenOptions();
        configuration.GetSection("TokenSettings").Bind(tokenOptions);
        return tokenOptions;
    }

    public static SecurityKey GetSymmetricSecurityKey(string securityKey)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
    }
}