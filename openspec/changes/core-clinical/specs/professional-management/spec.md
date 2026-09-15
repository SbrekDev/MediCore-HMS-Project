# Professional Management Specification

## Purpose

Register healthcare professionals, their licenses, and their assigned specialties. A professional's specialties determine which appointments they can receive.

## Requirements

### Requirement: Professional registration with license

The system MUST record, for every professional: first name, last name, one national license (MN), and a list of zero or more provincial licenses (MP), each MP carrying a number and province.

#### Scenario: Register a professional with MN and MP
- GIVEN an admin provides names, an MN, and two MP licenses
- WHEN the professional is registered
- THEN the professional is persisted with the MN and both MPs

#### Scenario: MN required
- GIVEN a professional without an MN
- WHEN registration is submitted
- THEN registration is rejected (MN is mandatory)

### Requirement: Specialty assignment

The system MUST allow a professional to hold multiple specialties through an assignment that carries a primary-specialty flag.

#### Scenario: Assign multiple specialties
- GIVEN an existing professional and catalog specialties Cardiología and Clínica Médica
- WHEN the admin assigns both specialties and marks one as primary
- THEN both assignments are recorded with the primary flag on the chosen one

### Requirement: Per-professional-specialty duration override

The system SHALL default a professional's consultation duration for a specialty to the specialty's default duration, and MUST allow an override on the professional-specialty assignment.

#### Scenario: Default duration inherited
- GIVEN a specialty has default duration 30 minutes
- WHEN a professional is assigned that specialty without an override
- THEN the professional's consultation duration for it is 30 minutes

#### Scenario: Override applied
- GIVEN a professional-specialty assignment
- WHEN an override of 45 minutes is set
- THEN bookings for that professional-specialty use 45 minutes
