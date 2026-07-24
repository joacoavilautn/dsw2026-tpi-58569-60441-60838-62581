using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Patient : EntityBase
    {
        public Guid Id { get; init; }
        public Guid UserId { get; private set; }
        public string Dni { get; private set; }
        public string? FullName { get; private set; }
        public bool Deleted { get; private set;  }

        protected Patient() { }

        public Patient(Guid userId, string dni, string? fullName)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Dni = dni;
            FullName = fullName;
            Deleted = false;

        }
    }
}
