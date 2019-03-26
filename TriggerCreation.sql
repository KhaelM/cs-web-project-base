CREATE TRIGGER [dbo].[person_trg]
ON [dbo].[Test]
INSTEAD OF INSERT
AS
BEGIN
	SET NOCOUNT OFF;

	DECLARE @Id NCHAR(10), @Mort NCHAR(10), @Vivant BIT, @Somme DATETIME
	
	SELECT @Mort = inserted.Mort,
			@Vivant = inserted.Vivant,
			@Somme = inserted.Somme,
			@Id = 'Test' + CAST(NEXT VALUE FOR person_seq AS varchar) 
	FROM inserted

	INSERT INTO [dbo].[Test] 
	VALUES (@Id, @Mort,@Vivant, @Somme)
END

DROP TRIGGER [dbo].[person_trg]