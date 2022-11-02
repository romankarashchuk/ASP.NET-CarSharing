﻿namespace CarSharingApp.Models.RatingData
{
    // Модель для хранения
    public class RatingModel
    {
        public int Id { get; set; }

        public double Condition { get; set; }
        public double FuelConsumption { get; set; }
        public double EasyToDrive { get; set; }
        public double FamilyFriendly { get; set; }
        public double SUV { get; set; }

        public double Overall { get; set; }

        public int ReviewsCount { get; set; }
    }
}
