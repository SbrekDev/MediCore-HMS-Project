# Patient Management Specification

## Purpose

Register and maintain patient identity, contact, address, and coverage information for MediCore HMS. Patients are the subjects of appointments; minors require a recorded responsible.

## Requirements

### Requirement: Patient registration

The system MUST capture, for every patient: a document type and number, first name, last name, and birth date. Contact (mobile/landline phone, email) and address (street, number, city, province, postal code) SHALL be captured as provided. DocumentType is one of DNI, Pasaporte, DNIExtranjero, LC/LE.

#### Scenario: Register an adult patient
- GIVEN a receptionist provides a valid document (DNI) with number, names, and birth date
- WHEN the receptionist registers the patient
- THEN the patient is persisted with a unique identifier
- AND an audit entry records the creation

#### Scenario: Register a patient with a non-DNI document
- GIVEN a patient holds a passport instead of a DNI
- WHEN the receptionist registers the patient with DocumentType=Pasaporte
- THEN the patient is accepted (identity is not DNI-only)

### Requirement: Unique document identity

The system MUST enforce uniqueness on the composite (DocumentType, DocumentNumber). Two patients SHALL NOT share the same document type AND number.

#### Scenario: Duplicate document rejected
- GIVEN a patient already exists with DocumentType=DNI and DocumentNumber=30111222
- WHEN a receptionist registers another patient with the same DNI 30111222
- THEN registration is rejected with a conflict error

#### Scenario: Same number, different type allowed
- GIVEN a patient exists with DocumentType=DNI and DocumentNumber=30111222
- WHEN a receptionist registers a patient with DocumentType=Pasaporte and DocumentNumber=30111222
- THEN the patient is accepted (distinct identity)

### Requirement: Coverage history

The system MUST store a patient's coverage history, each entry having a type (ObraSocial, Prepaga, or Particular), insurer name, plan name, and affiliate number. Exactly one entry is active at a time; "Particular" is self-pay with no insurer.

#### Scenario: Add active coverage
- GIVEN a patient has no coverage
- WHEN a receptionist adds an ObraSocial coverage with insurer and affiliate number
- THEN the coverage becomes the patient's active coverage

#### Scenario: Particular (self-pay) coverage
- GIVEN a patient chooses to self-pay
- WHEN a receptionist records coverage of type Particular
- THEN no insurer or affiliate number is required

### Requirement: Minor requires responsible

The system MUST require a recorded responsible (guardian/legal representative) for any patient under 18 years of age, derived from birth date. Adults SHALL NOT require a responsible.

#### Scenario: Minor without responsible rejected
- GIVEN a patient is under 18 at registration
- WHEN the receptionist submits registration without a responsible
- THEN registration is rejected

#### Scenario: Minor with responsible accepted
- GIVEN a patient under 18 and an existing responsible patient record
- WHEN the receptionist registers the minor linking the responsible
- THEN registration succeeds and the responsible is recorded

#### Scenario: Adult without responsible accepted
- GIVEN a patient is 18 or older
- WHEN the receptionist registers the patient without a responsible
- THEN registration succeeds

### Requirement: Patient search

The system MUST allow searching patients by document number and by name.

#### Scenario: Search by document
- GIVEN a patient with a known document number
- WHEN a user searches by that document number
- THEN the matching patient is returned
