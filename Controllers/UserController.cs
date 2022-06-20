using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public IActionResult Register(UserDto userDto)
    {
        return Ok(_userService.UserRegister(userDto));
    }

    [HttpPost("login")]
    public IActionResult Login(UserDto userDto)
    {
        return Ok(_userService.UserLogin(userDto));
    }

    [HttpPost("refresh")]
    public IActionResult RefreshAccessToken([FromHeader]string refreshToken)
    {
        return Ok(_userService.GetFullToken(refreshToken));
    }

    [HttpGet]
    public IActionResult GetPublicData()
    {
        return Ok("Some public data");
    }

    [HttpGet("member"), Authorize]
    public IActionResult GetMemberData()
    {
        return Ok("Member only data");
    }
}