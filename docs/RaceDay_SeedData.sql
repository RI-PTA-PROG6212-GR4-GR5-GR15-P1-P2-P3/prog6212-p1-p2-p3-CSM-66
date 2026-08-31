-- RaceDay Sample Data
-- Part 1 - Section C: Seed Data
-- Run this AFTER the schema script, on RaceDayDB

USE RaceDayDB;
GO

-- ROLES
INSERT INTO Roles (RoleName) VALUES
('Organiser'),
('Participant');
GO

-- USERS (2 Organisers, 2 Participants)
-- Note: PasswordHash values below are placeholders only,
-- not real hashes 
INSERT INTO Users (Name, Email, PasswordHash, RoleID, CreatedAt) VALUES
('Johan Pretorius', 'johan@raceday.co.za', 'HashedPassword123', 1, GETDATE()),   -- UserID 1, Organiser
('Lindiwe Khumalo', 'lindiwe@raceday.co.za', 'HashedPassword456', 1, GETDATE()), -- UserID 2, Organiser
('Sipho Ndlovu', 'sipho@example.com', 'HashedPassword789', 2, GETDATE()),        -- UserID 3, Participant
('Anrich Botha', 'anrich@example.com', 'HashedPassword321', 2, GETDATE());       -- UserID 4, Participant
GO

-- EVENTS (3 events, run by the 2 organisers)
INSERT INTO Events (OrganiserID, Name, Description, EventDate, Location, CreatedAt, Distance, EventType) VALUES
(1, 'Pretoria Park Run Challenge', 'Community 5km/10km road running event through Pretoria park routes.', '2026-09-12', 'Pretoria, Gauteng', GETDATE(), 10.00, 'Run'),
(1, 'Polokwane City Cycle Tour', 'Road cycling event through Polokwane city and surrounds.', '2026-10-03', 'Polokwane, Limpopo', GETDATE(), 80.00, 'Cycle'),
(2, 'Soweto Heritage Marathon', 'Annual road marathon celebrating Soweto heritage routes.', '2026-11-15', 'Soweto, Gauteng', GETDATE(), 42.20, 'Run');
GO

-- CATEGORIES (per event)
INSERT INTO Categories (EventID, Name, Distance, Price) VALUES
(1, '5km Fun Run', 5.00, 80.00),
(1, '10km Challenge', 10.00, 120.00),
(2, '40km Road Cycle', 40.00, 200.00),
(2, '80km Road Cycle', 80.00, 300.00),
(3, '21km Half Marathon', 21.10, 250.00),
(3, '42km Full Marathon', 42.20, 350.00);
GO

-- ENROLMENTS (participants entering categories)
INSERT INTO Enrolments (ParticipantID, CategoryID, EnrolmentDate, Status) VALUES
(3, 2, GETDATE(), 'Registered'),  -- Sipho -> 10km Challenge
(4, 1, GETDATE(), 'Registered'),  -- Anrich -> 5km Fun Run
(3, 5, GETDATE(), 'Registered'),  -- Sipho -> 21km Half Marathon
(4, 3, GETDATE(), 'Registered');  -- Anrich -> 40km Road Cycle
GO

-- RESULTS (captured by organisers, one per enrolment)
INSERT INTO Results (EnrolmentID, FinishTime, Position, CapturedByUserID) VALUES
(1, '00:52:30', 4, 1),   -- Sipho's 10km result, captured by Johan
(2, '00:28:15', 2, 1),   -- Anrich's 5km result, captured by Johan
(3, '01:55:40', 10, 2);  -- Sipho's 21km result, captured by Lindiwe
-- Note: Enrolment 4 (Anrich, 40km cycle) has no result yet simulates
-- an event that hasn't happened/been captured yet.
GO
