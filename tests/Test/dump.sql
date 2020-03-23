CREATE DATABASE garden;
    
USE garden;

CREATE TABLE pet (id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20), owner VARCHAR(20), species VARCHAR(20), sex CHAR(1), birth DATE, death DATE, timeUpdated datetime);