public class Country
{
    /// <summary>Идентификатор страны</summary>
    public int Id { get; set; }
    /// <summary>Название страны</summary>
    public string Name { get; set; }

    /// <summary>Конструктор с параметрами</summary>
    public Country(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Конструктор по умолчанию (вызывает полный через цепочку this)</summary>
    public Country() : this(0, "") { }

    /// <summary>Строковое представление страны</summary>
    public override string ToString() => $"[{Id}] {Name}";
}