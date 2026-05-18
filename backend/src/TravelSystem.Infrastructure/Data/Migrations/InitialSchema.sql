-- ============================================================
-- TravelSystem — Initial Database Schema
-- MySQL 8.0+ / MariaDB 10.6+
-- Generated for Entity Framework Core (Pomelo provider)
-- Run: dotnet ef migrations add InitialCreate
--      dotnet ef database update
-- ============================================================

-- Roles
CREATE TABLE IF NOT EXISTS `Roles` (
    `Id`               CHAR(36)      NOT NULL,
    `Name`             VARCHAR(256),
    `NormalizedName`   VARCHAR(256),
    `ConcurrencyStamp` LONGTEXT,
    CONSTRAINT `PK_Roles` PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

-- Users
CREATE TABLE IF NOT EXISTS `Users` (
    `Id`                       CHAR(36)      NOT NULL,
    `FirstName`                VARCHAR(100)  NOT NULL,
    `LastName`                 VARCHAR(100)  NOT NULL,
    `PreferredLanguage`        VARCHAR(10)   NOT NULL DEFAULT 'pt-AO',
    `AvatarUrl`                VARCHAR(500),
    `IsActive`                 TINYINT(1)    NOT NULL DEFAULT 1,
    `CreatedAt`                DATETIME(6)   NOT NULL,
    `LastLoginAt`              DATETIME(6),
    `PasswordResetToken`       LONGTEXT,
    `PasswordResetTokenExpiry` DATETIME(6),
    `RefreshToken`             LONGTEXT,
    `RefreshTokenExpiry`       DATETIME(6),
    -- Identity columns
    `UserName`                 VARCHAR(256),
    `NormalizedUserName`       VARCHAR(256),
    `Email`                    VARCHAR(256),
    `NormalizedEmail`          VARCHAR(256),
    `EmailConfirmed`           TINYINT(1)    NOT NULL,
    `PasswordHash`             LONGTEXT,
    `SecurityStamp`            LONGTEXT,
    `ConcurrencyStamp`         LONGTEXT,
    `PhoneNumber`              LONGTEXT,
    `PhoneNumberConfirmed`     TINYINT(1)    NOT NULL,
    `TwoFactorEnabled`         TINYINT(1)    NOT NULL,
    `LockoutEnd`               DATETIME(6),
    `LockoutEnabled`           TINYINT(1)    NOT NULL,
    `AccessFailedCount`        INT           NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

CREATE UNIQUE INDEX `UserNameIndex`  ON `Users` (`NormalizedUserName`);
CREATE INDEX        `EmailIndex`     ON `Users` (`NormalizedEmail`);

-- UserRoles
CREATE TABLE IF NOT EXISTS `UserRoles` (
    `UserId` CHAR(36) NOT NULL,
    `RoleId` CHAR(36) NOT NULL,
    CONSTRAINT `PK_UserRoles` PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_UserRoles_Users` FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserRoles_Roles` FOREIGN KEY (`RoleId`) REFERENCES `Roles`(`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

-- Itineraries
CREATE TABLE IF NOT EXISTS `Itineraries` (
    `Id`           CHAR(36)        NOT NULL,
    `UserId`       CHAR(36)        NOT NULL,
    `Title`        VARCHAR(200)    NOT NULL,
    `Description`  LONGTEXT,
    `Destination`  VARCHAR(200)    NOT NULL,
    `CountryCode`  VARCHAR(3),
    `Latitude`     DOUBLE,
    `Longitude`    DOUBLE,
    `StartDate`    DATETIME(6)     NOT NULL,
    `EndDate`      DATETIME(6)     NOT NULL,
    `Budget`       DECIMAL(15,2),
    `CurrencyCode` VARCHAR(3),
    `Status`       INT             NOT NULL DEFAULT 0,
    `CreatedAt`    DATETIME(6)     NOT NULL,
    `UpdatedAt`    DATETIME(6)     NOT NULL,
    CONSTRAINT `PK_Itineraries` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Itineraries_Users` FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_Itineraries_UserId` ON `Itineraries` (`UserId`);

-- ItineraryStops
CREATE TABLE IF NOT EXISTS `ItineraryStops` (
    `Id`              CHAR(36)     NOT NULL,
    `ItineraryId`     CHAR(36)     NOT NULL,
    `Name`            VARCHAR(200) NOT NULL,
    `Address`         VARCHAR(500),
    `Latitude`        DOUBLE       NOT NULL,
    `Longitude`       DOUBLE       NOT NULL,
    `DayNumber`       INT          NOT NULL,
    `OrderIndex`      INT          NOT NULL,
    `Notes`           LONGTEXT,
    `Category`        INT          NOT NULL DEFAULT 0,
    `VisitTime`       DATETIME(6),
    `DurationMinutes` INT,
    CONSTRAINT `PK_ItineraryStops` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ItineraryStops_Itineraries` FOREIGN KEY (`ItineraryId`) REFERENCES `Itineraries`(`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_ItineraryStops_ItineraryId_Day_Order` ON `ItineraryStops` (`ItineraryId`, `DayNumber`, `OrderIndex`);

-- ItineraryAttractions
CREATE TABLE IF NOT EXISTS `ItineraryAttractions` (
    `Id`          CHAR(36)     NOT NULL,
    `ItineraryId` CHAR(36)     NOT NULL,
    `PlaceId`     VARCHAR(200) NOT NULL,
    `Name`        VARCHAR(200) NOT NULL,
    `Category`    VARCHAR(100),
    `Latitude`    DOUBLE       NOT NULL,
    `Longitude`   DOUBLE       NOT NULL,
    `ImageUrl`    VARCHAR(500),
    `Rating`      DOUBLE,
    `IsVisited`   TINYINT(1)   NOT NULL DEFAULT 0,
    CONSTRAINT `PK_ItineraryAttractions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ItineraryAttractions_Itineraries` FOREIGN KEY (`ItineraryId`) REFERENCES `Itineraries`(`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

-- ItineraryExpenses
CREATE TABLE IF NOT EXISTS `ItineraryExpenses` (
    `Id`           CHAR(36)     NOT NULL,
    `ItineraryId`  CHAR(36)     NOT NULL,
    `Category`     VARCHAR(100) NOT NULL,
    `Description`  LONGTEXT     NOT NULL,
    `Amount`       DECIMAL(15,2) NOT NULL,
    `CurrencyCode` VARCHAR(3),
    `Date`         DATETIME(6)  NOT NULL,
    CONSTRAINT `PK_ItineraryExpenses` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ItineraryExpenses_Itineraries` FOREIGN KEY (`ItineraryId`) REFERENCES `Itineraries`(`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

-- AiChatMessages
CREATE TABLE IF NOT EXISTS `AiChatMessages` (
    `Id`          CHAR(36)    NOT NULL,
    `ItineraryId` CHAR(36)    NOT NULL,
    `Role`        VARCHAR(20) NOT NULL,
    `Content`     LONGTEXT    NOT NULL,
    `CreatedAt`   DATETIME(6) NOT NULL,
    CONSTRAINT `PK_AiChatMessages` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AiChatMessages_Itineraries` FOREIGN KEY (`ItineraryId`) REFERENCES `Itineraries`(`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

-- Hotels
CREATE TABLE IF NOT EXISTS `Hotels` (
    `Id`          CHAR(36)     NOT NULL,
    `ExternalId`  VARCHAR(100),
    `Provider`    VARCHAR(50),
    `Name`        VARCHAR(300) NOT NULL,
    `Address`     VARCHAR(500) NOT NULL,
    `City`        VARCHAR(200) NOT NULL,
    `CountryCode` VARCHAR(3)   NOT NULL,
    `Latitude`    DOUBLE       NOT NULL,
    `Longitude`   DOUBLE       NOT NULL,
    `StarRating`  INT          NOT NULL DEFAULT 0,
    `GuestRating` DOUBLE,
    `ImageUrl`    VARCHAR(500),
    `Description` LONGTEXT,
    `Amenities`   LONGTEXT,
    `CachedAt`    DATETIME(6)  NOT NULL,
    CONSTRAINT `PK_Hotels` PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_Hotels_City` ON `Hotels` (`City`);

-- HotelRooms
CREATE TABLE IF NOT EXISTS `HotelRooms` (
    `Id`             CHAR(36)     NOT NULL,
    `HotelId`        CHAR(36)     NOT NULL,
    `RoomType`       VARCHAR(100) NOT NULL,
    `Description`    LONGTEXT,
    `MaxGuests`      INT          NOT NULL,
    `PricePerNight`  DECIMAL(10,2) NOT NULL,
    `CurrencyCode`   VARCHAR(3),
    `IsAvailable`    TINYINT(1)   NOT NULL DEFAULT 1,
    CONSTRAINT `PK_HotelRooms` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_HotelRooms_Hotels` FOREIGN KEY (`HotelId`) REFERENCES `Hotels`(`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

-- Flights
CREATE TABLE IF NOT EXISTS `Flights` (
    `Id`              CHAR(36)     NOT NULL,
    `ExternalId`      VARCHAR(100) NOT NULL,
    `Provider`        VARCHAR(50)  NOT NULL,
    `Airline`         VARCHAR(200) NOT NULL,
    `FlightNumber`    VARCHAR(20)  NOT NULL,
    `OriginCode`      VARCHAR(3)   NOT NULL,
    `DestinationCode` VARCHAR(3)   NOT NULL,
    `OriginCity`      VARCHAR(200) NOT NULL,
    `DestinationCity` VARCHAR(200) NOT NULL,
    `DepartureAt`     DATETIME(6)  NOT NULL,
    `ArrivalAt`       DATETIME(6)  NOT NULL,
    `DurationMinutes` INT          NOT NULL,
    `Stops`           INT          NOT NULL DEFAULT 0,
    `CabinClass`      VARCHAR(20)  NOT NULL DEFAULT 'economy',
    `Price`           DECIMAL(10,2) NOT NULL,
    `CurrencyCode`    VARCHAR(3),
    `SeatsAvailable`  INT          NOT NULL,
    `CachedAt`        DATETIME(6)  NOT NULL,
    CONSTRAINT `PK_Flights` PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_Flights_Route_Date` ON `Flights` (`OriginCode`, `DestinationCode`, `DepartureAt`);

-- Bookings
CREATE TABLE IF NOT EXISTS `Bookings` (
    `Id`                 CHAR(36)      NOT NULL,
    `UserId`             CHAR(36)      NOT NULL,
    `HotelId`            CHAR(36),
    `HotelRoomId`        CHAR(36),
    `FlightId`           CHAR(36),
    `Type`               INT           NOT NULL,
    `Status`             INT           NOT NULL DEFAULT 0,
    `CheckIn`            DATETIME(6)   NOT NULL,
    `CheckOut`           DATETIME(6)   NOT NULL,
    `Guests`             INT           NOT NULL DEFAULT 1,
    `TotalPrice`         DECIMAL(10,2) NOT NULL,
    `CurrencyCode`       VARCHAR(3),
    `ConfirmationNumber` VARCHAR(100),
    `ProviderReference`  LONGTEXT,
    `Notes`              LONGTEXT,
    `CreatedAt`          DATETIME(6)   NOT NULL,
    `UpdatedAt`          DATETIME(6)   NOT NULL,
    CONSTRAINT `PK_Bookings` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Bookings_Users`   FOREIGN KEY (`UserId`)   REFERENCES `Users`(`Id`)   ON DELETE CASCADE,
    CONSTRAINT `FK_Bookings_Hotels`  FOREIGN KEY (`HotelId`)  REFERENCES `Hotels`(`Id`)  ON DELETE SET NULL,
    CONSTRAINT `FK_Bookings_Flights` FOREIGN KEY (`FlightId`) REFERENCES `Flights`(`Id`) ON DELETE SET NULL
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_Bookings_UserId` ON `Bookings` (`UserId`);

-- FlightAlerts
CREATE TABLE IF NOT EXISTS `FlightAlerts` (
    `Id`               CHAR(36)      NOT NULL,
    `UserId`           CHAR(36)      NOT NULL,
    `OriginCode`       VARCHAR(3)    NOT NULL,
    `DestinationCode`  VARCHAR(3)    NOT NULL,
    `DepartureFrom`    DATETIME(6),
    `DepartureTo`      DATETIME(6),
    `TargetPrice`      DECIMAL(10,2) NOT NULL,
    `CurrencyCode`     VARCHAR(3),
    `IsActive`         TINYINT(1)    NOT NULL DEFAULT 1,
    `LastTriggeredAt`  DATETIME(6),
    `CreatedAt`        DATETIME(6)   NOT NULL,
    CONSTRAINT `PK_FlightAlerts` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_FlightAlerts_Users` FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_FlightAlerts_UserId_Active` ON `FlightAlerts` (`UserId`, `IsActive`);

-- ============================================================
-- Seed: default roles
-- ============================================================
INSERT IGNORE INTO `Roles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES
    (UUID(), 'Admin',           'ADMIN',           UUID()),
    (UUID(), 'PremiumTraveler', 'PREMIUMTRAVELER',  UUID()),
    (UUID(), 'Traveler',        'TRAVELER',         UUID());
