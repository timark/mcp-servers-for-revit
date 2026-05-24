using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace RevitMcpServer.Database;

/// <summary>
/// SQLite-backed store for Revit project and room metadata.
/// Replicates the schema and operations from server/src/database/service.ts.
/// Database file is stored alongside the executable as revit-data.db.
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private readonly SqliteConnection _connection;

    public DatabaseService()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "revit-data.db");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS projects (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                project_name TEXT NOT NULL,
                project_path TEXT,
                project_number TEXT,
                project_address TEXT,
                client_name TEXT,
                project_status TEXT,
                author TEXT,
                timestamp INTEGER NOT NULL,
                last_updated INTEGER NOT NULL,
                metadata TEXT
            );

            CREATE TABLE IF NOT EXISTS rooms (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                project_id INTEGER NOT NULL,
                room_id TEXT NOT NULL,
                room_name TEXT,
                room_number TEXT,
                department TEXT,
                level TEXT,
                area REAL,
                perimeter REAL,
                occupancy TEXT,
                comments TEXT,
                timestamp INTEGER NOT NULL,
                metadata TEXT,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
                UNIQUE(project_id, room_id)
            );

            CREATE INDEX IF NOT EXISTS idx_projects_name ON projects(project_name);
            CREATE INDEX IF NOT EXISTS idx_projects_timestamp ON projects(timestamp);
            CREATE INDEX IF NOT EXISTS idx_rooms_project_id ON rooms(project_id);
            CREATE INDEX IF NOT EXISTS idx_rooms_room_number ON rooms(room_number);
            """;
        cmd.ExecuteNonQuery();
    }

    // ── Projects ────────────────────────────────────────────────────────────

    public long StoreProject(string projectName, string? projectPath, string? projectNumber,
        string? projectAddress, string? clientName, string? projectStatus, string? author,
        string? metadataJson)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var existing = GetProjectIdByName(projectName);

        if (existing.HasValue)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                UPDATE projects SET
                    project_path = $path, project_number = $num, project_address = $addr,
                    client_name = $client, project_status = $status, author = $author,
                    last_updated = $now, metadata = $meta
                WHERE id = $id
                """;
            cmd.Parameters.AddWithValue("$path", (object?)projectPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$num", (object?)projectNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$addr", (object?)projectAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$client", (object?)clientName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$status", (object?)projectStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$author", (object?)author ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.Parameters.AddWithValue("$meta", (object?)metadataJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", existing.Value);
            cmd.ExecuteNonQuery();
            return existing.Value;
        }
        else
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO projects
                    (project_name, project_path, project_number, project_address,
                     client_name, project_status, author, timestamp, last_updated, metadata)
                VALUES ($name, $path, $num, $addr, $client, $status, $author, $now, $now, $meta);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$name", projectName);
            cmd.Parameters.AddWithValue("$path", (object?)projectPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$num", (object?)projectNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$addr", (object?)projectAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$client", (object?)clientName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$status", (object?)projectStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$author", (object?)author ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.Parameters.AddWithValue("$meta", (object?)metadataJson ?? DBNull.Value);
            return (long)cmd.ExecuteScalar()!;
        }
    }

    public List<Dictionary<string, object?>> GetAllProjects()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, project_name, project_path, project_number, project_address,
                   client_name, project_status, author, timestamp, last_updated, metadata
            FROM projects ORDER BY last_updated DESC
            """;
        return ReadProjects(cmd);
    }

    public Dictionary<string, object?>? GetProjectById(long id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, project_name, project_path, project_number, project_address,
                   client_name, project_status, author, timestamp, last_updated, metadata
            FROM projects WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$id", id);
        return ReadProjects(cmd).FirstOrDefault();
    }

    public Dictionary<string, object?>? GetProjectByName(string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, project_name, project_path, project_number, project_address,
                   client_name, project_status, author, timestamp, last_updated, metadata
            FROM projects WHERE project_name = $name
            """;
        cmd.Parameters.AddWithValue("$name", name);
        return ReadProjects(cmd).FirstOrDefault();
    }

    private long? GetProjectIdByName(string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM projects WHERE project_name = $name";
        cmd.Parameters.AddWithValue("$name", name);
        var result = cmd.ExecuteScalar();
        return result is long id ? id : null;
    }

    private static List<Dictionary<string, object?>> ReadProjects(SqliteCommand cmd)
    {
        var list = new List<Dictionary<string, object?>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>
            {
                ["id"] = reader.GetInt64(reader.GetOrdinal("id")),
                ["project_name"] = reader.IsDBNull(reader.GetOrdinal("project_name")) ? null : reader.GetString(reader.GetOrdinal("project_name")),
                ["project_path"] = reader.IsDBNull(reader.GetOrdinal("project_path")) ? null : reader.GetString(reader.GetOrdinal("project_path")),
                ["project_number"] = reader.IsDBNull(reader.GetOrdinal("project_number")) ? null : reader.GetString(reader.GetOrdinal("project_number")),
                ["project_address"] = reader.IsDBNull(reader.GetOrdinal("project_address")) ? null : reader.GetString(reader.GetOrdinal("project_address")),
                ["client_name"] = reader.IsDBNull(reader.GetOrdinal("client_name")) ? null : reader.GetString(reader.GetOrdinal("client_name")),
                ["project_status"] = reader.IsDBNull(reader.GetOrdinal("project_status")) ? null : reader.GetString(reader.GetOrdinal("project_status")),
                ["author"] = reader.IsDBNull(reader.GetOrdinal("author")) ? null : reader.GetString(reader.GetOrdinal("author")),
                ["timestamp"] = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(reader.GetOrdinal("timestamp"))).UtcDateTime.ToString("o"),
                ["last_updated"] = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(reader.GetOrdinal("last_updated"))).UtcDateTime.ToString("o"),
                ["metadata"] = reader.IsDBNull(reader.GetOrdinal("metadata")) ? null
                    : JsonSerializer.Deserialize<object>(reader.GetString(reader.GetOrdinal("metadata")))
            };
            list.Add(row);
        }
        return list;
    }

    // ── Rooms ────────────────────────────────────────────────────────────────

    public int StoreRoomsBatch(long projectId, IEnumerable<RoomData> rooms)
    {
        int count = 0;
        using var transaction = _connection.BeginTransaction();
        foreach (var room in rooms)
        {
            StoreRoom(projectId, room, transaction);
            count++;
        }
        transaction.Commit();
        return count;
    }

    private void StoreRoom(long projectId, RoomData room, SqliteTransaction transaction)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string? metaJson = room.Metadata != null ? JsonSerializer.Serialize(room.Metadata) : null;

        // Check for existing room
        using var checkCmd = _connection.CreateCommand();
        checkCmd.Transaction = transaction;
        checkCmd.CommandText = "SELECT id FROM rooms WHERE project_id = $pid AND room_id = $rid";
        checkCmd.Parameters.AddWithValue("$pid", projectId);
        checkCmd.Parameters.AddWithValue("$rid", room.RoomId);
        var existing = checkCmd.ExecuteScalar();

        if (existing is long existingId)
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                UPDATE rooms SET
                    room_name = $name, room_number = $num, department = $dept,
                    level = $level, area = $area, perimeter = $perim,
                    occupancy = $occ, comments = $comments, timestamp = $now, metadata = $meta
                WHERE id = $id
                """;
            cmd.Parameters.AddWithValue("$name", (object?)room.RoomName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$num", (object?)room.RoomNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$dept", (object?)room.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$level", (object?)room.Level ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$area", room.Area.HasValue ? room.Area.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("$perim", room.Perimeter.HasValue ? room.Perimeter.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("$occ", (object?)room.Occupancy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$comments", (object?)room.Comments ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.Parameters.AddWithValue("$meta", (object?)metaJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", existingId);
            cmd.ExecuteNonQuery();
        }
        else
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO rooms
                    (project_id, room_id, room_name, room_number, department,
                     level, area, perimeter, occupancy, comments, timestamp, metadata)
                VALUES ($pid, $rid, $name, $num, $dept, $level, $area, $perim, $occ, $comments, $now, $meta)
                """;
            cmd.Parameters.AddWithValue("$pid", projectId);
            cmd.Parameters.AddWithValue("$rid", room.RoomId);
            cmd.Parameters.AddWithValue("$name", (object?)room.RoomName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$num", (object?)room.RoomNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$dept", (object?)room.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$level", (object?)room.Level ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$area", room.Area.HasValue ? room.Area.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("$perim", room.Perimeter.HasValue ? room.Perimeter.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("$occ", (object?)room.Occupancy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$comments", (object?)room.Comments ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.Parameters.AddWithValue("$meta", (object?)metaJson ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    public List<Dictionary<string, object?>> GetRoomsByProjectId(long projectId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, project_id, room_id, room_name, room_number, department,
                   level, area, perimeter, occupancy, comments, timestamp, metadata
            FROM rooms WHERE project_id = $pid ORDER BY room_number
            """;
        cmd.Parameters.AddWithValue("$pid", projectId);
        return ReadRooms(cmd);
    }

    public List<Dictionary<string, object?>> GetAllRoomsWithProject()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT r.id, r.project_id, r.room_id, r.room_name, r.room_number,
                   r.department, r.level, r.area, r.perimeter, r.occupancy,
                   r.comments, r.timestamp, r.metadata,
                   p.project_name, p.project_number
            FROM rooms r
            JOIN projects p ON r.project_id = p.id
            ORDER BY p.project_name, r.room_number
            """;
        return ReadRooms(cmd);
    }

    public Dictionary<string, object?> GetStats()
    {
        using var projCmd = _connection.CreateCommand();
        projCmd.CommandText = "SELECT COUNT(*) FROM projects";
        long projectCount = (long)projCmd.ExecuteScalar()!;

        using var roomCmd = _connection.CreateCommand();
        roomCmd.CommandText = "SELECT COUNT(*) FROM rooms";
        long roomCount = (long)roomCmd.ExecuteScalar()!;

        return new Dictionary<string, object?>
        {
            ["total_projects"] = projectCount,
            ["total_rooms"] = roomCount
        };
    }

    private static List<Dictionary<string, object?>> ReadRooms(SqliteCommand cmd)
    {
        var list = new List<Dictionary<string, object?>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string col = reader.GetName(i);
                if (reader.IsDBNull(i))
                {
                    row[col] = null;
                }
                else if (col == "timestamp")
                {
                    row[col] = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(i))
                                             .UtcDateTime.ToString("o");
                }
                else if (col == "metadata")
                {
                    row[col] = JsonSerializer.Deserialize<object>(reader.GetString(i));
                }
                else
                {
                    row[col] = reader.GetValue(i);
                }
            }
            list.Add(row);
        }
        return list;
    }

    public void Dispose() => _connection.Dispose();
}

public sealed class RoomData
{
    public required string RoomId { get; init; }
    public string? RoomName { get; init; }
    public string? RoomNumber { get; init; }
    public string? Department { get; init; }
    public string? Level { get; init; }
    public double? Area { get; init; }
    public double? Perimeter { get; init; }
    public string? Occupancy { get; init; }
    public string? Comments { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}
