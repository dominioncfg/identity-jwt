using ClientMVC.Models;
using System.Collections.Generic;

namespace IdentityJWT.ClientMVC.Models.ViewModels
{
    public class CarsListViewModel
    {
        public IEnumerable<Car> Cars { get; set; }

    }
}
