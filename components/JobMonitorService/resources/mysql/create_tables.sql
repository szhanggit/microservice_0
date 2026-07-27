-- Assumes the userdb database already exists (see UserRepositoryService's
-- create_tables.sql, which creates it) - this file only adds JobMonitor.
USE userdb;

CREATE TABLE IF NOT EXISTS JobMonitor (
    Id CHAR(36) NOT NULL,
    JobStartDateTime DATETIME(3) NOT NULL,
    JobEndDateTime DATETIME(3) NULL,
    Status ENUM('Running', 'Succeeded', 'Failed') NOT NULL DEFAULT 'Running',
    PRIMARY KEY (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
