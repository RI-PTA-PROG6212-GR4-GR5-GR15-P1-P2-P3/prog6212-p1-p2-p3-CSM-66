-- RaceDay Sample Data 
-- Part 1 - Section C: Seed Data
USE RaceDayDB;
GO

-- ORGANISERS (2 required)
-- Note: PasswordHash values below are placeholders only, not real hashes.
INSERT INTO Organisers (Name, Email, PasswordHash, CreatedAt) VALUES
('Johan Pretorius', 'johan@raceday.co.za', 'HashedPassword123', GETDATE()),   -- OrganiserID 1
('Lindiwe Khumalo', 'lindiwe@raceday.co.za', 'HashedPassword456', GETDATE()); -- OrganiserID 2
GO

-- PARTICIPANTS (2 required)
INSERT INTO Participants (Name, Email, PasswordHash, CreatedAt) VALUES
('Choeu Molepo', 'choeu@gmail.com', 'HashedPassword789', GETDATE()),  -- ParticipantID 1
('Anrich Botha', 'anrich@gmail.com', 'HashedPassword321', GETDATE()); -- ParticipantID 2
GO

-- EVENTS (3 required, run by the 2 organisers)
INSERT INTO Events (OrganiserID, Name, Description, EventDate, Location, Distance, EventType) VALUES
(1, 'Pretoria Park Run Challenge', 'Community 5km/10km road running event through Pretoria park routes.', '2026-09-12', 'Pretoria, Gauteng', 10.00, 'Run'),
(1, 'Polokwane City Cycle Tour', 'Road cycling event through Polokwane city and surrounds.', '2026-10-03', 'Polokwane, Limpopo', 80.00, 'Cycle'),
(2, 'Soweto Heritage Marathon', 'Annual road marathon celebrating Soweto heritage routes.', '2026-11-15', 'Soweto, Gauteng', 42.20, 'Run');
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
(1, 2, GETDATE(), 'Registered'),  -- Choeu -> 10km Challenge
(2, 1, GETDATE(), 'Registered'),  -- Anrich -> 5km Fun Run
(1, 5, GETDATE(), 'Registered'),  -- Choeu -> 21km Half Marathon
(2, 3, GETDATE(), 'Registered');  -- Anrich -> 40km Road Cycle
GO

-- RESULTS (captured by organisers, one per enrolment)
INSERT INTO Results (EnrolmentID, FinishTime, Position, CapturedByOrganiserID) VALUES
(1, '00:52:30', 4, 1),   -- Choeu's 10km result, captured by Johan
(2, '00:28:15', 2, 1),   -- Anrich's 5km result, captured by Johan
(3, '01:55:40', 10, 2);  -- Choeu's 21km result, captured by Lindiwe
-- Note: Enrolment 4 (Anrich, 40km cycle) intentionally has no matching
-- row in Results, simulating an event whose results have not yet been
-- captured by the organiser.
GO

