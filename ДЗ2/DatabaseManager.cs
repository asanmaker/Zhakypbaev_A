using Microsoft.Data.Sqlite;

public class DatabaseManager
{
    private readonly string _connectionString;

    /// <summary>Конструктор. Принимает путь к файлу БД.</summary>
    public DatabaseManager(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>Создаёт таблицы, если они ещё не существуют</summary>
    public void CreateTables()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS countries (
                country_id INTEGER PRIMARY KEY AUTOINCREMENT,
                country_name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS cities (
                city_id INTEGER PRIMARY KEY AUTOINCREMENT,
                country_id INTEGER NOT NULL,
                city_name TEXT NOT NULL,
                population_k INTEGER NOT NULL,
                FOREIGN KEY(country_id) REFERENCES countries(country_id)
            );";
        cmd.ExecuteNonQuery();
    }

    /// <summary>Импорт данных из двух CSV-файлов</summary>
    public void ImportFromCsv(string countriesCsvPath, string citiesCsvPath)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Импорт стран
        foreach (var line in File.ReadLines(countriesCsvPath).Skip(1))
        {
            var parts = line.Split(';');
            if (parts.Length < 2) continue;
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO countries(country_id, country_name) VALUES(@id, @name)";
            cmd.Parameters.AddWithValue("@id", int.Parse(parts[0]));
            cmd.Parameters.AddWithValue("@name", parts[1]);
            cmd.ExecuteNonQuery();
        }

        // Импорт городов
        foreach (var line in File.ReadLines(citiesCsvPath).Skip(1))
        {
            var parts = line.Split(';');
            if (parts.Length < 4) continue;
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO cities(city_id, country_id, city_name, population_k) VALUES(@id, @cid, @name, @pop)";
            cmd.Parameters.AddWithValue("@id", int.Parse(parts[0]));
            cmd.Parameters.AddWithValue("@cid", int.Parse(parts[1]));
            cmd.Parameters.AddWithValue("@name", parts[2]);
            cmd.Parameters.AddWithValue("@pop", int.Parse(parts[3]));
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Получить все страны</summary>
    public List<Country> GetAllCountries()
    {
        var result = new List<Country>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT country_id, country_name FROM countries ORDER BY country_id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new Country(reader.GetInt32(0), reader.GetString(1)));
        return result;
    }

    /// <summary>Получить все города</summary>
    public List<City> GetAllCities()
    {
        var result = new List<City>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT city_id, country_id, city_name, population_k FROM cities ORDER BY city_id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new City(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3)));
        return result;
    }

    /// <summary>Получить город по ID</summary>
    public City GetCityById(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT city_id, country_id, city_name, population_k FROM cities WHERE city_id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? new City(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3)) : null;
    }

    /// <summary>Добавить город (Id генерируется автоматически)</summary>
    public void AddCity(City city)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO cities(country_id, city_name, population_k) VALUES(@cid, @name, @pop)";
        cmd.Parameters.AddWithValue("@cid", city.CountryId);
        cmd.Parameters.AddWithValue("@name", city.Name);
        cmd.Parameters.AddWithValue("@pop", city.PopulationK);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Обновить данные города</summary>
    public void UpdateCity(City city)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE cities SET country_id=@cid, city_name=@name, population_k=@pop WHERE city_id=@id";
        cmd.Parameters.AddWithValue("@id", city.Id);
        cmd.Parameters.AddWithValue("@cid", city.CountryId);
        cmd.Parameters.AddWithValue("@name", city.Name);
        cmd.Parameters.AddWithValue("@pop", city.PopulationK);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Удалить город по ID</summary>
    public void DeleteCity(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM cities WHERE city_id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>[ГРУППА Г] Получить города конкретной страны</summary>
    public List<City> GetCitiesByCountryId(int countryId)
    {
        var result = new List<City>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT city_id, country_id, city_name, population_k FROM cities WHERE country_id=@cid ORDER BY city_name";
        cmd.Parameters.AddWithValue("@cid", countryId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new City(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3)));
        return result;
    }

    /// <summary>Выполняет произвольный SQL-запрос для отчётов</summary>
    public (string[] columns, List<string[]> rows) ExecuteQuery(string sql)
    {
        var rows = new List<string[]>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();

        string[] columns = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++) columns[i] = reader.GetName(i);

        while (reader.Read())
        {
            string[] row = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                row[i] = reader.GetValue(i)?.ToString() ?? "";
            rows.Add(row);
        }
        return (columns, rows);
    }
}