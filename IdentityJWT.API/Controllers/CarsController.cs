using IdentityJWT.API.Data;
using IdentityJWT.API.Models;
using IdentityJWT.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityJWT.API.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class CarsController : ControllerBase
    {
        
        public CarsController()
        {

        }

        [HttpGet("public")]
        public async Task<IEnumerable<Car>> GetAllCars()
        {
            return await Task.FromResult(CarsData.PublicCars.ToImmutableList());
        }

        [Authorize]
        [HttpGet("any-logged-on")]
        public async Task<IEnumerable<Car>> GetAllCarsAnyUser()
        {
            return await Task.FromResult(CarsData.AnyUserCars.ToImmutableList());
        }

        [AuthorizeAnyAppRolePolicy]
        [HttpGet("any-role")]
        public async Task<IEnumerable<Car>> GetAllCarsAnyRole()
        {
            return await Task.FromResult(CarsData.AnyUserCars.ToImmutableList());
        }

        [AuthorizeAdminUserOnlyPolicy]
        [HttpGet("admin")]
        public async Task<IEnumerable<Car>> GetAllCarsAdmin()
        {
            return await Task.FromResult(CarsData.AdminCars.ToImmutableList());
        }
    }
}
