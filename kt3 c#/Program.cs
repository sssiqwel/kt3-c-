using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;

public class User
{
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateTime BirthDate { get; set; }

    public string? Phone { get; set; }

    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Year;
            if (BirthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}

public static class RussianNames
{
    public static readonly string[] FirstNamesMale =
    {
        "Александр", "Алексей", "Андрей", "Артем", "Борис",
        "Вадим", "Василий", "Виктор", "Владимир", "Дмитрий",
        "Евгений", "Иван", "Игорь", "Кирилл", "Максим",
        "Михаил", "Никита", "Олег", "Павел", "Роман",
        "Сергей", "Станислав", "Юрий", "Ярослав"
    };

    public static readonly string[] FirstNamesFemale =
    {
        "Александра", "Алина", "Анастасия", "Анна", "Валентина",
        "Валерия", "Вера", "Виктория", "Галина", "Дарья",
        "Екатерина", "Елена", "Ирина", "Ксения", "Лариса",
        "Марина", "Мария", "Наталья", "Ольга", "Светлана",
        "Татьяна", "Юлия", "Яна"
    };

    public static readonly string[] LastNames =
    {
        "Иванов", "Петров", "Сидоров", "Кузнецов", "Попов",
        "Васильев", "Смирнов", "Новиков", "Федоров", "Морозов",
        "Волков", "Алексеев", "Лебедев", "Семенов", "Егоров",
        "Павлов", "Козлов", "Степанов", "Николаев", "Орлов",
        "Андреев", "Макаров", "Никитин", "Захаров"
    };
}

public class UserGenerator
{
    private const int MinAge = 14;
    private readonly Random _random;

    public UserGenerator()
    {
        _random = new Random();
    }

    public User GenerateUser()
    {
        bool isMale = _random.Next(2) == 0;

        var firstName = isMale
            ? RussianNames.FirstNamesMale[_random.Next(RussianNames.FirstNamesMale.Length)]
            : RussianNames.FirstNamesFemale[_random.Next(RussianNames.FirstNamesFemale.Length)];

        var lastName = isMale
            ? RussianNames.LastNames[_random.Next(RussianNames.LastNames.Length)]
            : RussianNames.LastNames[_random.Next(RussianNames.LastNames.Length)] + "а";

        var years = _random.Next(MinAge, 80);
        var months = _random.Next(12);
        var days = _random.Next(1, 28);
        var birthDate = DateTime.Today.AddYears(-years).AddMonths(-months).AddDays(-days);

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = GenerateEmail(firstName, lastName),
            BirthDate = birthDate,
            Phone = GeneratePhone()
        };

        if (user.Age < MinAge)
        {
            throw new InvalidOperationException($"Пользователь {firstName} {lastName} слишком молод: {user.Age} лет");
        }

        return user;
    }

    private string GenerateEmail(string firstName, string lastName)
    {
        var domains = new[] { "gmail.com", "mail.ru", "yandex.ru", "yahoo.com" };
        var domain = domains[_random.Next(domains.Length)];

        var formats = new[]
        {
            $"{firstName.ToLower()}.{lastName.ToLower()}",
            $"{firstName.ToLower()}_{lastName.ToLower()}",
            $"{firstName[0]}.{lastName.ToLower()}",
            $"{lastName.ToLower()}.{firstName.ToLower()}"
        };

        var format = formats[_random.Next(formats.Length)];
        return $"{format}{_random.Next(100)}@{domain}";
    }

    private string GeneratePhone()
    {
        return $"+7 {_random.Next(900, 999)} {_random.Next(100, 999)}-{_random.Next(10, 99)}-{_random.Next(10, 99)}";
    }

    public List<User> GenerateUsers(int count = 10)
    {
        var users = new List<User>();
        var attempts = 0;
        var maxAttempts = count * 3;

        Console.WriteLine($"Генерация {count} пользователей...");

        while (users.Count < count && attempts < maxAttempts)
        {
            attempts++;

            try
            {
                var user = GenerateUser();
                users.Add(user);
                Console.WriteLine($"✅ {user.FirstName} {user.LastName}, возраст: {user.Age}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }
        }

        Console.WriteLine($"Сгенерировано: {users.Count} пользователей");
        return users;
    }
}

public class DatabaseManager
{
    private readonly string _connectionString;

    public DatabaseManager()
    {
        _connectionString = "Data Source=users.db";
    }

    public void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Email TEXT NOT NULL UNIQUE,
                BirthDate TEXT NOT NULL,
                Phone TEXT,
                Age INTEGER NOT NULL
            )";
        command.ExecuteNonQuery();

        Console.WriteLine("✅ База данных создана");
    }

    public void SaveUsers(List<User> users)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var savedCount = 0;

        foreach (var user in users)
        {
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Users (FirstName, LastName, Email, BirthDate, Phone, Age)
                    VALUES (@firstName, @lastName, @email, @birthDate, @phone, @age)";

                command.Parameters.AddWithValue("@firstName", user.FirstName);
                command.Parameters.AddWithValue("@lastName", user.LastName);
                command.Parameters.AddWithValue("@email", user.Email);
                command.Parameters.AddWithValue("@birthDate", user.BirthDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@phone", user.Phone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@age", user.Age);

                command.ExecuteNonQuery();
                savedCount++;
                Console.WriteLine($"💾 Сохранен: {user.FirstName} {user.LastName}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint failed
            {
                Console.WriteLine($"⚠️ Дубликат email: {user.Email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения: {ex.Message}");
            }
        }

        Console.WriteLine($"📊 Сохранено: {savedCount}/{users.Count} пользователей");
    }

    public void DisplayAllUsers()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Users ORDER BY Id";

        using var reader = command.ExecuteReader();

        Console.WriteLine("\n📋 Все пользователи в базе:");
        Console.WriteLine("=========================================");

        if (!reader.HasRows)
        {
            Console.WriteLine("База данных пуста");
            return;
        }

        while (reader.Read())
        {
            Console.WriteLine($"👤 {reader.GetString(1)} {reader.GetString(2)}");
            Console.WriteLine($"   📧 {reader.GetString(3)}");
            Console.WriteLine($"   📅 {DateTime.Parse(reader.GetString(4)):dd.MM.yyyy}");
            Console.WriteLine($"   📅 Возраст: {reader.GetInt32(6)} лет");
            Console.WriteLine($"   📞 {reader.GetString(5)}");
            Console.WriteLine($"   🆔 ID: {reader.GetInt32(0)}");
            Console.WriteLine("-----------------------------------------");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 Генерация пользователей и сохранение в SQLite");
        Console.WriteLine("=========================================\n");

        try
        {
            var userGenerator = new UserGenerator();
            var dbManager = new DatabaseManager();

            dbManager.InitializeDatabase();


            var users = userGenerator.GenerateUsers(10);

            if (users.Count == 0)
            {
                Console.WriteLine("❌ Не удалось сгенерировать пользователей");
                return;
            }


            Console.WriteLine("\n💾 Сохранение в базу данных...");
            dbManager.SaveUsers(users);

            dbManager.DisplayAllUsers();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Ошибка: {ex.Message}");
        }

        Console.WriteLine("\n🎉 Готово! Нажмите любую клавишу...");
        Console.ReadKey();
    }
}