# Appointment Scheduling Specification

## Purpose

The appointment lifecycle: booking, rescheduling, cancellation (including late), completion, and no-show, with overlap, window, specialty-validity, and sobreturno rules.

## Requirements

### Requirement: Booking by reception staff

The system MUST allow booking appointments for authorized staff (Receptionist/Admin) only. A new appointment SHALL be born in the Confirmed state. States are Confirmed, Cancelled, Completed, NoShow.

#### Scenario: Receptionist books a confirmed appointment
- GIVEN a receptionist selects patient, professional, specialty, and a valid start time
- WHEN the receptionist books the appointment
- THEN the appointment is created with status Confirmed

### Requirement: No professional double-booking

The system MUST NOT allow a professional to hold two appointments whose time ranges overlap.

#### Scenario: Overlapping professional appointment rejected
- GIVEN a professional already has an appointment 10:00–10:30
- WHEN a receptionist books the same professional at 10:15
- THEN the booking is rejected

#### Scenario: Adjacent appointments allowed
- GIVEN a professional has an appointment 10:00–10:30
- WHEN a receptionist books the same professional at 10:30
- THEN the booking succeeds (no overlap)

#### Scenario: Concurrent double-booking prevented
- GIVEN two receptionists attempt to book the same professional's slot simultaneously
- WHEN both requests commit
- THEN exactly one succeeds and the other is rejected

### Requirement: No patient overlap

The system MUST NOT allow a patient to hold two appointments at the same time, across any professional.

#### Scenario: Patient overlap rejected
- GIVEN a patient already has an appointment at 10:00
- WHEN a receptionist books that patient at 10:10 with a different professional
- THEN the booking is rejected

### Requirement: Specialty validity

The system MUST reject an appointment whose specialty is not held by the chosen professional.

#### Scenario: Unheld specialty rejected
- GIVEN a professional holds only Cardiología
- WHEN a receptionist books that professional for Pediatría
- THEN the booking is rejected

### Requirement: Duration resolution

The system SHALL use the professional-specialty duration override when present, otherwise the specialty default.

#### Scenario: Duration from specialty default
- GIVEN a specialty default of 30 minutes and no override
- WHEN an appointment is booked
- THEN its duration is 30 minutes

#### Scenario: Duration from override
- GIVEN a professional-specialty override of 45 minutes
- WHEN an appointment is booked
- THEN its duration is 45 minutes

### Requirement: Booking window

The system MUST reject a booking whose start is less than 2 hours from now or more than 90 days ahead.

#### Scenario: Too soon rejected
- GIVEN a requested start 1 hour from now
- WHEN a receptionist books
- THEN the booking is rejected

#### Scenario: Too far rejected
- GIVEN a requested start 91 days ahead
- WHEN a receptionist books
- THEN the booking is rejected

#### Scenario: Within window accepted
- GIVEN a requested start 3 days ahead
- WHEN a receptionist books
- THEN the booking succeeds

### Requirement: Sobreturno

The system MUST allow flagged sobreturno appointments only for Receptionist/Admin, only beyond normal block capacity, and within the agenda's daily sobreturno cap.

#### Scenario: Sobreturno by authorized role
- GIVEN a professional's agenda is full for a day and the sobreturno cap is not reached
- WHEN a Receptionist books a sobreturno appointment
- THEN the appointment is created with IsSobreturno=true

#### Scenario: Sobreturno by unauthorized role rejected
- GIVEN a Professional (non-receptionist) attempts a sobreturno
- WHEN the booking is submitted
- THEN it is rejected

#### Scenario: Sobreturno cap exceeded rejected
- GIVEN the daily sobreturno cap is reached
- WHEN a Receptionist books another sobreturno
- THEN it is rejected

### Requirement: Reschedule

The system MUST allow rescheduling an appointment, revalidating window, overlap, and specialty rules against the new time.

#### Scenario: Reschedule to free slot
- GIVEN a Confirmed appointment
- WHEN a receptionist reschedules it to a free, valid slot
- THEN the appointment's start time updates and status stays Confirmed

### Requirement: Cancellation and late cancellation

The system MUST allow cancellation up to 24 hours before start. A cancellation within 24 hours SHALL be recorded as a late cancellation.

#### Scenario: Normal cancellation
- GIVEN an appointment more than 24 hours away
- WHEN a receptionist cancels it
- THEN status becomes Cancelled

#### Scenario: Late cancellation recorded
- GIVEN an appointment less than 24 hours away
- WHEN a receptionist cancels it
- THEN status becomes Cancelled and the cancellation is flagged as late

### Requirement: Completion and no-show

The system MUST allow a professional to mark only their own appointments as Completed or NoShow. Marking NoShow SHALL increment the patient's no-show counter.

#### Scenario: Professional completes own appointment
- GIVEN a professional with a Confirmed appointment
- WHEN the professional marks it Completed
- THEN status becomes Completed

#### Scenario: Professional marks another's appointment
- GIVEN a professional attempts to mark another professional's appointment
- WHEN the request is submitted
- THEN it is rejected

#### Scenario: No-show increments counter
- GIVEN a Confirmed appointment
- WHEN the professional marks it NoShow
- THEN status becomes NoShow and the patient's no-show count increases
