# Specialty Catalog Specification

## Purpose

Provide the seeded catalog of medical specialties with default consultation durations used by scheduling.

## Requirements

### Requirement: Seeded catalog

The system MUST seed a catalog of specialties, each with a code, name, and default consultation duration. The default duration SHALL be 30 minutes unless overridden per specialty.

#### Scenario: Catalog is seeded
- GIVEN a fresh database
- WHEN the catalog seed runs
- THEN the reference specialties (e.g. Cardiología, Clínica Médica, Pediatría) exist with a 30-minute default duration

#### Scenario: Seed is idempotent
- GIVEN the catalog is already seeded
- WHEN the seed runs again
- THEN no duplicate specialties are created

### Requirement: Active flag

The system MUST allow a specialty to be marked inactive; inactive specialties SHALL NOT be offered for new appointments.

#### Scenario: Inactive specialty excluded
- GIVEN a specialty is marked inactive
- WHEN a receptionist books an appointment
- THEN that specialty is not available for selection
