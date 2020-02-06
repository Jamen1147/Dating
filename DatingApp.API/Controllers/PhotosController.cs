using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
  [Authorize]
  [Route("api/users/{userId}/photos")]
  [ApiController]
  public class PhotosController : ControllerBase
  {
    private readonly IDatingRepository _repo;
    private readonly IMapper _mapper;
    private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
    private Cloudinary _cloudinary;

    public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
    {
      _cloudinaryConfig = cloudinaryConfig;
      _mapper = mapper;
      _repo = repo;

      Account acc = new Account(
        _cloudinaryConfig.Value.CloudName,
        _cloudinaryConfig.Value.ApiKey,
        _cloudinaryConfig.Value.ApiSecret
      );

      _cloudinary = new Cloudinary(acc);
    }

    [HttpGet("{id}", Name = "GetPhoto")]
    public async Task<IActionResult> GetPhoto(int id)
    {
      var photoFromRepo = await _repo.GetPhoto(id);
      var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);
      return Ok(photo);
    }

    [HttpPost]
    public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
    {
      // Double check user id
      if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
        return Unauthorized();

      var userFromRepo = await _repo.GetUser(userId);

      // Get file being passed in from the request and store it to a var
      var file = photoForCreationDto.File;

      // Init the cloudinary upload result method and store it to a var
      var uploadResult = new ImageUploadResult();

      // Check if the file being passed in is empty
      if (file.Length > 0)
      {
        // create a disposible file stream with the use of 'using' by reading the file passed in
        using (var stream = file.OpenReadStream())
        {
          // create upload params/options, which includes the File requring the stream and a name
          // and the Transformation definding how the image should look like
          var uploadParams = new ImageUploadParams()
          {
            File = new FileDescription(file.Name, stream),
            Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
          };

          // start upload and get the upload info back from the upload method then store it to the upload result that was previously created
          uploadResult = _cloudinary.Upload(uploadParams);
        }
      }

      // now that i have the upload result containing a lot of info
      // i need to use it to fill out the photoCreateDto properties.
      photoForCreationDto.Url = uploadResult.Uri.ToString();
      photoForCreationDto.PublicId = uploadResult.PublicId;

      // then map the Dto to the real Photo model
      var photo = _mapper.Map<Photo>(photoForCreationDto);

      // check if the user already has a default photo
      // if not, set this photo to default
      if (!userFromRepo.Photos.Any(u => u.IsMain))
      {
        photo.IsMain = true;
      }

      // add this photo to user
      userFromRepo.Photos.Add(photo);

      // save all repo changes and check if it was successful
      if (await _repo.SaveAll())
      {
        // as we don't return all the photo info, we then map the full photo to the photoForReturnDto, which has reduced some unecessary properties
        var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);

        return CreatedAtRoute("GetPhoto", new
        {
          userId = userId,
          id = photo.Id
        }, photoToReturn);
      }

      return BadRequest("Could not add the photo");
    }
  }
}