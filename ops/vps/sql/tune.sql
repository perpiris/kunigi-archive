-- SQL Server settings for the Kunigi Archive instance. Safe to re-run.
-- Applied by ops/vps/setup.sh (or by hand with sqlcmd inside kunigi_db).

-- Nightly FULL backups only (no log backups), so FULL recovery just grows the log.
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'KunigiArchive' AND recovery_model_desc <> N'SIMPLE')
    ALTER DATABASE [KunigiArchive] SET RECOVERY SIMPLE;
GO

-- New databases inherit from model.
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'model' AND recovery_model_desc <> N'SIMPLE')
    ALTER DATABASE [model] SET RECOVERY SIMPLE;
GO

-- Cap the built-in system_health trace files at 4 x 20 MB (default is 10 x 100 MB).
IF NOT EXISTS (
    SELECT 1
    FROM sys.server_event_session_fields f
    JOIN sys.server_event_sessions s ON s.event_session_id = f.event_session_id
    WHERE s.name = N'system_health' AND f.name = N'max_rollover_files' AND CAST(f.value AS int) = 4)
BEGIN
    ALTER EVENT SESSION [system_health] ON SERVER STATE = STOP;
    ALTER EVENT SESSION [system_health] ON SERVER DROP TARGET package0.event_file;
    ALTER EVENT SESSION [system_health] ON SERVER ADD TARGET package0.event_file
        (SET filename = N'system_health.xel', max_file_size = (20), max_rollover_files = (4));
    ALTER EVENT SESSION [system_health] ON SERVER STATE = START;
END
GO
