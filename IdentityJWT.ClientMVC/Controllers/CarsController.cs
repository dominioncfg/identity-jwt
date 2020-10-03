using ClientMVC.Models;
using IdentityJWT.ClientMVC.Infra;
using IdentityJWT.ClientMVC.Models.ViewModels;
using IdentityJWT.ClientMVC.Services.HttpClientService;
using IdentityJWT.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityJWT.ClientMVC.Controllers
{
    public class CarsController : Controller
    {
        private readonly IHttpClientService _httpClientService;
        private readonly string _apiUrl;

        public CarsController(IHttpClientService httpClientService, IConfiguration configuration)
        {
            _httpClientService = httpClientService;
            _apiUrl = configuration["ClientAuthSettings:ApiServerUrl"];
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> IndexPublic()
        {
            string epUrl = Endpoints.Api.Cars.Public(_apiUrl);
            var theCars = await _httpClientService.GetAsync<IEnumerable<Car>>(epUrl, false);

            CarsListViewModel viewModel = new CarsListViewModel()
            {
                Cars = theCars,
            };
            return View(viewModel);
        }

        [HttpGet]
        [AuthorizeAnyAppRolePolicy]
        public async Task<IActionResult> IndexAnyUser()
        {
            string epUrl = Endpoints.Api.Cars.AnyRole(_apiUrl);
            var theCars = await _httpClientService.GetAsync<IEnumerable<Car>>(epUrl, true);

            CarsListViewModel viewModel = new CarsListViewModel()
            {
                Cars = theCars,
            };
            return View(viewModel);
        }

        [HttpGet]
        [AuthorizeAdminUserOnlyPolicy]
        public async Task<IActionResult> IndexAdmin()
        {
            string epUrl = Endpoints.Api.Cars.Admin(_apiUrl);
            var theCars = await _httpClientService.GetAsync<IEnumerable<Car>>(epUrl, true);

            CarsListViewModel viewModel = new CarsListViewModel()
            {
                Cars = theCars,
            };
            return View(viewModel);
        }
    }
}
