CREATE DATABASE IF NOT EXISTS userdb
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE userdb;

CREATE TABLE IF NOT EXISTS UserInfo (
    UserId CHAR(36) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(256) NOT NULL,
    PRIMARY KEY (UserId),
    UNIQUE KEY UQ_UserInfo_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO UserInfo (UserId, FirstName, LastName, Email) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Alice', 'Anderson', 'alice.anderson@example.com'),
    ('22222222-2222-2222-2222-222222222222', 'Bob', 'Brown', 'bob.brown@example.com'),
    ('33333333-3333-3333-3333-333333333333', 'Carol', 'Clark', 'carol.clark@example.com'),
    ('44444444-4444-4444-4444-444444444444', 'David', 'Davis', 'david.davis@example.com'),
    ('55555555-5555-5555-5555-555555555555', 'Eve', 'Evans', 'eve.evans@example.com');
