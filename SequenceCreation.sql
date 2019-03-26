CREATE SEQUENCE person_seq
	AS BIGINT
	START WITH 1
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 99999
	NO CYCLE
	CACHE 50;

-- cache is used for performance reason so mssql doesn't store always nextval in db but use two variable
-- no cycle means it doesn't increment anymore after 99999