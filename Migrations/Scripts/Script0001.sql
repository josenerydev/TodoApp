create table TodoItem
(
    Id         bigint primary key identity (1,1),
    Name       nvarchar(50) not null,
    IsComplete bit          not null,
    Secret     nvarchar(50) not null
);