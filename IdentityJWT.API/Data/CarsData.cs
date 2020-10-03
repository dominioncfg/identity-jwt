using IdentityJWT.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityJWT.API.Data
{
    public class CarsData
    {
        public static List<Car> PublicCars
        {
            get
            {
                return new List<Car>()
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
            }
        }

        public static List<Car> AnyUserCars
        {
            get
            {
                return PublicCars.ToList().Concat(new List<Car>()
                {
                       new Car()
                    {
                        Brand = "SecretCar",
                        Model = "LoggedUserCar",
                        MaxSpeed = 500,
                    },
                }).ToList();
               
            }
        }

        public static List<Car> AdminCars
        {
            get
            {
                return AnyUserCars.ToList().Concat(new List<Car>()
                {
                       new Car()
                    {
                        Brand = "SuperSecretCar",
                        Model = "AdminCar",
                        MaxSpeed = 10000,
                    },
                }).ToList();

            }
        }
    }
}
