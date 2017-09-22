﻿using System.Threading.Tasks;
using NFluent;
using NSubstitute;
using NUnit.Framework;
using TrainTrain.Domain;
using TrainTrain.Infra;

namespace TrainTrain.Test.Acceptance
{
    public class TrainTrainSystemShould
    {
        private const string TrainId = "9043-2017-09-22";
        private const string BookingReference = "75bcd15";

        [Test]
        public void Reserve_seats_when_train_is_empty()
        {
            const int seatsRequestedCount = 3;

            // Step1: Instantiate the "I want to go out" adapters
            var trainDataServiceAdapter = BuildTrainDataService(TrainId, TrainTopologyGenerator.With_10_available_seats());
            var bookingReferenceServiceAdapter = BuildBookingReferenceService(BookingReference);

            // Step2: Instantiate the hexagon
            IReserveSeats hexagon = new WebTicketManager(trainDataServiceAdapter, bookingReferenceServiceAdapter);

            // Step3: Instantiate the "I want to go in" adapter(s)
            var seatsReservationAdapter = new SeatsReservationAdapter(hexagon);
            var reservationRequestDto = new ReservationRequestDto() {number_of_seats = seatsRequestedCount, train_id = TrainId};

            string jsonAnswer = seatsReservationAdapter.Post(reservationRequestDto).Result;

            Check.That(jsonAnswer).IsEqualTo($"{{\"train_id\": \"{TrainId}\", \"booking_reference\": \"{BookingReference}\", \"seats\": [\"1A\", \"2A\", \"3A\"]}}");
        }

        [Test]
        public void Not_reserve_seats_when_it_exceed_max_capacty_threshold()
        {
            const int seatsRequestedCount = 3;

            var trainDataService = BuildTrainDataService(TrainId, TrainTopologyGenerator.With_10_seats_and_6_already_reserved());
            var bookingReferenceService = BuildBookingReferenceService(BookingReference);

            var webTicketManager = new WebTicketManager(trainDataService, bookingReferenceService);
            var reservation = webTicketManager.Reserve(TrainId, seatsRequestedCount).Result;

            Check.That(SeatsReservationAdapter.AdaptReservation(reservation))
                .IsEqualTo($"{{\"train_id\": \"{TrainId}\", \"booking_reference\": \"\", \"seats\": []}}");
        }

        [Test]
        public void Reserve_all_seats_in_the_same_coach()
        {
            const int seatsRequestedCount = 2;

            var trainDataService = BuildTrainDataService(TrainId, TrainTopologyGenerator.With_2_coaches_and_9_seats_already_reserved_in_the_first_coach());
            var bookingReferenceService = BuildBookingReferenceService(BookingReference);

            var webTicketManager = new WebTicketManager(trainDataService, bookingReferenceService);
            var reservation = webTicketManager.Reserve(TrainId, seatsRequestedCount).Result;

            Check.That(SeatsReservationAdapter.AdaptReservation(reservation))
                .IsEqualTo($"{{\"train_id\": \"{TrainId}\", \"booking_reference\": \"{BookingReference}\", \"seats\": [\"1B\", \"2B\"]}}");
        }

        private static IBookingReferenceService BuildBookingReferenceService(string bookingReference)
        {
            var bookingReferenceService = Substitute.For<IBookingReferenceService>();
            bookingReferenceService.GetBookingReference().Returns(Task.FromResult(bookingReference));
            return bookingReferenceService;
        }

        private static ITrainDataService BuildTrainDataService(string trainId, string trainTopology)
        {
            var trainDataService = Substitute.For<ITrainDataService>();
            trainDataService.GetTrain(trainId)
                .Returns(Task.FromResult( new Train( TrainDataService.AdaptTrainTopology(trainTopology))));
            return trainDataService;
        }
    }
}