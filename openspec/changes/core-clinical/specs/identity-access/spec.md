# Identity & Access Specification

## Purpose

Authentication and role-based authorization for MediCore HMS. Roles: Admin, Receptionist, Professional. Patient role deferred to the portal module.

## Requirements

### Requirement: JWT authentication

The system MUST authenticate users via ASP.NET Core Identity and issue a short-lived JWT access token with role claims, plus a refresh token in an httpOnly SameSite cookie.

#### Scenario: Successful login
- GIVEN a registered user with valid credentials
- WHEN the user logs in
- THEN an access token with role claims is issued
- AND a refresh token cookie is set

#### Scenario: Failed login
- GIVEN invalid credentials
- WHEN the user logs in
- THEN authentication fails with an unauthorized response

### Requirement: Role-based access per endpoint

The system MUST enforce role authorization on every endpoint. Roles are Admin, Receptionist, Professional.

| Endpoint area | Admin | Receptionist | Professional |
|---------------|-------|--------------|--------------|
| Users, professionals, catalog, config | ✅ | — | — |
| Patients, booking, reschedule, cancel, sobreturnos | ✅ | ✅ | — |
| Own agenda view, mark own Completed/NoShow | ✅ | ✅ | ✅ (own only) |

#### Scenario: Authorized role allowed
- GIVEN a Receptionist with a valid token
- WHEN the Receptionist calls the booking endpoint
- THEN the request is authorized

#### Scenario: Unauthorized role forbidden
- GIVEN a Professional with a valid token
- WHEN the Professional calls the booking endpoint
- THEN the request is rejected with 403

#### Scenario: Anonymous rejected
- GIVEN a request with no valid token
- WHEN it calls a protected endpoint
- THEN the request is rejected with 401

### Requirement: Role catalog

The system MUST support the roles Admin, Receptionist, and Professional. The Patient role SHALL be deferred to the portal module and MUST NOT gate current endpoints.

#### Scenario: Roles seeded
- GIVEN a fresh database
- WHEN the role seed runs
- THEN Admin, Receptionist, and Professional roles exist
