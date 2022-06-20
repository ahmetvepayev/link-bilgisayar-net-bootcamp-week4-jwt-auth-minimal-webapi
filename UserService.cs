using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public UserService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public User UserRegister(UserDto userDto)
    {
        byte[]? passwordSalt, passwordHash;
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(userDto.Password));
        }

        var addedUser = new User(){
            Id = userDto.UserName,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };

        _context.Users.Add(addedUser);
        _context.SaveChanges();

        return addedUser;
    }

    public TokenDto UserLogin(UserDto userDto)
    {
        var user = _context.Users.Find(userDto.UserName);
        if (user == null)
        {
            return new TokenDto{AccessToken = "User not found"};
        }

        using (var hmac = new HMACSHA512(user.PasswordSalt))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(userDto.Password));
            if (!computedHash.SequenceEqual(user.PasswordHash))
            {
                return new TokenDto{AccessToken = "Incorrect password"};
            }

            return CreateFullToken(user);
        }
    }

    public TokenDto GetFullToken(string refreshToken)
    {
        var dbToken = _context.RefreshTokens.FirstOrDefault(x => x.Guid == refreshToken);
        if (dbToken == null)
        {
            return new TokenDto{AccessToken = "Invalid refresh token"};
        }
        else if (dbToken.Expiration < DateTime.Now)
        {
            return new TokenDto{AccessToken = "Expired refresh token"};
        }

        var user = _context.Users.Find(dbToken.UserName);
        return CreateFullToken(user);
    }

    private TokenDto CreateFullToken(User user)
    {
        var tokenOptions = _configuration.GetTokenOptions();

        var audiences = tokenOptions.Audience;
        var issuer = tokenOptions.Issuer;

        List<Claim> claimsList = new List<Claim>{
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };
        claimsList.AddRange(audiences.Select(x => new Claim(JwtRegisteredClaimNames.Aud, x)));
        
        var key = TokenAuthConfig.GetSymmetricSecurityKey(tokenOptions.SymmetricSecurityKey);

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var accessTokenExpiration = DateTime.Now.AddMinutes(tokenOptions.AccessTokenDuration);
        var refreshTokenExpiration = DateTime.Now.AddMinutes(tokenOptions.RefreshTokenDuration);

        var refreshToken = CreateRefreshToken();
        //-------------DbContext operation. Move elsewhere later
        var dbToken = _context.RefreshTokens.Find(user.Id);
        if (dbToken == null)
        {
            _context.RefreshTokens.Add(new(){UserName = user.Id, Guid = refreshToken, Expiration = refreshTokenExpiration});
        }
        else
        {
            dbToken.Guid = refreshToken;
            dbToken.Expiration = refreshTokenExpiration;
        }
        _context.SaveChanges();
        //----------------------------

        var token = new JwtSecurityToken(
            issuer : issuer,
            expires : accessTokenExpiration,
            notBefore : DateTime.Now,
            claims : claimsList,
            signingCredentials : credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var tokenDto = new TokenDto
        {
            AccessToken = accessToken,
            AccessTokenExpiration = accessTokenExpiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshTokenExpiration
        };

        return tokenDto;
    }

    private string CreateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
}