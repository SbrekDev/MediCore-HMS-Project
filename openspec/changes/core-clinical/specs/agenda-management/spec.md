# Agenda Management Specification

## Purpose

Define when a professional is bookable: a weekly availability template plus per-date exceptions, and a configurable daily sobreturno cap per agenda.

## Requirements

### Requirement: Weekly availability template

The system MUST allow a weekly template per professional, composed of availability blocks (day of week, start time, end time). Bookings are only allowed within these blocks.

#### Scenario: Define weekly template
- GIVEN a professional
- WHEN an admin defines blocks (e.g. Mon–Fri 08:00–12:00 and 14:00–18:00)
- THEN the professional is bookable within those windows on the corresponding weekdays

#### Scenario: Booking outside template rejected
- GIVEN a professional has no block covering a requested time
- WHEN a receptionist attempts to book that time
- THEN the booking is rejected

### Requirement: Per-date exceptions

The system MUST support per-date exceptions that override the weekly template for a specific date (holiday, vacation, leave).

#### Scenario: Full-day exception
- GIVEN a professional normally works Monday
- WHEN an admin marks a specific Monday as a holiday (full-day exception)
- THEN the professional is not bookable that date

#### Scenario: Partial-day exception
- GIVEN a professional normally works 08:00–18:00
- WHEN an admin sets a partial exception of 08:00–12:00 for a date
- THEN only the remaining hours are bookable that date

### Requirement: Sobreturno daily cap

The system MUST allow a configurable daily sobreturno cap per agenda, and SHALL enforce it during scheduling.

#### Scenario: Cap configured and enforced
- GIVEN an agenda with a daily sobreturno cap of 2
- WHEN two sobreturnos are already booked for that day
- THEN a third sobreturno for that day is rejected
