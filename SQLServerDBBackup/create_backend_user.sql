/* sp_help_revlogin script
** Generated nov 27 2024  9:32AM on RTX4070Ti\SQLEXPRESS
*/
 
/* Login [backend_user] */
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE [name] = N'backend_user'
    )
BEGIN
	CREATE LOGIN [backend_user]
    WITH PASSWORD = 0x020022B95F0B40D8E2DF91EF7ABE25CC52E73E81F3B2A2FED1FC5D773DE229F752BAB731B3202A28C7331BE4AB506F987D30EEE40CE1ABB88F42D809C792ADB05DB0070F3571 HASHED
        ,SID = 0xE7122D0578790242839CCEC8911D9C38
        ,DEFAULT_DATABASE = [PatientDb]
        ,DEFAULT_LANGUAGE = [Français]
        ,CHECK_POLICY = ON
        ,CHECK_EXPIRATION = ON

	
	EXEC [master].dbo.sp_addsrvrolemember @loginame = N'backend_user', @rolename = N'sysadmin'
END
