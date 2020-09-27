using IdentityJWT.API.Models;
using IdentityJWT.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityJWT.API.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class CarsController : ControllerBase
    {
        private readonly List<Car> _carsData = new List<Car>()
        {
            new Car()
            {
                Brand = "Toyota",
                Model = "Yaris",
                MaxSpeed = 224,
            },
            new Car()
            {
                Brand = "Toyota",
                Model = "Auris",
                MaxSpeed = 250,
            }
        };
        public CarsController()
        {

        }

        [HttpGet("public")]
        public async Task<IEnumerable<Car>> GetAllCars()
        {
            return await Task.FromResult(_carsData.ToList());
        }

        [Authorize]
        [HttpGet("any-logged-on")]
        public async Task<IEnumerable<Car>> GetAllCarsAnyUser()
        {
            return await Task.FromResult(_carsData.ToList());
        }

        [AuthorizeAnyAppRolePolicy]
        [HttpGet("any-role")]
        public async Task<IEnumerable<Car>> GetAllCarsAnyRole()
        {
            return await Task.FromResult(_carsData.ToList());
        }

        [AuthorizeAdminUserOnlyPolicy]
        [HttpGet("admin")]
        public async Task<IEnumerable<Car>> GetAllCarsAdmin()
        {
            return await Task.FromResult(_carsData.ToList());
        }
    }
}
