/* sp_help_revlogin script
** Generated nov 27 2024  9:32AM on RTX4070Ti\SQLEXPRESS
*/
 
/* Login [AUTORITE NT\Système] */
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE [name] = N'AUTORITE NT\Système'
    )
BEGIN
    CREATE LOGIN [AUTORITE NT\Système]
    FROM WINDOWS
    WITH DEFAULT_DATABASE = [master]
        ,DEFAULT_LANGUAGE = [Français]
END
 
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
 
/* Login [BUILTIN\Utilisateurs] */
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE [name] = N'BUILTIN\Utilisateurs'
    )
BEGIN
    CREATE LOGIN [BUILTIN\Utilisateurs]
    FROM WINDOWS
    WITH DEFAULT_DATABASE = [master]
        ,DEFAULT_LANGUAGE = [Français]
END
 
/* Login [NT Service\MSSQL$SQLEXPRESS] */
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE [name] = N'NT Service\MSSQL$SQLEXPRESS'
    )
BEGIN
    CREATE LOGIN [NT Service\MSSQL$SQLEXPRESS]
    FROM WINDOWS
    WITH DEFAULT_DATABASE = [master]
        ,DEFAULT_LANGUAGE = [Français]

	
	EXEC [master].dbo.sp_addsrvrolemember @loginame = N'NT Service\MSSQL$SQLEXPRESS', @rolename = N'sysadmin'
END
 
/* Login [NT SERVICE\SQLTELEMETRY$SQLEXPRESS] */
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE [name] = N'NT SERVICE\SQLTELEMETRY$SQLEXPRESS'
    )
BEGIN
    CREATE LOGIN [NT SERVICE\SQLTELEMETRY$SQLEXPRESS]
    FROM WINDOWS
    WITH DEFAULT_DATABASE = [master]
        ,DEFAULT_LANGUAGE = [Français]
END
 
/* Login [NT SERVICE\SQLWriter] */
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE [name] = N'NT SERVICE\SQLWriter'
    )
BEGIN
    CREATE LOGIN [NT SERVICE\SQLWriter]
    FROM WINDOWS
    WITH DEFAULT_DATABASE = [master]
        ,DEFAULT_LANGUAGE = [Français]

	
	EXEC [master].dbo.sp_addsrvrolemember @loginame = N'NT SERVICE\SQLWriter', @rolename = N'sysadmin'
END
 
/* Login [NT SERVICE\Winmgmt] */
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE [name] = N'NT SERVICE\Winmgmt'
    )
BEGIN
    CREATE LOGIN [NT SERVICE\Winmgmt]
    FROM WINDOWS
    WITH DEFAULT_DATABASE = [master]
        ,DEFAULT_LANGUAGE = [Français]

	
	EXEC [master].dbo.sp_addsrvrolemember @loginame = N'NT SERVICE\Winmgmt', @rolename = N'sysadmin'
END
 
/* Login [RTX4070Ti\antoi] */
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE [name] = N'RTX4070Ti\antoi'
    )
BEGIN
    CREATE LOGIN [RTX4070Ti\antoi]
    FROM WINDOWS
    WITH DEFAULT_DATABASE = [master]
        ,DEFAULT_LANGUAGE = [Français]

	
	EXEC [master].dbo.sp_addsrvrolemember @loginame = N'RTX4070Ti\antoi', @rolename = N'sysadmin'
END