using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

string dbPath = "cities.db";
string countriesCsv = Path.Combine(AppContext.BaseDirectory, "countries.csv");
string citiesCsv = Path.Combine(AppContext.BaseDirectory, "cities.csv");

var db = new DatabaseManager(dbPath);
db.CreateTables();

// Автозагрузка CSV, если таблицы пусты
if (db.GetAllCountries().Count == 0 && File.Exists(countriesCsv))
{
    db.ImportFromCsv(countriesCsv, citiesCsv);
    Console.WriteLine("[OK] Данные загружены из CSV.\n");
}

string choice;
do
{
    Console.WriteLine("\n🏙 УПРАВЛЕНИЕ ГОРОДАМИ 🌍");
    Console.WriteLine("1 — Список стран");
    Console.WriteLine("2 — Список городов");
    Console.WriteLine("3 — Добавить город");
    Console.WriteLine("4 — Редактировать город");
    Console.WriteLine("5 — Удалить город");
    Console.WriteLine("6 — Отчёты");
    Console.WriteLine("7 — Фильтр по стране [Группа Г]");
    Console.WriteLine("0 — Выход");
    Console.Write("Ваш выбор: ");
    choice = Console.ReadLine()?.Trim() ?? "";

    switch (choice)
    {
        case "1": PrintList(db.GetAllCountries(), "Страны"); break;
        case "2": PrintList(db.GetAllCities(), "Города"); break;
        case "3": AddCity(db); break;
        case "4": EditCity(db); break;
        case "5": DeleteCity(db); break;
        case "6": ReportsMenu(db); break;
        case "7": FilterByCountry(db); break;
        case "0": Console.WriteLine("Выход."); break;
        default: Console.WriteLine("❌ Неверный пункт."); break;
    }
} while (choice != "0");

// --- Вспомогательные методы ---
static void PrintList<T>(List<T> list, string title)
{
    Console.WriteLine($"\n📋 {title}:");
    foreach (var item in list) Console.WriteLine($"  {item}");
    Console.WriteLine($"Итого: {list.Count}");
}

static void AddCity(DatabaseManager db)
{
    Console.WriteLine("\n➕ Добавление города");
    Console.WriteLine("Доступные страны:");
    PrintList(db.GetAllCountries(), "Страны");

    Console.Write("ID страны: ");
    if (!int.TryParse(Console.ReadLine(), out int cid)) { Console.WriteLine("❌ Ошибка ввода."); return; }

    Console.Write("Название города: ");
    string name = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(name)) { Console.WriteLine("❌ Имя не может быть пустым."); return; }

    Console.Write("Население (тыс. чел.): ");
    if (!int.TryParse(Console.ReadLine(), out int pop)) { Console.WriteLine("❌ Ошибка ввода."); return; }

    try
    {
        db.AddCity(new City(0, cid, name, pop));
        Console.WriteLine("✅ Город добавлен.");
    }
    catch (Exception ex) { Console.WriteLine($"❌ {ex.Message}"); }
}

static void EditCity(DatabaseManager db)
{
    Console.WriteLine("\n✏️ Редактирование");
    Console.Write("ID города: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;
    var city = db.GetCityById(id);
    if (city == null) { Console.WriteLine("❌ Не найден."); return; }

    Console.WriteLine($"Текущие: {city}\n(Enter = оставить без изменений)");

    Console.Write($"Страна [{city.CountryId}]: ");
    string inp = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(inp) && int.TryParse(inp, out int nc)) city.CountryId = nc;

    Console.Write($"Название [{city.Name}]: ");
    inp = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(inp)) city.Name = inp;

    Console.Write($"Население [{city.PopulationK}]: ");
    inp = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(inp) && int.TryParse(inp, out int np))
        try { city.PopulationK = np; } catch (Exception ex) { Console.WriteLine($"❌ {ex.Message}"); return; }

    db.UpdateCity(city);
    Console.WriteLine("✅ Обновлено.");
}

static void DeleteCity(DatabaseManager db)
{
    Console.WriteLine("\n🗑 Удаление");
    Console.Write("ID города: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;
    var city = db.GetCityById(id);
    if (city == null) { Console.WriteLine("❌ Не найден."); return; }

    Console.Write($"Удалить «{city.Name}»? (да/нет): ");
    if (Console.ReadLine()?.Trim().ToLower() == "да") { db.DeleteCity(id); Console.WriteLine("✅ Удалено."); }
}

static void ReportsMenu(DatabaseManager db)
{
    string c;
    do
    {
        Console.WriteLine("\n📊 ОТЧЁТЫ");
        Console.WriteLine("1 — Города по странам (JOIN)");
        Console.WriteLine("2 — Количество городов по странам (COUNT)");
        Console.WriteLine("3 — Среднее население по странам (AVG)");
        Console.WriteLine("0 — Назад");
        Console.Write("Выбор: ");
        c = Console.ReadLine()?.Trim();

        switch (c)
        {
            case "1":
                new ReportBuilder(db)
                    .Query("SELECT c.city_name, cn.country_name, c.population_k FROM cities c JOIN countries cn ON c.country_id = cn.country_id ORDER BY c.city_name")
                    .Title("Города по странам")
                    .Header("Город", "Страна", "Население (тыс.)")
                    .ColumnWidths(25, 15, 18)
                    .Print(); break;
            case "2":
                new ReportBuilder(db)
                    .Query("SELECT cn.country_name, COUNT(*) AS cnt FROM cities c JOIN countries cn ON c.country_id = cn.country_id GROUP BY cn.country_name ORDER BY cn.country_name")
                    .Title("Количество городов по странам")
                    .Header("Страна", "Кол-во")
                    .ColumnWidths(20, 10)
                    .Print(); break;
            case "3":
                new ReportBuilder(db)
                    .Query("SELECT cn.country_name, ROUND(AVG(c.population_k), 1) AS avg_pop FROM cities c JOIN countries cn ON c.country_id = cn.country_id GROUP BY cn.country_name ORDER BY avg_pop DESC")
                    .Title("Среднее население по странам")
                    .Header("Страна", "Среднее (тыс.)")
                    .ColumnWidths(20, 15)
                    .Print(); break;
        }
    } while (c != "0");
}

static void FilterByCountry(DatabaseManager db)
{
    Console.WriteLine("\n🔍 Фильтр городов по стране [Группа Г]");
    Console.WriteLine("Доступные страны:");
    PrintList(db.GetAllCountries(), "Страны");

    Console.Write("Введите ID страны: ");
    if (!int.TryParse(Console.ReadLine(), out int cid))
    {
        Console.WriteLine("❌ Ошибка: введите целое число.");
        return;
    }

    var cities = db.GetCitiesByCountryId(cid);
    if (cities.Count == 0)
        Console.WriteLine("ℹ️ В этой стране нет городов в базе.");
    else
    {
        Console.WriteLine($"\n🏙 Города выбранной страны:");
        PrintList(cities, "Города");
    }
}