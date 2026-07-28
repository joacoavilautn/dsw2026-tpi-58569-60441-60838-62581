namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : EntityBase
{
    public string Name { get; private set; }
    public string LicenseNumber { get; private set; }
    public Guid SpecialityId { get; private set; }
    public Speciality Speciality { get; private set; }
    public bool Deleted { get; private set; }

    #region Constructor for EF
#pragma warning disable CS8618
    private Doctor() { }
#pragma warning restore CS8618
    #endregion

    public Doctor(string name, string licenseNumber, Guid specialityId, Guid? id = null) : base(id)
    {
        Name = name;
        LicenseNumber = licenseNumber;
        SpecialityId = specialityId;
        Deleted = false;
    }

    public void Delete()
    {
        Deleted = true;
    }

    public void Update(string name, string licenseNumber, Guid specialityId)
    {
        Name = name;
        LicenseNumber = licenseNumber;
        SpecialityId = specialityId;
    }
}
