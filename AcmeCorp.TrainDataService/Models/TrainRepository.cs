﻿using System;
using System.Collections.Generic;

namespace AcmeCorp.TrainDataService.Models
{
    public class TrainRepository : IProvideTrain
    {
        readonly Dictionary<string, Train> _trains = new Dictionary<string, Train>();

        public Train GetTrain(string trainId)
        {
            if (!_trains.ContainsKey(trainId))
            {
                // First time, we create the train with default value
                var train = new Train(trainId);
                foreach (var c in "ABCDEFGHIJKL")
                {
                    var coach = new Coach(c.ToString());

                    for (var i = 1; i < 42; i++)
                    {
                        var seat = new Seat(coach.Name, i.ToString(), string.Empty);
                        coach.Seats.Add(seat);
                    }

                    train.Add(coach);
                }

                _trains.Add(trainId, train);
            }

            return _trains[trainId];
        }

        public void UpdateTrainReservations(TrainUpdateDTO trainUpdateDto)
        {
            if (string.IsNullOrEmpty(trainUpdateDto.train_id))
            {
                throw new InvalidOperationException("Must have a non-null or non-empty train_id");
            }

            var train = GetTrain(trainUpdateDto.train_id);

            var seats = new List<Seat>();
            foreach (var seatInString in trainUpdateDto.seats)
            {
                var s = new Seat(seatInString[1].ToString(), seatInString[0].ToString(), trainUpdateDto.booking_reference);
                seats.Add(s);
            }

            train.Reserve(seats, trainUpdateDto.booking_reference);
        }
    }

    public class Coach
    {
        public Coach(string coachName)
        {
            this.Name = coachName;
            this.Seats = new List<Seat>();
        }

        public List<Seat> Seats { get; set; }
        public string Name { get; set; }

        public void UpsertSeat(Seat seat)
        {
            this.Seats.Remove(seat);
            this.Seats.Add(seat);
        }
    }
}