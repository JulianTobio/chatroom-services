using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.API.Models;
using Users.API.Service;

namespace Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserProfileService userProfileService) : ControllerBase
{
    [HttpPost("profiles")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PublicUserProfile>>> GetProfiles(
        [FromBody, Required] UserProfilesRequest request)
    {
        if (request.UserIds.Count > 100)
        {
            ModelState.AddModelError(nameof(request.UserIds), "Se pueden consultar hasta 100 perfiles por solicitud.");
            return ValidationProblem(ModelState);
        }

        if (request.UserIds.Any(userId => userId <= 0))
        {
            ModelState.AddModelError(nameof(request.UserIds), "Los identificadores deben ser positivos.");
            return ValidationProblem(ModelState);
        }

        return Ok(await userProfileService.GetByIdsAsync(request.UserIds));
    }
}
