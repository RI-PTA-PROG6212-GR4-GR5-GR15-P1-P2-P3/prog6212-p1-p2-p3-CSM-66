-- RaceDay Database Schema
-- Part 1 - Section C: SQL Database Script
-- Organisers and Participants are separate tables so a person cannot
-- hold both roles, and public registration can only create Participants.
CREATE DATABASE RaceDayDB;
GO
USE RaceDayDB;
GO

-- 1. ORGANISERS
-- Organiser accounts are NOT created through public self-registration.
-- They are provisioned separately (e.g. by an Admin), matching the
-- Rosebank example: an Organiser cannot sign themselves up.
CREATE TABLE Organisers (
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- 2. PARTICIPANTS
-- Public self-registration only ever creates rows here.
CREATE TABLE Participants (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- 3. EVENTS
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL DEFAULT 0,
    EventType NVARCHAR(20) NOT NULL DEFAULT 'Run',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserID) REFERENCES Organisers(OrganiserID)
);
GO

-- 4. CATEGORIES
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    Price DECIMAL(8,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES Events(EventID)
);
GO

-- 5. ENROLMENTS
CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Registered',
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantID) REFERENCES Participants(ParticipantID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Enrolment UNIQUE (ParticipantID, CategoryID)
);
GO

-- 6. RESULTS
-- EnrolmentID is UNIQUE below, enforcing a one-to-one relationship
-- between Enrolments and Results (one result per enrolment).
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    Position INT NULL,
    CapturedByOrganiserID INT NOT NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID),
    CONSTRAINT FK_Results_Organisers FOREIGN KEY (CapturedByOrganiserID) REFERENCES Organisers(OrganiserID)
);
GO
