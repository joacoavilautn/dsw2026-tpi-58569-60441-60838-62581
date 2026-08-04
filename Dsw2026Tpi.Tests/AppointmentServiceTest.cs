using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Tests
{
    public class AppointmentServiceTest
    {
        private Dsw2026TpiDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<Dsw2026TpiDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new Dsw2026TpiDbContext(options);
        }

        [Fact]
        public async Task BookAppointmentAsync_CuandoElRequestEsValido_EntoncesSeCreaElTurno()
        {
            // Arrange (Preparación)
            using var context = CreateInMemoryDbContext();
            var loggerMock = Substitute.For<ILogger<AppointmentService>>();
            var service = new AppointmentService(context, loggerMock);

            var specialityId = Guid.NewGuid();
            var speciality = new Speciality("Cardiologia", "Especialidad del corazon", specialityId);
            context.Specialities.Add(speciality);
            var doctorId = Guid.NewGuid();
            var doctor = new Doctor("Dr. Juan Perez", "MP12345", specialityId, doctorId);
            context.Doctors.Add(doctor);

            var patientUser = new Patient(Guid.NewGuid(), "12345678", "Carlos Gomez");
            context.Patients.Add(patientUser);

            var futureDate = DateTime.UtcNow.AddDays(1).Date;
            var slot = new AvailabilitySlot
            {
                DoctorId = doctorId,
                SlotDate = futureDate,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(10, 30, 0)
            };
            context.AvailabilitySlots.Add(slot);
            await context.SaveChangesAsync();

            var request = new AppointmentModel.Request(
                doctorId,
                slot.Id,
                new AppointmentModel.PatientDto("12345678"),
                "Consulta general de rutina"
            );

            // Act (Ejecución)
            var response = await service.BookAppointmentAsync(request);

            // Assert (Verificación)
            Assert.NotNull(response);
            Assert.Equal(doctorId, response.Doctor.DoctorId);
            Assert.Equal("BOOKED", response.AppointmentsStatus);

            // Verificar que el slot en la base de datos cambió de estado a BOOKED
            var updatedSlot = await context.AvailabilitySlots.FindAsync(slot.Id);
            Assert.NotNull(updatedSlot);
            Assert.Equal(SlotStatus.BOOKED, updatedSlot.Status);

            // Verificar que el Appointment fue persistido en la base de datos
            var savedAppointment = await context.Appointments.FirstOrDefaultAsync(a => a.PatientId == patientUser.Id);
            Assert.NotNull(savedAppointment);
            Assert.Equal(AppointmentStatus.BOOKED, savedAppointment.Status);
        }

        [Fact]
        public async Task BookAppointmentAsync_CuandoElDoctorNoExiste_EntoncesLanzaEntityNotFoundException()
        {
            // Arrange (Preparación)
            using var context = CreateInMemoryDbContext();
            var loggerMock = Substitute.For<ILogger<AppointmentService>>();
            var service = new AppointmentService(context, loggerMock);

            var nonExistentDoctorId = Guid.NewGuid();
            var request = new AppointmentModel.Request(
                nonExistentDoctorId,
                Guid.NewGuid(),
                new AppointmentModel.PatientDto("12345678"),
                "Consulta medica"
            );

            // Act & Assert (Ejecución y Verificación de Excepción)
            var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => service.BookAppointmentAsync(request));
            Assert.Contains("Doctor", exception.Message);
        }

        [Fact]
        public async Task BookAppointmentAsync_CuandoElSlotNoEstaDisponible_EntoncesLanzaConflictException()
        {
            // Arrange (Preparación - RN03: Sin superposición / Slot no disponible)
            using var context = CreateInMemoryDbContext();
            var loggerMock = Substitute.For<ILogger<AppointmentService>>();
            var service = new AppointmentService(context, loggerMock);

            var specialityId = Guid.NewGuid();
            var speciality = new Speciality("Clinica", "Medicina general", specialityId);
            context.Specialities.Add(speciality);
            var doctorId = Guid.NewGuid();
            var doctor = new Doctor("Dra. Ana Lopez", "MP67890", specialityId, doctorId);
            context.Doctors.Add(doctor);

            var patientUser = new Patient(Guid.NewGuid(), "87654321", "Maria Rodriguez");
            context.Patients.Add(patientUser);

            var slot = new AvailabilitySlot
            {
                DoctorId = doctorId,
                SlotDate = DateTime.UtcNow.AddDays(2).Date,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(14, 30, 0)
            };
            slot.Reserve(); // Cambiamos el estado a BOOKED previamente para simular ocupado
            context.AvailabilitySlots.Add(slot);
            await context.SaveChangesAsync();

            var request = new AppointmentModel.Request(
                doctorId,
                slot.Id,
                new AppointmentModel.PatientDto("87654321"),
                "Consulta de seguimiento"
            );

            // Act & Assert (Ejecución y Verificación de Conflicto RN03)
            var exception = await Assert.ThrowsAsync<ConflictException>(() => service.BookAppointmentAsync(request));
            Assert.Equal("SLOT_NOT_AVAILABLE", exception.Error.ErrorCode);
        }

        [Fact]
        public async Task BookAppointmentAsync_CuandoLaFechaDelSlotEsPasada_EntoncesLanzaBusinessRuleException()
        {
            // Arrange (Preparación - RN04: Reserva futura / No fechas pasadas)
            using var context = CreateInMemoryDbContext();
            var loggerMock = Substitute.For<ILogger<AppointmentService>>();
            var service = new AppointmentService(context, loggerMock);

            var specialityId = Guid.NewGuid();
            var speciality = new Speciality("Traumatologia", "Especialidad de huesos", specialityId);
            context.Specialities.Add(speciality);
            var doctorId = Guid.NewGuid();
            var doctor = new Doctor("Dr. Esteban Quito", "MP99999", specialityId, doctorId);
            context.Doctors.Add(doctor);

            var patientUser = new Patient(Guid.NewGuid(), "11223344", "Pedro Picapiedra");
            context.Patients.Add(patientUser);

            var pastDate = DateTime.UtcNow.AddDays(-5).Date; // Fecha pasada
            var slot = new AvailabilitySlot
            {
                DoctorId = doctorId,
                SlotDate = pastDate,
                StartTime = new TimeSpan(09, 0, 0),
                EndTime = new TimeSpan(09, 30, 0)
            };
            context.AvailabilitySlots.Add(slot);
            await context.SaveChangesAsync();

            var request = new AppointmentModel.Request(
                doctorId,
                slot.Id,
                new AppointmentModel.PatientDto("11223344"),
                "Intento de reserva pasada"
            );

            // Act & Assert (Ejecución y Verificación de Regla de Negocio RN04)
            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.BookAppointmentAsync(request));
            Assert.Equal("PAST_DATE_NOT_ALLOWED", exception.Error.ErrorCode);
        }
    }
}
