using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace API.Controllers;

public class AdminController : BaseApiController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _uow;
    private readonly IPhotoService _photoService;

    public AdminController(UserManager<AppUser> userManager, IUnitOfWork uow, IPhotoService photoService)
    {
        _userManager = userManager;
        _uow = uow;
        _photoService = photoService;
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("users-with-roles")]
    public async Task<ActionResult> GetUsersWithRoles()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .Select(u => new
            {
                u.Id,
                Username = u.UserName,
                Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
            })
            .ToListAsync();

        return Ok(users);
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("edit-roles/{username}")]
    public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
    {
        if (string.IsNullOrEmpty(roles)) return BadRequest("No roles have been selected");

        var selectedRoles = roles.Split(",").ToArray();

        var user = await _userManager.FindByNameAsync(username);

        if (user == null) return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);

        var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        if (!result.Succeeded) return BadRequest("Failed to add roles. " + result.Errors);

        result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        if (!result.Succeeded) return BadRequest("Failed to remove roles. " + result.Errors);

        return Ok(await _userManager.GetRolesAsync(user));
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photos-to-moderate")]
    public async Task<ActionResult<IEnumerable<PhotoForApprovalDto>>> GetPhotosForModeration()
    {
        return Ok(await _uow.PhotoRepository.GetUnapprovedPhotos());
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("approve-photo/{id}")]
    public async Task<ActionResult> ApprovePhoto(int id)
    {
        var photo = await _uow.PhotoRepository.GetPhotoById(id);
        if (photo == null) return NotFound("Photo " + id + " not found");

        photo.IsApproved = true;

        var user = await _uow.UserRepository.GetUserByPhotoIdAsync(id);

        if (user == null) return NotFound("User not found");
        
        if (!user.Photos.Any(p => p.IsMain)) photo.IsMain = true;

        if (await _uow.Complete()) return Ok();

        return BadRequest("Failed to update photo");
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("reject-photo/{id}")]
    public async Task<ActionResult> RejectPhoto(int id)
    {
        var photo = await _uow.PhotoRepository.GetPhotoById(id);
        if (photo.PublicId != null)
        {
            var result = await
            _photoService.DeletePhotoAsync(photo.PublicId);
            if (result.Result == "ok")
            {
                _uow.PhotoRepository.RemovePhoto(photo);
            }
        }
        else
        {
            _uow.PhotoRepository.RemovePhoto(photo);
        }
        await _uow.Complete();
        return Ok();
    }
}
