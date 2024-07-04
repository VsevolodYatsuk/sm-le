using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;

namespace smile.server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;

        public ImagesController(IWebHostEnvironment env)
        {
            _env = env;
            _contentTypeProvider = new FileExtensionContentTypeProvider();
        }

        [HttpGet("{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            var imagePath = Path.Combine(_env.WebRootPath, "images", fileName);

            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound();
            }

            if (!_contentTypeProvider.TryGetContentType(imagePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var image = System.IO.File.OpenRead(imagePath);
            return File(image, contentType);
        }
    }
}
