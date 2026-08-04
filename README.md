# Trabajo Práctico Integrador
## Desarrollo de Software 2026

## Integrantes

| Legajo | Nombre |
|--------|--------|
| 60441 | Avila Joaquin |
| 58569 | Farias Romano Carlos Augusto |
| 60838 | Ortega Miguel Ignacio |
| 62581 | Quiroga Santiago |

---

## Requisitos previos

- .NET 10 SDK
- SQL Server (local o remoto)
- Git

---

## Configuración y ejecución local

1. Clonar el repositorio:

```
git clone <url-del-repositorio>
cd dsw2026-tpi
```

2. Configurar la cadena de conexión en `Dsw2026Tpi.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=Dsw2026TpiDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

3. Aplicar las migraciones para crear la base de datos:

```
dotnet ef database update --project Dsw2026Tpi.Data --startup-project Dsw2026Tpi.Api
```

4. Ejecutar la API:

```
dotnet run --project Dsw2026Tpi.Api
```

Al iniciar por primera vez, el sistema crea automáticamente los roles `ADMINISTRADOR` y `PACIENTE` en la base de datos. No se genera ningún usuario administrador por defecto. Para crear el primer administrador, usar el endpoint `POST /api/auth/admin/register` descripto más abajo.

5. La documentación interactiva (Swagger) estará disponible en:

```
https://localhost:{puerto}/swagger
```

---

## Endpoints implementados

Todos los endpoints, salvo los de autenticación, requieren un token JWT válido en el header `Authorization: Bearer <token>`.

### Autenticación

| Método | Endpoint | Descripción | Acceso |
|--------|----------|-------------|--------|
| POST | `/api/auth/admin/register` | Registrar un nuevo usuario administrador. | Público |
| POST | `/api/auth/admin/login` | Login de administrador. Devuelve un JWT. | Público |
| POST | `/api/auth/patient/login` | Login de paciente. Si no existe, lo registra automáticamente. | Público |

**Body para admin register y login:**
```json
{
  "email": "admin@sistema.com",
  "password": "string"
}
```

La contraseña debe tener al menos 6 caracteres, una mayúscula, una minúscula y un número.

**Body para patient login:**
```json
{
  "email": "string",
  "dni": 12345678
}
```

---

### Especialidades

Todos los endpoints requieren rol `ADMINISTRADOR`.

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/specialties?pageSize=10&pageIndex=1&name=string` | Lista paginada de especialidades activas. |
| GET | `/api/specialties/{id}` | Detalle de una especialidad por ID. |
| POST | `/api/specialties` | Crear una nueva especialidad. |
| PUT | `/api/specialties/{id}` | Actualizar una especialidad existente. |
| DELETE | `/api/specialties/{id}` | Eliminación lógica de una especialidad. |

**Body para POST/PUT:**
```json
{
  "name": "string",
  "description": "string"
}
```

---

### Médicos

Todos los endpoints requieren rol `ADMINISTRADOR`.

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/doctors?pageSize=10&pageIndex=1&name=string` | Lista paginada de médicos activos con su especialidad. |
| GET | `/api/doctors/{id}` | Detalle de un médico por ID. |
| GET | `/api/doctors/{id}/availabilities` | Disponibilidad horaria mensual del médico. |
| POST | `/api/doctors` | Registrar un nuevo médico. |
| PUT | `/api/doctors/{id}` | Actualizar los datos de un médico. |
| DELETE | `/api/doctors/{id}` | Eliminación lógica de un médico. |

**Body para POST/PUT:**
```json
{
  "name": "string",
  "licenseNumber": "string",
  "specialityId": "guid"
}
```

---

### Disponibilidades

Todos los endpoints requieren rol `ADMINISTRADOR`.

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/availabilities` | Configura la disponibilidad mensual de un médico. Genera slots de 30 minutos automáticamente, excluyendo feriados. |
| PUT | `/api/availabilities` | Sobreescribe la disponibilidad del mes actual para un médico, preservando los slots ya reservados. |

**Body para POST/PUT:**
```json
{
  "doctorId": "guid",
  "days": [
    {
      "day": "LUNES",
      "startTime": "09:00",
      "endTime": "12:00"
    }
  ]
}
```

Los valores válidos para `day` son: `LUNES`, `MARTES`, `MIÉRCOLES`, `JUEVES`, `VIERNES`, `SÁBADO`, `DOMINGO`.

---

### Turnos (Appointments)

| Método | Endpoint | Descripción | Acceso |
|--------|----------|-------------|--------|
| POST | `/api/appointments` | Reservar un turno disponible. | PACIENTE |
| DELETE | `/api/appointments/{id}` | Cancelar un turno reservado. | PACIENTE |
| GET | `/api/appointments/patient?dni=number` | Ver turnos activos de un paciente por DNI. | PACIENTE o ADMINISTRADOR |
| GET | `/api/appointments?date=YYYY-MM-DD&pageSize=10&pageIndex=1` | Listar todos los turnos de un día específico, paginado. | ADMINISTRADOR |
| GET | `/api/appointments/search` | Búsqueda avanzada y paginada de turnos con filtros. | ADMINISTRADOR |

**Body para POST (reservar turno):**
```json
{
  "doctorId": "guid",
  "availabilityId": "guid",
  "patient": {
    "dni": "12345678"
  },
  "reason": "Consulta general"
}
```

**Query params para búsqueda avanzada (`/api/appointments/search`):**
- `pageSize`, `pageIndex`
- `doctorId` (opcional)
- `specialityId` (opcional)
- `dateFrom` (opcional, formato `YYYY-MM-DD`)
- `dateTo` (opcional, formato `YYYY-MM-DD`)
- `status` (opcional: `BOOKED`, `CANCELLED`, `ATTENDED`, `NO_SHOW`)

---

## Manejo de errores

Todos los errores devuelven el siguiente formato:

```json
{
  "errorCode": "CODIGO_ERROR",
  "message": "Descripción del error",
  "details": [
    {
      "field": "campo",
      "issue": "descripción del problema"
    }
  ]
}
```

Los códigos de estado HTTP utilizados son: `200`, `201`, `204`, `400`, `401`, `403`, `404`, `409`, `429`, `500`.

---

## Rate Limiting

Los endpoints de autenticación y reserva de turnos tienen límites de solicitudes configurables desde `appsettings.json`:

- `POST /api/auth/admin/login`: máximo 5 solicitudes por minuto por IP.
- `POST /api/auth/patient/login`: máximo 10 solicitudes por minuto por IP.
- `POST /api/appointments`: máximo 5 solicitudes por minuto por usuario autenticado.
- Resto de endpoints: máximo 100 solicitudes por minuto por usuario o IP.

Las solicitudes que excedan el límite devuelven HTTP `429 Too Many Requests`.
