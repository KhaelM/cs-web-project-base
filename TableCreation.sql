CREATE TABLE [dbo].[Test] (
    [Id]     NCHAR (10) NOT NULL,
    [Mort]   NCHAR (10) NULL,
    [Vivant] BIT        NULL,
    [Somme]  DATETIME   NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);