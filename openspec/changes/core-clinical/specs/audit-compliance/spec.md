# Audit & Compliance Specification

## Purpose

Record who changed what and when on clinical data, and capture patient consent as required by Ley 25.326 and Ley 26.529.

## Requirements

### Requirement: Audit trail

The system MUST record an audit entry — actor, action, target, and timestamp — for every mutation of patient, professional, agenda, and appointment records.

#### Scenario: Patient mutation audited
- GIVEN a receptionist registers a patient
- WHEN the patient is persisted
- THEN an audit entry records the actor, action (created), target, and timestamp

#### Scenario: Appointment mutation audited
- GIVEN a receptionist cancels an appointment
- WHEN the cancellation is persisted
- THEN an audit entry records the actor, action, target, and timestamp

### Requirement: Consent capture

The system MUST capture and record patient consent for handling health data, and SHALL mark a patient's record with consent status.

#### Scenario: Consent recorded at registration
- GIVEN a patient consents to data handling
- WHEN the receptionist registers the patient with consent
- THEN the consent status is recorded on the patient record

#### Scenario: Missing consent flagged
- GIVEN a patient record without consent
- WHEN the patient's record is viewed
- THEN the missing consent is surfaced as pending
