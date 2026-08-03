using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Data.Identity;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly Dsw2026TpiDbContext _dbContext;

    public AuthenticationService(UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        Dsw2026TpiDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        if (!request.Email.IsEmailValid()) throw new AuthenticationException();
        var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new AuthenticationException();
        var result = await _signInManager.CheckPassword(user, request.Password);

        if (!result)
        {
            _logger.LogError("Intento de login fallido para: {Email}", request.Email);
            throw new AuthenticationException();
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

        var token  = _jwtService.GenerateToken(user.UserName!, role);

        return new LoginAdminModel.Response(
            token,
            role
        );
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        if(string.IsNullOrWhiteSpace(request.Email) || request.Dni <= 0)
        {
            throw new ArgumentException("El email y el DNI son obligatorios. ");
        }

        var dniString = request.Dni.ToString();
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            var dniExists = _dbContext.Patients.Any(p => p.Dni == dniString);
            if (dniExists)
            {
                throw new ConflictException("PATIENT_DNI_EXITS", "El DNI ingresado ya se encuentra registrado con otro correo electrónico.");
            }
            
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
               throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT), 
                "No se pudo crear el usuario paciente.")
                .WithDetail(createResult.Errors.Select(e => (e.Code, e.Description)));
            }

            await _userManager.AddToRoleAsync(user, "PACIENTE");

            var userIdGuid = Guid.Parse(user.Id);
            var newPatient = new Patient(userIdGuid, dniString, null);

            _dbContext.Patients.Add(newPatient);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            var dniString = request.Dni.ToString();
            var userIdGuid = Guid.Parse(user.Id);
            var patient = _dbContext.Patients.FirstOrDefault(p => p.Id == userIdGuid);

            if (patient == null || patient.Dni != dniString)
            {
                _logger.LogError("Intento de login fallido para paciente: {Email}. DNI Incorrecto.", request.Email);
                throw new AuthenticationException();
            }
        }

        var token = _jwtService.GenerateToken(request.Email, "PACIENTE");

        return new LoginPatientModel.Response(token, "PACIENTE");


    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        if (!request.Email.IsEmailValid()) throw new ValidationException(ErrorCodes.REGISTER_USER_INVALID,
            nameof(ErrorCodes.REGISTER_USER_INVALID));

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT),
            ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));

        if (!await _roleManager.RoleExistsAsync(Roles.Administrator))
        {
            await _roleManager.CreateAsync(new IdentityRole(Roles.Administrator));
        }

        _ = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }
}
