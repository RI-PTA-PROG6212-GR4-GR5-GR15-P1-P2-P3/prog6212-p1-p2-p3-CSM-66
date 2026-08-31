 --RaceDay Database Schema
-- Part 1 - Section C: SQL Database Script
-- Run this in SSMS on a clean SQL Server instance
-- I Added Distance and EventType  to Events after cross-checking Part 2 functional requirements

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- 1. ROLES
CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
GO
-- 2. USERS
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    RoleID INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
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
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    Distance DECIMAL(6,2) NOT NULL DEFAULT 0,
    EventType NVARCHAR(20) NOT NULL DEFAULT 'Run',
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserID) REFERENCES Users(UserID)
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
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantID) REFERENCES Users(UserID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Enrolment UNIQUE (ParticipantID, CategoryID)
);
GO
-- EnrolmentID is UNIQUE below, enforcing a one-to-one relationship
-- between Enrolments and Results (one result per enrolment).
-- 6. RESULTS
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    Position INT NULL,
    CapturedByUserID INT NOT NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID),
    CONSTRAINT FK_Results_Users FOREIGN KEY (CapturedByUserID) REFERENCES Users(UserID)
);
GO
