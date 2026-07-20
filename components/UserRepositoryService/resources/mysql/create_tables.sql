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
